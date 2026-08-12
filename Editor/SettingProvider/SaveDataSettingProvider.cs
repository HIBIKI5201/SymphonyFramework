using SymphonyFrameWork.Config;
using SymphonyFrameWork.System.SaveSystem;
using System.Collections.Generic;
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

            // PackageInitializerより先に画面が開かれた場合も、設定を生成してから描画する。
            SaveDataConfig config = GetOrCreateConfig();
            // 生成後も取得できない場合は、nullを渡したSerializedObjectの生成を避ける。
            if (config == null)
            {
                EditorGUILayout.HelpBox("SaveDataConfig を生成できませんでした。", MessageType.Error);
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
        ///     SaveDataConfigを取得し、存在しない場合は生成して再取得する。
        /// </summary>
        private static SaveDataConfig GetOrCreateConfig()
        {
            SaveDataConfig config = SymphonyConfigLocator.GetConfig<SaveDataConfig>();
            // 既存設定が見つかった場合は、生成処理とAssetDatabase更新を省く。
            if (config != null) { return config; }

            // PackageInitializer以外の生成入口として全設定を確認し、AssetDatabase更新後に保存先から直接読み直す。
            // TODO(#162): SettingsProviderのcallbackからpackage-wideな初期化とAsset生成を開始している。
            //             規約は発見用属性のcallbackから初期化を始めることを禁じており、
            //             Editor起動時のAsset変更はOrchestratorが集約してRefreshを1回に抑える設計になっている。
            //             生成の開始をSymphonyEditorOrchestrator側の入口へ委譲し、
            //             ここは未生成である旨の表示に留める。
            SymphonyConfigManager.AllConfigCheck();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<SaveDataConfig>(
                SymphonyConfigLocator.GetFullPath<SaveDataConfig>());
        }

        #endregion
    }
}
