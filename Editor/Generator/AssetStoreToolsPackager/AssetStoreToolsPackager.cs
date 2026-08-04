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

            /// <summary>
            ///     選択ディレクトリを1つの統合パッケージとして出力する。
            /// </summary>
            /// <remarks>
            ///     統合パッケージはディレクトリ単位で取り出せないため、差分インポートの単位にならない。
            ///     この形式で出力してもPackageManifest.jsonは作られない。
            /// </remarks>
            [Obsolete(
                "差分インポートに対応しないため廃止予定です。" + nameof(Singles) + "を使用してください。",
                error: false)]
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
            AssetStoreToolsPackagePlan plan = CreatePlan(directories, mode, createZip, usedDependencies);
            if (plan == null)
            {
                return;
            }

            Export(plan);
        }

        /// <summary>
        ///     出力内容を確定した計画を組み立てる。この時点ではファイルを出力しない。
        /// </summary>
        /// <param name="directories"> 出力対象のディレクトリ。 </param>
        /// <param name="mode"> 個別出力と統合出力の指定。 </param>
        /// <param name="createZip"> 出力後にZIP化するか。 </param>
        /// <param name="usedDependencies"> 使用中アセットと強制包含拡張子だけへ絞るか。 </param>
        /// <returns> 出力計画。対象が無い場合や設定を読み込めない場合はnull。 </returns>
        internal static AssetStoreToolsPackagePlan CreatePlan(
            string[] directories,
            PackageModeEnum mode,
            bool createZip,
            bool usedDependencies)
        {
            if (directories == null || directories.Length == 0)
            {
                Debug.LogWarning("パッケージ化するフォルダが存在しませんでした。");
                return null;
            }

            AssetStoreToolsPackagerConfig config = AssetStoreToolsPackagerConfigStore.Load();
            if (config == null)
            {
                Debug.LogError($"[{nameof(AssetStoreToolsPackager)}]\n設定を読み込めなかったためパッケージを出力しませんでした。");
                return null;
            }

            HashSet<string> usedAssetPaths = usedDependencies
                ? GetProjectUsedDependencies(AssetStoreToolsPackagerData.AssetStoreToolsPath)
                : null;

            // 読み込めない場合もリビジョン0として出力は続行する。
            // 差分インポート側で常に新規と判定されるだけで、既存の出力機能は損なわれない。
            AssetStoreToolsVersionLog versionLog = AssetStoreToolsVersionLogStore.Load();

            List<AssetStoreToolsPackagePlanEntry> entries = new();
            foreach (string dir in directories)
            {
                string name = Path.GetFileName(dir);
                string[] assetPaths = usedAssetPaths != null
                    ? CollectExportAssets(dir, usedAssetPaths, config.ForceIncludeExtensions)
                    : CollectAllAssets(dir);

                entries.Add(new AssetStoreToolsPackagePlanEntry
                {
                    DirectoryPath = dir,
                    Name = name,
                    Version = versionLog?.GetVersion(name) ?? 0,
                    AssetPaths = assetPaths,
                });
            }

            return new AssetStoreToolsPackagePlan
            {
                Mode = mode,
                CreateZip = createZip,
                UsedDependencies = usedDependencies,
                Entries = entries,
            };
        }

        /// <summary>
        ///     確定済みの計画に従ってパッケージを出力し、必要に応じてZIP化する。
        /// </summary>
        /// <param name="plan"> 出力する計画。 </param>
        internal static void Export(AssetStoreToolsPackagePlan plan)
        {
            if (plan == null || plan.Entries.Count == 0)
            {
                Debug.LogWarning("パッケージ化するフォルダが存在しませんでした。");
                return;
            }

            var context = new AssetStoreToolsPackageContext(
                PACKAGE_NAME,
                AssetStoreToolsPackagerData.ExportedPackagesPath,
                plan.Entries.Select(entry => entry.DirectoryPath).ToArray()
            );

            // 出力フォルダ作成
            if (!Directory.Exists(context.ExportFullPath))
            {
                Directory.CreateDirectory(context.ExportFullPath);
            }

            // 出力時バージョンを先に書き、AssetDatabaseへ載せてからパッケージ化する。
            // Refreshを省くと新規ファイルがAssetDatabaseに載らず、Recurseでも明示指定でも出力されない。
            WriteExportedVersions(plan);
            AssetDatabase.Refresh();

            if ((plan.Mode & PackageModeEnum.Singles) != 0)
            {
                ExportPackage(context, plan);
            }

            // 非推奨のCombineは削除まで動作を維持する必要があるため、
            // 廃止予定の警告をここでは抑止する。利用側の指定に対しては警告が出る。
#pragma warning disable CS0618
            if ((plan.Mode & PackageModeEnum.Combine) != 0)
            {
                CreateCombinedPackage(context, plan);
            }
#pragma warning restore CS0618

            // マニフェストは個別出力のときだけ書く。ZIPへ含めるためZIP化より前に書く。
            if ((plan.Mode & PackageModeEnum.Singles) != 0)
            {
                WriteManifest(context, plan);
            }
#pragma warning disable CS0618
            else if ((plan.Mode & PackageModeEnum.Combine) != 0)
            {
                Debug.LogWarning(
                    $"[{nameof(AssetStoreToolsPackager)}]\n"
                    + "統合パッケージだけの出力は差分インポートの対象になりません。"
                    + "ディレクトリ単位で取り出せないためです。"
                    + $"\n{nameof(PackageModeEnum)}.{nameof(PackageModeEnum.Combine)}は廃止予定です。"
                    + $"{nameof(PackageModeEnum.Singles)}を使用してください。");
            }
#pragma warning restore CS0618

            if (plan.CreateZip)
            {
                CreateZip(context);
            }

            Debug.Log($"[{nameof(AssetStoreToolsPackager)}]\nパッケージを出力しました\npath : {context.ExportLocalPath}");
        }


        private const string PACKAGE_NAME = "AssetStoreToolsPackage";

        /// <summary>
        ///     出力対象アセットへ出力時バージョンファイルのパスを加える。
        /// </summary>
        /// <remarks>
        ///     「Used Dependencies」の経路では計画の一覧がそのままExportPackageの引数になるため、
        ///     ここで加えないとバージョンファイルがパッケージへ含まれない。
        ///     計画そのものへは加えない。加えると空のディレクトリを検出できなくなる。
        /// </remarks>
        /// <param name="assetPaths"> 収集済みの出力対象アセット。 </param>
        /// <param name="directoryPath"> 出力単位となるディレクトリのパス。 </param>
        /// <returns> パスの昇順で並んだ出力対象アセットのパス。 </returns>
        private static string[] AppendExportedVersionPath(
            IEnumerable<string> assetPaths,
            string directoryPath)
        {
            string exportedVersionPath = BuildExportedVersionPath(directoryPath);

            return assetPaths
                .Append(exportedVersionPath)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary> ディレクトリ直下の出力時バージョンファイルのパスを組み立てる。 </summary>
        /// <param name="directoryPath"> 出力単位となるディレクトリのパス。 </param>
        /// <returns> スラッシュ区切りのアセットパス。 </returns>
        private static string BuildExportedVersionPath(string directoryPath)
            => directoryPath.Replace("\\", "/").TrimEnd('/')
               + "/" + EditorSymphonyConstant.ASSET_STORE_TOOLS_EXPORTED_VERSION_FILE_NAME;

        /// <summary>
        ///     計画中の各ディレクトリへ出力時バージョンを書き出す。
        /// </summary>
        /// <remarks>
        ///     書き込みに失敗しても出力は続行する。バージョンファイルの無いパッケージは
        ///     インポート側で常に新規と判定されるだけで、既存の出力機能は損なわれない。
        /// </remarks>
        /// <param name="plan"> 出力する計画。 </param>
        private static void WriteExportedVersions(AssetStoreToolsPackagePlan plan)
        {
            foreach (AssetStoreToolsPackagePlanEntry entry in plan.Entries)
            {
                AssetStoreToolsVersionLogStore.TryWriteExportedVersion(
                    entry.DirectoryPath,
                    entry.Name,
                    entry.Version);
            }
        }

        /// <summary>
        ///     出力先フォルダへ、個別出力したパッケージ一覧のマニフェストを書き出す。
        /// </summary>
        /// <param name="context"> 出力先を保持するパッケージコンテキスト。 </param>
        /// <param name="plan"> 出力した計画。 </param>
        private static void WriteManifest(
            in AssetStoreToolsPackageContext context,
            AssetStoreToolsPackagePlan plan)
        {
            var manifest = new AssetStoreToolsPackageManifest
            {
                ExportedAt = AssetStoreToolsVersionLog.CreateTimestamp(),
                Packages = plan.Entries
                    .Select(entry => new AssetStoreToolsPackageManifestEntry
                    {
                        Name = entry.Name,
                        Version = entry.Version,
                        FileName = $"{entry.Name}.unitypackage",
                    })
                    .ToList(),
            };

            AssetStoreToolsVersionLogStore.TryWriteManifest(context.ExportFullPath, manifest);
        }

        /// <summary>
        ///     個別のパッケージ生成。
        /// </summary>
        /// <param name="context"> 出力対象と出力先を保持するパッケージコンテキスト。 </param>
        /// <param name="plan"> 出力内容を確定した計画。 </param>
        private static void ExportPackage(
            AssetStoreToolsPackageContext context,
            AssetStoreToolsPackagePlan plan)
        {
            foreach (AssetStoreToolsPackagePlanEntry entry in plan.Entries)
            {
                try
                {
                    string[] exportFiles;
                    ExportPackageOptions options;

                    if (plan.UsedDependencies)
                    {
                        if (entry.AssetPaths.Count == 0)
                        {
                            Debug.LogWarning($"使用中アセットなし: {entry.DirectoryPath}");
                            continue;
                        }

                        // 出力時バージョンは計画に含めず、ここで加える。
                        exportFiles = AppendExportedVersionPath(entry.AssetPaths, entry.DirectoryPath);
                        options = ExportPackageOptions.Default;
                    }
                    else
                    {
                        // 丸ごと出力する経路。計画のAssetPathsは提示用で、出力はディレクトリ単位で行う。
                        exportFiles = new[] { entry.DirectoryPath };
                        options = ExportPackageOptions.Recurse;
                    }

                    AssetDatabase.ExportPackage(
                        exportFiles,
                        Path.Combine(
                            context.ExportLocalPath,
                            $"{entry.Name}.unitypackage"),
                        options
                    );
                }
                catch (Exception e)
                {
                    Debug.LogError($"パッケージの出力に失敗しました: {entry.DirectoryPath}\n{e}");
                }
            }
        }

        /// <summary>
        ///     連結されたパッケージ生成。
        /// </summary>
        /// <param name="context"> 出力対象と出力先を保持するパッケージコンテキスト。 </param>
        /// <param name="plan"> 出力内容を確定した計画。 </param>
        private static void CreateCombinedPackage(
            in AssetStoreToolsPackageContext context,
            AssetStoreToolsPackagePlan plan)
        {
            try
            {
                string combinedName =
                    $"AllPackages_{context.DateTime:yyyyMMdd_HHmmss}.unitypackage";

                string[] exportFiles;
                ExportPackageOptions options;

                if (plan.UsedDependencies)
                {
                    // 空判定は出力時バージョンを加える前に行う。加えた後では常に非空になる。
                    if (plan.Entries.All(entry => entry.AssetPaths.Count == 0))
                    {
                        Debug.LogWarning("使用中アセットが存在しないため統合パッケージを作成しませんでした。");
                        return;
                    }

                    exportFiles = plan.Entries
                        .SelectMany(entry =>
                            AppendExportedVersionPath(entry.AssetPaths, entry.DirectoryPath))
                        .Distinct()
                        .ToArray();
                    options = ExportPackageOptions.Default;
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
        ///     指定ディレクトリ配下の全アセットを収集する。
        /// </summary>
        /// <remarks>
        ///     丸ごと出力する経路で、何が含まれるかを提示するために使う。
        /// </remarks>
        /// <param name="dir"> 収集対象のディレクトリ。 </param>
        /// <returns> パスの昇順で並んだアセットのパス。フォルダ自体は含まない。 </returns>
        private static string[] CollectAllAssets(string dir)
        {
            return AssetDatabase.FindAssets(string.Empty, new[] { dir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path))
                .Distinct()
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