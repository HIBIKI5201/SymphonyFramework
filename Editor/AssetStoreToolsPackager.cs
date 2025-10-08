using SymphonyFrameWork.Core;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     AssetStoreToolsフォルダをパッケージ化するクラス
    /// </summary>
    public static class AssetStoreToolsPackager
    {
        public const string AssetStoreToolsPath = "Assets/AssetStoreTools";
        public static string PackageName => $"Export_{Path.GetFileName(AssetStoreToolsPath)}_{DateTime.Now:yyyyMMdd_HHmmss}.unitypackage";

        // メニューから実行
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(ExportAssetStoreToolsFolder), priority = 100)]
        public static void ExportAssetStoreToolsFolder()
        {
            // 対象ディレクトリ

            if (!AssetDatabase.IsValidFolder(AssetStoreToolsPath))
            {
                Debug.LogError($"AssetStoreToolsフォルダが存在しません: {AssetStoreToolsPath}");
                return;
            }

            // 出力ファイル名
            string exportPath = Path.Combine("ExportedPackages", PackageName);

            // ExportedPackages フォルダがなければ作成
            string fullExportDir = Path.Combine(Application.dataPath, "..", "ExportedPackages");
            if (!Directory.Exists(fullExportDir))
            {
                Directory.CreateDirectory(fullExportDir);
            }

            // パッケージ化
            AssetDatabase.ExportPackage(
                AssetStoreToolsPath,
                exportPath,
                ExportPackageOptions.Interactive | ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies
            );

            Debug.Log($"パッケージを出力しました: {exportPath}");
        }
    }
}
