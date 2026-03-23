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
        public class PackageDirectoryInfo
        {
            public string Path;
            public string Name;
            public bool IsIgnored;
        }

        /// <summary>
        ///     AssetStoreToolsフォルダをパッケージ化してExportedPackagesフォルダに保存します。
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(ExportAssetStoreToolsFolder), priority = 100)]
        public static void ExportAssetStoreToolsFolder()
        {
            AssetStoreToolsPackageWindow.ShowWindow();
        }

        /// <summary>
        ///     パッケージ対象のディレクトリ一覧を取得します。無視リストのフィルタリングもここで行います。
        /// </summary>
        public static List<PackageDirectoryInfo> GetPackageDirectories()
        {
            List<PackageDirectoryInfo> results = new();

            if (!AssetDatabase.IsValidFolder(EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH))
            {
                return results;
            }

            // 無視ファイルの確認と作成。
            HashSet<string> ignoredNames = GetIgnoredNames();

            // ディレクトリの取得と情報の生成。
            string[] dirs = Directory.GetDirectories(EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH);
            foreach (string dir in dirs)
            {
                string name = Path.GetFileName(dir);
                bool isIgnored = ignoredNames.Contains(name);

                results.Add(new PackageDirectoryInfo
                {
                    Path = dir.Replace("\\", "/"),
                    Name = name,
                    IsIgnored = isIgnored
                });
            }

            return results;
        }

        private static HashSet<string> GetIgnoredNames()
        {
            HashSet<string> ignoredNames = new HashSet<string>();
            if (!File.Exists(EditorSymphonyConstant.ASSET_STORE_TOOLS_IGNORE_FILE))
            {
                File.WriteAllText(EditorSymphonyConstant.ASSET_STORE_TOOLS_IGNORE_FILE, "# Write folder names to ignore (one per line)\n");
                AssetDatabase.Refresh();
            }
            else
            {
                string[] lines = File.ReadAllLines(EditorSymphonyConstant.ASSET_STORE_TOOLS_IGNORE_FILE);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                    {
                        ignoredNames.Add(trimmed);
                    }
                }
            }
            return ignoredNames;
        }

        public static void Export(string[] directories, bool createCombinedPackage = false)
        {
            // ExportedPackages フォルダがなければ作成。
            string fullExportDir = Path.Combine(Application.dataPath, "..", EXPORTED_PACKAGES);
            if (!Directory.Exists(fullExportDir))
            {
                Directory.CreateDirectory(fullExportDir);
            }

            // 出力フォルダ名。
            string exportPath = Path.Combine(EXPORTED_PACKAGES, PackageName);
            Directory.CreateDirectory(exportPath);

            if (directories.Length == 0)
            {
                Debug.LogWarning("パッケージ化するフォルダが存在しませんでした。");
                return;
            }

            // 個別のパッケージを出力
            foreach (string dir in directories)
            {
                try
                {
                    // パッケージ化を実行。
                    AssetDatabase.ExportPackage(
                        dir,
                        Path.Combine(exportPath, $"{Path.GetFileName(dir)}.unitypackage"),
                        ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies
                        );
                }
                catch (Exception e)
                {
                    Debug.LogError($"パッケージの出力に失敗しました: {dir}\n{e}");
                }
            }

            // まとめたパッケージを出力
            if (createCombinedPackage)
            {
                try
                {
                    string combinedName = $"AllPackages_{DateTime.Now:yyyyMMdd_HHmmss}.unitypackage";
                    AssetDatabase.ExportPackage(
                        directories,
                        Path.Combine(exportPath, combinedName),
                        ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies
                    );
                    Debug.Log($"[{nameof(AssetStoreToolsPackager)}] 合計パッケージを作成しました: {combinedName}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"合計パッケージの出力に失敗しました\n{e}");
                }
            }

            Debug.Log($"[{nameof(AssetStoreToolsPackager)}]\nパッケージを出力しました\npath : {exportPath}\n\nexported\n{string.Join('\n', directories.Select(d => $"- {Path.GetFileName(d)}"))}");

        }

        private const string EXPORTED_PACKAGES = "ExportedPackages";

        private static string PackageName =>
            $"Export_{Path.GetFileName(EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH)}_{DateTime.Now:yyyyMMdd_HHmmss}";
    }
}
