using System;
using System.Collections.Generic;
using System.Linq;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Debugger.Logger;
using SymphonyFrameWork.System.SaveSystem;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor.SettingProvider
{
    /// <summary>
    ///     Save SystemのProject Settings画面を提供する。
    /// </summary>
    public sealed class SaveDataSettingProvider
    {
        #region 外部向けAPI

        /// <summary> Project Settingsに表示する設定項目名。 </summary>
        public const string LABEL = "Save System";

        /// <summary> SettingsProviderの完全な設定パス。 </summary>
        public const string SELF_PATH = SymphonySettingProvider.PROVIDER_PATH + LABEL;

        /// <summary>
        ///     セーブローダー設定用のSettingsProviderを生成する。
        /// </summary>
        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
        {
            SettingsProvider provider = new(SELF_PATH, SettingsScope.Project)
            {
                label = LABEL,
                guiHandler = IMGUI,
                activateHandler = (_, _) => ReloadManagedTypes(),
                keywords = new HashSet<string>(new[]
                {
                    "save", "savedata", "registry", "loader", "managed", "type"
                }),
            };

            return provider;
        }

        #endregion

        #region 内部処理

        /// <summary> 名前空間ノードの一括切り替えトグルの幅。 </summary>
        private const float NAMESPACE_TOGGLE_WIDTH = 18f;

        private static IReadOnlyList<Type> _saveDataTypes = Array.Empty<Type>();

        private static SaveDataTypeTreeNode _typeTree;

        /// <summary> 型の完全名と、テストAssembly由来かどうかの対応。 </summary>
        /// <remarks>
        ///     テストAssembly判定はAssemblyの参照メタデータを読むため、IMGUIの再描画ごとに
        ///     全型分を評価しない。探索し直したときにだけ作り直す。
        /// </remarks>
        private static KeyValuePair<string, bool>[] _catalogEntries =
            Array.Empty<KeyValuePair<string, bool>>();

        /// <summary>
        ///     ローダー選択と現在のローダー情報を描画する。
        /// </summary>
        private static void IMGUI(string searchContext)
        {
            SymphonyDocumentationGUI.DrawOpenButton(SymphonyDocumentPageEnum.SaveDataSystem);

            // 画面を開いただけでRuntime Configの生成を始めず、取得できたものだけを描画する。
            SaveDataConfig config = SymphonyConfigLocator.GetConfig<SaveDataConfig>();
            // 未生成の状態でも例外にせず、生成の導線だけを出す。
            if (config == null)
            {
                DrawMissingConfig();
            }
            else { DrawLoaderConfig(config); }

            // Runtime Configの有無に関係なく、Editor専用の型管理設定を続けて描画する。
            EditorGUILayout.Space();
            DrawManagedTypes();
        }

        /// <summary>
        ///     ローダー選択と現在のローダー情報を描画する。
        /// </summary>
        /// <param name="config"> 編集するSave Data設定。 </param>
        private static void DrawLoaderConfig(SaveDataConfig config)
        {
            // ローダーの用途と拡張方法を設定項目の直前で案内する。
            EditorGUILayout.LabelField("Project Save Loader", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "SaveStore が使用するローダーを設定します。独自ローダーは SaveDataLoaderStrategy を継承してください。共通の検証やデータ復旧処理は基底クラスが担当します。",
                MessageType.Info);

            SerializedObject serializedObject = new(config);
            SerializedProperty loaderProperty = serializedObject.FindProperty("_loader");

            // SerializeReferenceの具象型情報を維持するため、標準PropertyFieldで多態的な値を編集する。
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(loaderProperty, new GUIContent("Loader"), true);
            // ローダーが変わった場合だけアセットを保存して実行時キャッシュを更新し、未変更時はUndoを増やさない。
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                SaveStore.RefreshLoader();
            }
            else { serializedObject.ApplyModifiedPropertiesWithoutUndo(); }

            // 未設定時は実行時のフォールバックを案内し、設定済みなら実際の具象型を表示する。
            if (config.Loader == null)
            {
                EditorGUILayout.HelpBox("ローダーが未設定です。SaveStore は既定の JsonUtility ローダーへフォールバックします。", MessageType.Warning);
            }
            else { EditorGUILayout.LabelField("Current Loader Type", config.Loader.GetType().FullName); }
        }

        /// <summary>
        ///     管理対象にするセーブデータ型を名前空間ツリーで描画する。
        /// </summary>
        private static void DrawManagedTypes()
        {
            EditorGUILayout.LabelField("Managed Save Data Types", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Save Data パネルへ表示する型を選択します。名前空間のチェックは配下を一括で切り替えます。",
                MessageType.Info);

            // activateHandlerより先に描画された場合も、空表示で固定せず現在の型を取得する。
            if (_typeTree == null) { ReloadManagedTypes(); }

            // 対応型が無ければ、チェック項目を描かず理由だけを表示する。
            if (_saveDataTypes.Count <= 0)
            {
                EditorGUILayout.HelpBox(
                    "プロジェクト内に SaveDataContent を継承したセーブデータ型が見つかりません。",
                    MessageType.Info);
                return;
            }

            // 保存済みの明示値とAssemblyごとの既定値から、現在のチェック状態を解決する。
            SaveDataVisibilityConfig config = SaveDataVisibilityConfig.instance;
            SaveDataVisibilityMap visibilityMap = new(_catalogEntries, config.GetOverrides());

            // ルートは表示用の容器なので、直下の名前空間または型から描画する。
            foreach (SaveDataTypeTreeNode child in _typeTree.Children)
            {
                DrawTypeNode(child, visibilityMap, config);
            }
        }

        /// <summary>
        ///     セーブデータ型ツリーの1ノードを描画する。
        /// </summary>
        /// <param name="node"> 描画するノード。 </param>
        /// <param name="visibilityMap"> 現在の管理対象状態。 </param>
        /// <param name="config"> 上書き値の保存先。 </param>
        private static void DrawTypeNode(
            SaveDataTypeTreeNode node,
            SaveDataVisibilityMap visibilityMap,
            SaveDataVisibilityConfig config)
        {
            // 葉は型ごとのチェックとして描き、操作された値だけを明示的に保存する。
            if (node.IsLeaf)
            {
                bool isManaged = visibilityMap.IsManaged(node.TypeFullName);
                bool edited = EditorGUILayout.ToggleLeft(node.Name, isManaged);
                if (edited != isManaged) { TrySetManaged(config, node.TypeFullName, edited); }
                return;
            }

            List<string> descendantTypeNames = node.EnumerateTypeFullNames().ToList();
            bool allManaged = descendantTypeNames.All(visibilityMap.IsManaged);
            bool noneManaged = descendantTypeNames.All(typeFullName => !visibilityMap.IsManaged(typeFullName));
            bool isMixed = !allManaged && !noneManaged;

            EditorGUILayout.BeginHorizontal();

            // 型の葉と同じ位置へチェックを並べるため、Foldoutより先に描く。
            // Foldoutは横方向へ広がるため、後に置くと行の右端へ追い出される。
            EditorGUI.BeginChangeCheck();
            bool editedValue;
            EditorGUI.showMixedValue = isMixed;
            try
            {
                // 混在状態の値はfalseとして描き、最初の操作では全選択へ切り替える。
                editedValue = EditorGUILayout.Toggle(allManaged, GUILayout.Width(NAMESPACE_TOGGLE_WIDTH));
            }
            finally
            {
                // 後続のIMGUI項目へ混在表示が漏れないよう、描画直後に必ず戻す。
                EditorGUI.showMixedValue = false;
            }

            bool isChanged = EditorGUI.EndChangeCheck();

            // 階層の字下げは先頭のトグルが消費済みのため、同じ行のFoldoutへ二重に掛けない。
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            try
            {
                // 名前空間の展開状態はツリーが保持し、探索結果が変わるまで維持する。
                node.IsExpanded = EditorGUILayout.Foldout(node.IsExpanded, node.Name, true);
            }
            finally
            {
                EditorGUI.indentLevel = indentLevel;
            }

            EditorGUILayout.EndHorizontal();

            // 名前空間の操作は、子孫すべてを書き換えてから保存と通知を1回だけ行う。
            if (isChanged)
            {
                bool managedValue = isMixed || editedValue;
                TrySetManaged(config, descendantTypeNames, managedValue);
            }

            // 折りたたまれている名前空間では子孫の描画を打ち切る。
            if (!node.IsExpanded) { return; }

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (SaveDataTypeTreeNode child in node.Children)
                {
                    DrawTypeNode(child, visibilityMap, config);
                }
            }
        }

        /// <summary>
        ///     1つの型の上書き値を、IMGUIへ例外を漏らさず保存する。
        /// </summary>
        /// <param name="config"> 上書き値の保存先。 </param>
        /// <param name="typeFullName"> 対象型の完全名。 </param>
        /// <param name="isManaged"> 管理対象にする場合はtrue。 </param>
        private static void TrySetManaged(
            SaveDataVisibilityConfig config,
            string typeFullName,
            bool isManaged)
        {
            try
            {
                // 明示操作された1件だけをProjectSettingsへ保存する。
                config.SetManaged(typeFullName, isManaged);
            }
            catch (Exception exception)
            {
                // IMGUI描画中に例外を再送出せず、Consoleへ診断を残す。
                SymphonyDebugLogger.LogException(exception);
            }
        }

        /// <summary>
        ///     複数型の上書き値を、IMGUIへ例外を漏らさず一括保存する。
        /// </summary>
        /// <param name="config"> 上書き値の保存先。 </param>
        /// <param name="typeFullNames"> 対象型の完全名。 </param>
        /// <param name="isManaged"> 管理対象にする場合はtrue。 </param>
        private static void TrySetManaged(
            SaveDataVisibilityConfig config,
            IEnumerable<string> typeFullNames,
            bool isManaged)
        {
            try
            {
                // 配下の上書きをまとめて反映し、保存と通知を1回に抑える。
                config.SetManaged(typeFullNames, isManaged);
            }
            catch (Exception exception)
            {
                // IMGUI描画中に例外を再送出せず、Consoleへ診断を残す。
                SymphonyDebugLogger.LogException(exception);
            }
        }

        /// <summary>
        ///     現在の対応型を探索し、変化した場合だけ表示ツリーを作り直す。
        /// </summary>
        private static void ReloadManagedTypes()
        {
            IReadOnlyList<Type> latestTypes = SaveDataTypeCatalog.CollectSupportedTypes();
            bool isChanged = latestTypes.Count != _saveDataTypes.Count
                || !latestTypes.SequenceEqual(_saveDataTypes);

            // 探索結果が同じ間は、各ノードが保持するFoldoutの展開状態を維持する。
            if (!isChanged && _typeTree != null) { return; }

            _saveDataTypes = latestTypes;
            _catalogEntries = latestTypes
                .Select(type => new KeyValuePair<string, bool>(
                    type.FullName,
                    SaveDataTypeCatalog.IsTestAssembly(type.Assembly)))
                .ToArray();
            _typeTree = SaveDataTypeTreeNode.Build(latestTypes.Select(type => type.FullName));
        }

        /// <summary>
        ///     SaveDataConfigが未生成であることと、生成の導線を描画する。
        /// </summary>
        /// <remarks>
        ///     生成そのものはOrchestratorへ委譲する。設定画面のcallbackからpackage-wideな初期化を
        ///     始めると、Editor起動時のAsset変更を1回のRefreshへ集約する設計の外側で生成が走る。
        /// </remarks>
        private static void DrawMissingConfig()
        {
            EditorGUILayout.HelpBox(
                "SaveDataConfig がまだ生成されていません。"
                + " 通常はEditor起動時に生成されます。生成されていない場合は下のボタンで生成してください。",
                MessageType.Warning);

            // 押されたときだけ生成を要求し、画面を開いた時点ではAssetを変更しない。
            if (!GUILayout.Button("設定アセットを生成")) { return; }

            // 初期化が完了していない間は、その初期化自身が同じ生成を行う。ここでは案内だけを残す。
            if (!SymphonyEditorOrchestrator.RequestPackageSetup())
            {
                SymphonyDebugLogger.LogDirect(
                    "Symphony Frameworkの初期化中のため、設定アセットの生成を要求できませんでした。"
                    + " 初期化の完了後に再度お試しください。",
                    LogKindEnum.Warning);
            }
        }

        #endregion
    }
}
