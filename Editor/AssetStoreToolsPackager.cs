using SymphonyFrameWork.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (!AssetDatabase.IsValidFolder(EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH))
            {
                Debug.LogError($"AssetStoreToolsフォルダが存在しません: {EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH}");
                return;
            }

            // ExportedPackages フォルダがなければ作成。
            string fullExportDir = Path.Combine(Application.dataPath, "..", EXPORTED_PACKAGES);
            if (!Directory.Exists(fullExportDir))
            {
                Directory.CreateDirectory(fullExportDir);
            }

            // 出力フォルダ名。
            string exportPath = Path.Combine(EXPORTED_PACKAGES, PackageName);
            Directory.CreateDirectory(exportPath);

            // アセットごとのパスを生成。
            string[] directories =
                Directory.GetDirectories(EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH);

            foreach (string dir in directories)
            {
                // パッケージ化を実行。
                AssetDatabase.ExportPackage(
                    dir,
                    $"{exportPath}/{Path.GetFileName(dir)}.unitypackage",
                    ExportPackageOptions.Interactive | ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies
                );
            }

            Debug.Log($"パッケージを出力しました: {exportPath}");
        }

        private static string PackageName => 
            $"Export_{Path.GetFileName(EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH)}_{DateTime.Now:yyyyMMdd_HHmmss}";
        private const string EXPORTED_PACKAGES = "ExportedPackages";
    }
}
