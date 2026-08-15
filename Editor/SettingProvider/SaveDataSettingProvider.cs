using System.Collections.Generic;

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
            return new SettingsProvider(SELF_PATH, SettingsScope.Project)
            {
                label = LABEL,
                guiHandler = IMGUI,
                keywords = new HashSet<string>(new[] { "save", "savedata", "registry", "loader" }),
            };
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     ローダー選択と現在のローダー情報を描画する。
        /// </summary>
        private static void IMGUI(string searchContext)
        {
            SymphonyDocumentationGUI.DrawOpenButton(SymphonyDocumentPageEnum.SaveDataSystem);

            // 画面を開いただけで生成を始めず、取得できたものだけを描画する。
            SaveDataConfig config = SymphonyConfigLocator.GetConfig<SaveDataConfig>();
            // 未生成の状態でも例外にせず、生成の導線だけを出して描画を終える。
            if (config == null)
            {
                DrawMissingConfig();
                return;
            }

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
