using SymphonyFrameWork.Core;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     AssetStoreToolsフォルダをパッケージ化するクラス。
    /// </summary>
    public static class AssetStoreToolsPackager
    {
        /// <summary>
        ///     AssetStoreToolsフォルダをパッケージ化してExportedPackagesフォルダに保存します。
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(ExportAssetStoreToolsFolder), priority = 100)]
        public static void ExportAssetStoreToolsFolder()
        {
            // パッケージ対象ディレクトリをバリデーションチェック。
            if (!AssetDatabase.IsValidFolder(AssetStoreToolsPath))
            {
                Debug.LogError($"AssetStoreToolsフォルダが存在しません: {AssetStoreToolsPath}");
                return;
            }

            // 出力ファイル名。
            string exportPath = Path.Combine(EXPORTED_PACKAGES, PackageName);

            // ExportedPackages フォルダがなければ作成。
            string fullExportDir = Path.Combine(Application.dataPath, "..", EXPORTED_PACKAGES);
            if (!Directory.Exists(fullExportDir))
            {
                Directory.CreateDirectory(fullExportDir);
            }

            // パッケージ化を実行。
            AssetDatabase.ExportPackage(
                AssetStoreToolsPath,
                exportPath,
                ExportPackageOptions.Interactive | ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies
            );

            Debug.Log($"パッケージを出力しました: {exportPath}");
        }

        private const string AssetStoreToolsPath = "Assets/AssetStoreTools";
        private static string PackageName => 
            $"Export_{Path.GetFileName(AssetStoreToolsPath)}_{DateTime.Now:yyyyMMdd_HHmmss}.unitypackage";
        private const string EXPORTED_PACKAGES = "ExportedPackages";
    }
}
