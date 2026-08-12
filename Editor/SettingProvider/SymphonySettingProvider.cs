using System.Collections.Generic;

using SymphonyFrameWork.Core;

using UnityEditor;

namespace SymphonyFrameWork.Editor.SettingProvider
{
    /// <summary>
    ///     Symphony FrameworkのルートProject Settings画面を提供する。
    /// </summary>
    public sealed class SymphonySettingProvider
    {
        #region 外部向けAPI

        /// <summary> Project Settingsに表示するルート設定項目名。 </summary>
        public const string LABEL = SymphonyConstant.SYMPHONY_FRAMEWORK;

        /// <summary> ルートSettingsProviderの完全な設定パス。 </summary>
        public const string SELF_PATH = EditorSymphonyConstant.PROJECT_SETTING_PATH + LABEL;

        /// <summary> 子SettingsProviderが使用する設定パスの接頭辞。 </summary>
        public const string PROVIDER_PATH = EditorSymphonyConstant.PROJECT_SETTING_PATH + LABEL + "/";

        /// <summary>
        ///     Framework設定用のSettingsProviderを生成する。
        /// </summary>
        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
        {
            // Project単位の入口へ、個人設定を編集するIMGUI画面と検索語を登録する。
            SettingsProvider provider = new(SELF_PATH, SettingsScope.Project)
            {
                label = LABEL,
                guiHandler = IMGUI,
                keywords = new HashSet<string>(new[]
                {
                    "symphony",
                    "framework",
                    "asset",
                    "protection",
                    "service locator",
                    "log",
                }),
            };

            return provider;
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     Frameworkのアセット保護とService Locatorログ設定を描画する。
        /// </summary>
        private static void IMGUI(string searchContext)
        {
            SymphonyDocumentationGUI.DrawOpenButton(SymphonyDocumentPageEnum.EditorTools);

            SymphonyUserSettingConfig config =
                SymphonyEditorConfigLocator.GetConfig<SymphonyUserSettingConfig>();

            // 個人ごとの保護設定をUserSettings/SymphonyFrameWork配下のアセットへ即時保存する。
            EditorGUILayout.LabelField("Asset Protection", EditorStyles.boldLabel);
            AssetProtectionModeEnum protectionMode =
                (AssetProtectionModeEnum)EditorGUILayout.EnumPopup(
                    "Protection Mode",
                    config.AssetProtectionMode);
            if (protectionMode != config.AssetProtectionMode) { config.AssetProtectionMode = protectionMode; }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Service Locator Logs", EditorStyles.boldLabel);

            // 3種のログ設定を一度に比較し、変更が無いフレームでは保存処理を避ける。
            bool isSetInstanceLogEnabled = EditorGUILayout.Toggle(
                "Set Instance",
                config.IsServiceLocatorSetInstanceLogEnabled);
            bool isGetInstanceLogEnabled = EditorGUILayout.Toggle(
                "Get Instance",
                config.IsServiceLocatorGetInstanceLogEnabled);
            bool isDestroyInstanceLogEnabled = EditorGUILayout.Toggle(
                "Destroy Instance",
                config.IsServiceLocatorDestroyInstanceLogEnabled);

            bool hasLogOptionChanged =
                isSetInstanceLogEnabled != config.IsServiceLocatorSetInstanceLogEnabled ||
                isGetInstanceLogEnabled != config.IsServiceLocatorGetInstanceLogEnabled ||
                isDestroyInstanceLogEnabled != config.IsServiceLocatorDestroyInstanceLogEnabled;
            if (!hasLogOptionChanged) { return; }

            // 個人設定を保存した後、現在のEditorセッションで使うログオプションへ反映する。
            config.IsServiceLocatorSetInstanceLogEnabled = isSetInstanceLogEnabled;
            config.IsServiceLocatorGetInstanceLogEnabled = isGetInstanceLogEnabled;
            config.IsServiceLocatorDestroyInstanceLogEnabled = isDestroyInstanceLogEnabled;
            PackageInitializer.ApplyServiceLocateLogOptions();
        }

        #endregion
    }
}
