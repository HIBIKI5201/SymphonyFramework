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
        /// <summary> パッケージ候補ディレクトリの表示名、パス、除外状態を保持する。 </summary>
        public sealed class PackageDirectoryInfo
        {
            /// <summary> パッケージ対象ディレクトリのパス。 </summary>
            public string Path;

            /// <summary> UIへ表示するディレクトリ名。 </summary>
            public string Name;

            /// <summary> 除外設定に含まれているかを示す。 </summary>
            public bool IsIgnored;
        }

        /// <summary> 個別または統合パッケージの出力形式を表すフラグ。 </summary>
        [Flags]
        public enum PackageModeEnum : byte
        {
            /// <summary> パッケージを出力しない無効な状態。 </summary>
            Nothing = 0,

            /// <summary> 選択ディレクトリを個別のパッケージとして出力する。 </summary>
            Singles = 1 << 0,

            /// <summary> 選択ディレクトリを1つの統合パッケージとして出力する。 </summary>
            Combine = 1 << 1,
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

            if (!AssetDatabase.IsValidFolder(AssetStoreToolsPackagerData.AssetStoreToolsPath))
            {
                return results;
            }

            // 設定ファイルの確認と作成。読み込めない場合は除外設定が失われるため何も返さない。
            AssetStoreToolsPackagerConfig config = AssetStoreToolsPackagerConfigStore.Load();
            if (config == null)
            {
                return results;
            }

            HashSet<string> ignoredNames = new(config.IgnoredDirectories, StringComparer.OrdinalIgnoreCase);

            // ディレクトリの取得と情報の生成。
            string[] dirs = Directory.GetDirectories(AssetStoreToolsPackagerData.AssetStoreToolsPath);
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

        /// <summary> 指定ディレクトリを選択された形式で出力し、必要に応じてZIP化する。 </summary>
        public static void Export(string[] directories, PackageModeEnum mode, bool createZip = false, bool usedDependencies = false)
        {
            if (directories.Length == 0)
            {
                Debug.LogWarning("パッケージ化するフォルダが存在しませんでした。");
                return;
            }

            AssetStoreToolsPackagerConfig config = AssetStoreToolsPackagerConfigStore.Load();
            if (config == null)
            {
                Debug.LogError($"[{nameof(AssetStoreToolsPackager)}]\n設定を読み込めなかったためパッケージを出力しませんでした。");
                return;
            }

            var context = new AssetStoreToolsPackageContext(
                PACKAGE_NAME,
                AssetStoreToolsPackagerData.ExportedPackagesPath,
                directories
            );

            // 出力フォルダ作成
            if (!Directory.Exists(context.ExportFullPath))
            {
                Directory.CreateDirectory(context.ExportFullPath);
            }


            HashSet<string> usedAssetPaths = null;
            if (usedDependencies)
            {
                string astPath = AssetStoreToolsPackagerData.AssetStoreToolsPath;
                usedAssetPaths = GetProjectUsedDependencies(astPath);
            }

            if ((mode & PackageModeEnum.Singles) != 0)
            {
                ExportPackage(context, config.ForceIncludeExtensions, usedAssetPaths);
            }

            if ((mode & PackageModeEnum.Combine) != 0)
            {
                CreateCombinedPackage(context, config.ForceIncludeExtensions, usedAssetPaths);
            }

            if (createZip)
            {
                CreateZip(context);
            }

            Debug.Log($"[{nameof(AssetStoreToolsPackager)}]\nパッケージを出力しました\npath : {context.ExportLocalPath}");
        }


        private const string PACKAGE_NAME = "AssetStoreToolsPackage";

        /// <summary>
        ///     個別のパッケージ生成。
        /// </summary>
        /// <param name="context"> 出力対象と出力先を保持するパッケージコンテキスト。 </param>
        /// <param name="forceIncludeExtensions"> 依存関係に関わらず含める拡張子の一覧。 </param>
        /// <param name="usedAssetPaths"> 使用中アセットだけを出力する場合のパス集合。 </param>
        private static void ExportPackage(
            AssetStoreToolsPackageContext context,
            IReadOnlyList<string> forceIncludeExtensions,
            HashSet<string> usedAssetPaths = null)
        {
            foreach (string dir in context.ExportDirectories)
            {
                try
                {
                    string[] exportFiles;
                    ExportPackageOptions options;

                    if (usedAssetPaths != null)
                    {
                        exportFiles = CollectExportAssets(dir, usedAssetPaths, forceIncludeExtensions);
                        options = ExportPackageOptions.Default;

                        if (exportFiles.Length == 0)
                        {
                            Debug.LogWarning($"使用中アセットなし: {dir}");
                            continue;
                        }
                    }
                    else
                    {
                        exportFiles = new[] { dir };
                        options = ExportPackageOptions.Recurse;
                    }

                    AssetDatabase.ExportPackage(
                        exportFiles,
                        Path.Combine(
                            context.ExportLocalPath,
                            $"{Path.GetFileName(dir)}.unitypackage"),
                        options
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
        /// <param name="context"> 出力対象と出力先を保持するパッケージコンテキスト。 </param>
        /// <param name="forceIncludeExtensions"> 依存関係に関わらず含める拡張子の一覧。 </param>
        /// <param name="usedAssetPaths"> 使用中アセットだけを出力する場合のパス集合。 </param>
        private static void CreateCombinedPackage(
            in AssetStoreToolsPackageContext context,
            IReadOnlyList<string> forceIncludeExtensions,
            HashSet<string> usedAssetPaths = null)
        {
            try
            {
                string combinedName =
                    $"AllPackages_{context.DateTime:yyyyMMdd_HHmmss}.unitypackage";

                string[] exportFiles;
                ExportPackageOptions options;

                if (usedAssetPaths != null)
                {
                    exportFiles = context.ExportDirectories
                        .SelectMany(dir => CollectExportAssets(dir, usedAssetPaths, forceIncludeExtensions))
                        .Distinct()
                        .ToArray();
                    options = ExportPackageOptions.Default;

                    if (exportFiles.Length == 0)
                    {
                        Debug.LogWarning("使用中アセットが存在しないため統合パッケージを作成しませんでした。");
                        return;
                    }
                }
                else
                {
                    exportFiles = context.ExportDirectories;
                    options = ExportPackageOptions.Recurse;
                }

                AssetDatabase.ExportPackage(
                    exportFiles,
                    Path.Combine(context.ExportLocalPath, combinedName),
                    options
                );

                Debug.Log($"合成パッケージ作成: {combinedName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"合計パッケージの出力に失敗\n{e}");
            }
        }

        /// <summary>
        ///     指定フォルダをZIP化する
        /// </summary>
        /// <param name="context"> 圧縮対象と出力先を保持するパッケージコンテキスト。 </param>
        private static void CreateZip(in AssetStoreToolsPackageContext context)
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

        /// <summary>
        /// プロジェクト内の全アセット内で一つでも依存している（＝使用している）アセットのパス一覧を取得する
        /// </summary>
        private static HashSet<string> GetProjectUsedDependencies(string excludedRootPath)
        {
            HashSet<string> usedPaths = new();

            // プロジェクト内のすべての一般アセット（Assetsフォルダ以下）を検索
            string[] allAssetGuids = AssetDatabase.FindAssets("", new[] { "Assets" });

            string normalizedExcludedRootPath = excludedRootPath
                ?.Replace("\\", "/")
                .TrimEnd('/');

            foreach (string guid in allAssetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // AssetStoreToolsは除外して依存関係を追う
                if (!string.IsNullOrEmpty(normalizedExcludedRootPath)
                    && (path.Equals(normalizedExcludedRootPath, StringComparison.Ordinal)
                        || path.StartsWith(normalizedExcludedRootPath + "/", StringComparison.Ordinal)))
                {
                    continue;
                }

                // そのアセットが依存しているリソースをすべて取得
                string[] dependencies = AssetDatabase.GetDependencies(path, recursive: true);

                foreach (string dependency in dependencies)
                {
                    usedPaths.Add(dependency);
                }
            }

            return usedPaths;
        }

        /// <summary>
        ///     指定ディレクトリから出力対象のアセットを収集する。
        /// </summary>
        /// <remarks>
        ///     ファイルシステムではなくAssetDatabaseを走査する。
        ///     .bundleや.frameworkはUnityが単一アセットとして扱うため、
        ///     ファイル列挙では中身のファイルしか拾えず、ExportPackageへ渡しても出力されない。
        /// </remarks>
        /// <param name="dir"> 収集対象のディレクトリ。 </param>
        /// <param name="usedAssetPaths"> プロジェクト内で使用中のアセットのパス集合。 </param>
        /// <param name="forceIncludeExtensions"> 依存関係に関わらず含める拡張子の一覧。 </param>
        /// <returns> パスの昇順で並んだ出力対象アセットのパス。 </returns>
        private static string[] CollectExportAssets(
            string dir,
            HashSet<string> usedAssetPaths,
            IReadOnlyList<string> forceIncludeExtensions)
        {
            return AssetDatabase.FindAssets(string.Empty, new[] { dir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct()
                .Where(path => IsExportTarget(path, usedAssetPaths, forceIncludeExtensions))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        ///     アセットを出力対象に含めるか判定する。
        /// </summary>
        /// <returns> 強制包含の拡張子に一致するか、使用中アセットであればtrue。 </returns>
        private static bool IsExportTarget(
            string path,
            HashSet<string> usedAssetPaths,
            IReadOnlyList<string> forceIncludeExtensions)
        {
            bool isForceIncluded = HasForceIncludeExtension(path, forceIncludeExtensions);

            // 通常のフォルダは出力対象にしない。
            // .bundle等のフォルダ形式アセットは、IsValidFolderの結果に関わらず拡張子一致で残す。
            if (!isForceIncluded && AssetDatabase.IsValidFolder(path))
            {
                return false;
            }

            return isForceIncluded || usedAssetPaths.Contains(path);
        }

        /// <summary>
        ///     パスの拡張子が強制包含の一覧に含まれるか判定する。
        /// </summary>
        /// <param name="path"> 判定するアセットのパス。 </param>
        /// <param name="forceIncludeExtensions"> 強制包含する拡張子の一覧。 </param>
        /// <returns> 大文字小文字を無視して一致する拡張子があればtrue。 </returns>
        internal static bool HasForceIncludeExtension(
            string path,
            IReadOnlyList<string> forceIncludeExtensions)
        {
            if (forceIncludeExtensions == null || forceIncludeExtensions.Count == 0)
            {
                return false;
            }

            string extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            for (int i = 0; i < forceIncludeExtensions.Count; i++)
            {
                if (string.Equals(extension, forceIncludeExtensions[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}