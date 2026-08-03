using System.IO;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.System.SaveSystem;
using SymphonyFrameWork.System.ServiceLocate;
using SymphonyFrameWork.Utility;

using UnityEditor;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Config、生成enum、Editor用依存を初期化する。
    /// </summary>
    internal static class PackageInitializer
    {
        /// <summary>
        ///     Config、生成enum、Editor用セーブローダーを整備する。
        /// </summary>
        /// <returns> AssetDatabaseの更新が必要な変更を行った場合はtrue。 </returns>
        internal static bool Initialize()
        {
            bool hasAssetChanges = SymphonyConfigManager.AllConfigCheck();
            ApplyServiceLocateLogOptions();
            SymphonyVisualElement.EditorAssetLoader =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>;
            SaveStore.ConfigureLoaderResolver(ResolveSaveDataLoader);
            hasAssetChanges |= EnumInitialize();

            return hasAssetChanges;
        }

        /// <summary> Editor用に注入したアセットローダーを解除する。 </summary>
        internal static void Shutdown()
        {
            SymphonyVisualElement.EditorAssetLoader = null;
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

        /// <summary>
        ///     自動生成enumとAssembly Definitionの配置・参照を整備する。
        /// </summary>
        /// <returns> AssetDatabaseの更新が必要な変更を行った場合はtrue。 </returns>
        private static bool EnumInitialize()
        {
            bool hasAssetChanges = false;

            //Enumファイルが無ければ生成する
            if (!Directory.Exists(EditorSymphonyConstant.ENUM_PATH))
            {
                AutoEnumGenerator.SceneListEnumGenerate(false);
                AutoEnumGenerator.TagsEnumGenerate(false);
                AutoEnumGenerator.LayersEnumGenerate(false);
                AutoEnumGenerator.AudioEnumGenerate(false);
                hasAssetChanges = true;
            }

            //パッケージ内のEnumを消す
            var path = $"Packages/{SymphonyConstant.SYMPHONY_PACKAGE}/Enum"; //パッケージ内のEnumフォルダ
            if (Directory.Exists(path))
            {
                FileUtil.DeleteFileOrDirectory(path);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
                hasAssetChanges = true;
            }

            var enumAsmdefPath = EditorSymphonyConstant.ENUM_PATH + "/SymphonyFrameWork.Enum.asmdef";
            var mainAsmdefPath = EditorSymphonyConstant.FRAMEWORK_PATH + "/SymphonyFrameWork.asmdef";
            string previousMainAsmdef = File.Exists(mainAsmdefPath)
                ? File.ReadAllText(mainAsmdefPath)
                : null;
            AssemblyGenerator.AddAsssemblyReference(mainAsmdefPath, enumAsmdefPath);
            hasAssetChanges |= File.Exists(mainAsmdefPath) &&
                               previousMainAsmdef != File.ReadAllText(mainAsmdefPath);

            return hasAssetChanges;
        }

        /// <summary>
        ///     Editor上の現在のConfigからセーブデータローダーを解決する。
        /// </summary>
        /// <returns> Configで選択されたローダー。未設定の場合は既定のローダー。 </returns>
        private static SaveDataLoaderStrategy ResolveSaveDataLoader()
        {
            SaveSystemConfig config =
                SymphonyConfigLocator.GetConfig<SaveSystemConfig>();
            return config?.Loader ?? new JsonUtilitySaveDataLoaderStrategy();
        }
    }
}
