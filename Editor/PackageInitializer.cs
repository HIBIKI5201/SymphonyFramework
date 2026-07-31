using System.IO;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.System.SaveSystem;
using SymphonyFrameWork.System.ServiceLocate;
using SymphonyFrameWork.Utility;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     起動時に初期化する
    /// </summary>
    [InitializeOnLoad]
    public static class PackageInitializer
    {
        /// <summary> Config、生成enum、Editor用セーブローダーをEditor起動時に整備する。 </summary>
        static PackageInitializer()
        {
            SymphonyConfigManager.AllConfigCheck();
            ApplyServiceLocateLogOptions();
            SymphonyVisualElement.EditorAssetLoader =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>;
            SaveDataRegistry.ConfigureLoaderResolver(ResolveSaveDataLoader);
            EnumInitialize();
            
            AssetDatabase.Refresh();
            
            Debug.Log("Symphony Framework Initialized");
        }

        /// <summary> UserSettingsの設定値をRuntimeのService Locatorログ設定へ注入する。 </summary>
        internal static void ApplyServiceLocateLogOptions()
        {
            SymphonyUserSettingConfig config =
                SymphonyEditorConfigLocator.GetConfig<SymphonyUserSettingConfig>();

            ServiceLocateLogOption.IsSetInstanceLogEnabled =
                config.IsServiceLocatorSetInstanceLogEnabled;
            ServiceLocateLogOption.IsGetInstanceLogEnabled =
                config.IsServiceLocatorGetInstanceLogEnabled;
            ServiceLocateLogOption.IsDestroyInstanceLogEnabled =
                config.IsServiceLocatorDestroyInstanceLogEnabled;
        }

        /// <summary> 自動生成enumとAssembly Definitionの配置・参照を整備する。 </summary>
        private static void EnumInitialize()
        {
            //Enumファイルが無ければ生成する
            if (!Directory.Exists(EditorSymphonyConstant.ENUM_PATH))
            {
                AutoEnumGenerator.SceneListEnumGenerate();
                AutoEnumGenerator.TagsEnumGenerate();
                AutoEnumGenerator.LayersEnumGenerate();
                AutoEnumGenerator.AudioEnumGenerate();
            }
            
            //パッケージ内のEnumを消す
            var path = $"Packages/{SymphonyConstant.SYMPHONY_PACKAGE}/Enum"; //パッケージ内のEnumフォルダ
            if (Directory.Exists(path))
            {
                FileUtil.DeleteFileOrDirectory(path);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
                AssetDatabase.Refresh();
            }
            
            var enumAsmdefPath = EditorSymphonyConstant.ENUM_PATH + "/SymphonyFrameWork.Enum.asmdef";
            var mainAsmdefPath = EditorSymphonyConstant.FRAMEWORK_PATH + "/SymphonyFrameWork.asmdef";
            AssemblyGenerator.AddAsssemblyReference(mainAsmdefPath, enumAsmdefPath);
        }

        /// <summary>
        ///     Editor上の現在のConfigからセーブデータローダーを解決する。
        /// </summary>
        /// <returns> Configで選択されたローダー。未設定の場合は既定のローダー。 </returns>
        private static SaveDataLoader ResolveSaveDataLoader()
        {
            SaveSystemConfig config =
                SymphonyConfigLocator.GetConfig<SaveSystemConfig>();
            return config?.Loader ?? new JsonUtilitySaveDataLoader();
        }
    }
}
