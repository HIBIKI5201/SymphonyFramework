using SymphonyFrameWork.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

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
        public static IReadOnlyList<PackageDirectoryInfo> GetPackageDirectories()
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

        public static void Export(string[] directories, bool createCombinedPackage = false, bool createZip = false)
        {
            if (directories.Length == 0)
            {
                Debug.LogWarning("パッケージ化するフォルダが存在しませんでした。");
                return;
            }

            var context = new AssetStoreToolsPakcageContext(
                PACKAGE_NAME,
                EXPORTED_PACKAGES,
                directories
            );

            // 出力フォルダ作成
            if (!Directory.Exists(context.ExportFullPath))
            {
                Directory.CreateDirectory(context.ExportFullPath);
            }

            ExportPackage(context);

            if (createCombinedPackage)
            {
                CreateCombinedPackage(context);
            }

            if (createZip)
            {
                CreateZip(context);
            }

            Debug.Log($"[{nameof(AssetStoreToolsPackager)}]\nパッケージを出力しました\npath : {context.ExportLocalPath}");
        }

        private const string EXPORTED_PACKAGES = "ExportedPackages";
        private const string PACKAGE_NAME = "AssetStoreToolsPackage";

        /// <summary>
        ///     個別のパッケージ生成。
        /// </summary>
        /// <param name="context"></param>
        private static void ExportPackage(AssetStoreToolsPakcageContext context)
        {
            foreach (string dir in context.ExportDirectories)
            {
                try
                {
                    AssetDatabase.ExportPackage(
                        dir,
                        Path.Combine(context.ExportLocalPath, $"{Path.GetFileName(dir)}.unitypackage"),
                        ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies
                    );
                }
                catch (Exception e)
                {
                    Debug.LogError($"パッケージの出力に失敗しました: {dir}\n{e}");
                }
            }
        }

        /// <summary>
        ///     連結されたパッケージ生成。
        /// </summary>
        /// <param name="context"></param>
        private static void CreateCombinedPackage(AssetStoreToolsPakcageContext context)
        {
            try
            {
                string combinedName = $"AllPackages_{context.DateTime:yyyyMMdd_HHmmss}.unitypackage";

                AssetDatabase.ExportPackage(
                    context.ExportDirectories,
                    Path.Combine(context.ExportLocalPath, combinedName),
                    ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies
                );

                Debug.Log($"合計パッケージ作成: {combinedName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"合計パッケージの出力に失敗\n{e}");
            }
        }

        /// <summary>
        ///     指定フォルダをZIP化する
        /// </summary>
        /// <param name="sourceDirectory">圧縮対象フォルダ（相対 or 絶対）</param>
        /// <param name="zipFilePath">出力ZIPパス（.zip含む）</param>
        private static void CreateZip(AssetStoreToolsPakcageContext context)
        {
            try
            {
                string zipFullPath = Path.Combine(context.ExportRoot, $"{context.PackageName}.zip");

                if (!Directory.Exists(context.ExportFullPath))
                {
                    Debug.LogError($"ZIP対象フォルダが存在しません: {context.ExportFullPath}");
                    return;
                }

                if (File.Exists(zipFullPath))
                {
                    File.Delete(zipFullPath);
                }

                ZipFile.CreateFromDirectory(
                    context.ExportFullPath,
                    zipFullPath,
                    CompressionLevel.Optimal,
                    true
                );

                Debug.Log($"ZIP作成完了: {zipFullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"ZIP作成失敗\n{e}");
            }
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
    }
}
