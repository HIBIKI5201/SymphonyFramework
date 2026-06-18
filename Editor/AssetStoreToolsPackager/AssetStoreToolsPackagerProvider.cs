using System.Collections.Generic;
using UnityEditor;

namespace SymphonyFrameWork.Editor.SettingProvider
{
    public class AssetStoreToolsPackagerProvider
    {
        public const string LABEL = "Asset Store Tools Packager";
        public const string SELF_PATH = SymphonySettingProvider.PROVIDER_PATH + LABEL;

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
                keywords = new HashSet<string>(new[] { "asset", "store", "tools", "packager" }),
            };

            return provider;
        }

        private static void IMGUI(string searchContext)
        {
            string assetStoreToolsPath = AssetStoreToolsPackagerData.AssetStoreToolsPath;
            assetStoreToolsPath = EditorGUILayout.TextField("Asset Store Tools Path", assetStoreToolsPath);
            if (assetStoreToolsPath != AssetStoreToolsPackagerData.AssetStoreToolsPath)
            {
                AssetStoreToolsPackagerData.SetAssetStoreToolsPath(assetStoreToolsPath);
            }

            string exportedPackagesPath = AssetStoreToolsPackagerData.ExportedPackagesPath;
            exportedPackagesPath = EditorGUILayout.TextField("Exported Packages Path", exportedPackagesPath);
            if (exportedPackagesPath != AssetStoreToolsPackagerData.ExportedPackagesPath)
            {
                AssetStoreToolsPackagerData.SetExportedPackagesPath(exportedPackagesPath);
            }
        }
    }
}
