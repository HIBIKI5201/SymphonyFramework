using SymphonyFrameWork.Core;
using System.Collections.Generic;
using UnityEditor;

namespace SymphonyFrameWork.Editor.SettingProvider
{
    public class SymphonySettingProvider
    {
        public const string LABEL = SymphonyConstant.SYMPHONY_FRAMEWORK;
        public const string SELF_PATH = EditorSymphonyConstant.PROJECT_SETTING_PATH + LABEL;
        public const string PROVIDER_PATH = EditorSymphonyConstant.PROJECT_SETTING_PATH + LABEL + "/";

        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
        {
            // SettingsScope.Projectを指定することでProject Settingsに項目を追加できる
            var provider = new SettingsProvider(SELF_PATH, SettingsScope.Project)
            {
                // 項目のタイトル
                label = LABEL,

                // どのように描画するか(IMGUI)
                guiHandler = IMGUI,

                // 検索するときのキーワード
                keywords = new HashSet<string>(new[] { "CustomSetting" }),
            };

            return provider;
        }

        private static void IMGUI(string searchContext)
        {
            EditorGUILayout.LabelField("これはSettingsProviderにより追加した独自項目です。");
        }
    }
}
