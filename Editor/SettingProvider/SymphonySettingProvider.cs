using SymphonyFrameWork.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Playables;

namespace SymphonyFrameWork.Editor.SettingProvider
{
    public class SymphonySettingProvider
    {
        public const string NAME = "Settings";
        public const string PATH = EditorSymphonyConstant.SETTING_PROVIDER_PATH + NAME;

        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
        {
            // SettingsScope.Projectを指定することでProject Settingsに項目を追加できる
            var provider = new SettingsProvider(PATH, SettingsScope.Project)
            {
                // 項目のタイトル
                label = "Sample",

                // どのように描画するか(IMGUI)
                guiHandler = searchContext =>
                {
                    EditorGUILayout.LabelField("これはSettingsProviderにより追加した独自項目です。");
                },

                // 検索するときのキーワード
                keywords = new HashSet<string>(new[] { "CustomSetting" }),
            };

            return provider;
        }
    }
}
