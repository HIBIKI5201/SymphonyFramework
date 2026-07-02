using SymphonyFrameWork.Core;
using UnityEditor;
using UnityEngine;
namespace SymphonyFrameWork.Editor
{
    [FilePath(EditorSymphonyConstant.PROJCET_SETTING_FILE_PATH
        + nameof(AssetStoreToolsPackagerData) + ".asset",
        FilePathAttribute.Location.ProjectFolder)]
    public class AssetStoreToolsPackagerData : ScriptableSingleton<AssetStoreToolsPackagerData>
    {
        public static string AssetStoreToolsPath => instance._assetStoreToolsPath;
        public static string ExportedPackagesPath => instance.exportedPackagesPath;

        public static void SetAssetStoreToolsPath(string path)
        {
            if (instance._assetStoreToolsPath != path)
            {
                instance._assetStoreToolsPath = path;
                EditorUtility.SetDirty(instance);
                AssetDatabase.SaveAssets();
                Save();
            }
        }
        public static void SetExportedPackagesPath(string path)
        {
            if (instance.exportedPackagesPath != path)
            {
                instance.exportedPackagesPath = path;
                EditorUtility.SetDirty(instance);
                AssetDatabase.SaveAssets();
                Save();
            }
        }

        [SerializeField] private string exportedPackagesPath = "ExportedPackages";
        [SerializeField] private string _assetStoreToolsPath = EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH;

        private static void Save() => instance.Save(true);
    }
}
