using SymphonyFrameWork.Config;
using SymphonyFrameWork.System.SaveSystem;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor.SettingProvider
{
    public class SaveSystemSettingProvider
    {
        public const string LABEL = "Save System";
        public const string SELF_PATH = SymphonySettingProvider.PROVIDER_PATH + LABEL;

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

        private static void IMGUI(string searchContext)
        {
            SaveSystemConfig config = GetOrCreateConfig();
            if (config == null)
            {
                EditorGUILayout.HelpBox("SaveSystemConfig を生成できませんでした。", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Project Save Loader", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "SaveDataRegistry が使用するローダーを設定します。独自ローダーは SaveDataLoader を継承してください。共通の検証やデータ復旧処理は基底クラスが担当します。",
                MessageType.Info);

            SerializedObject serializedObject = new(config);
            SerializedProperty loaderProperty = serializedObject.FindProperty("_loader");

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(loaderProperty, new GUIContent("Loader"), true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                SaveDataRegistry.RefreshLoader();
            }
            else
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            if (config.Loader == null)
            {
                EditorGUILayout.HelpBox("ローダーが未設定です。SaveDataRegistry は既定の JsonUtility ローダーへフォールバックします。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Current Loader Type", config.Loader.GetType().FullName);
            }
        }

        private static SaveSystemConfig GetOrCreateConfig()
        {
            SaveSystemConfig config = SymphonyConfigLocator.GetConfig<SaveSystemConfig>();
            if (config != null)
            {
                return config;
            }

            SymphonyConfigManager.AllConfigCheck();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<SaveSystemConfig>(
                SymphonyConfigLocator.GetFullPath<SaveSystemConfig>());
        }
    }
}
