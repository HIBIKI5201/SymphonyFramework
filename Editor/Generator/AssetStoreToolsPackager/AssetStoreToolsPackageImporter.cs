using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     出力済みパッケージの一覧取得と、選択したパッケージのインポートを担う。
    /// </summary>
    internal static class AssetStoreToolsPackageImporter
    {
        /// <summary>
        ///     出力先フォルダにある出力済みフォルダを新しい順に取得する。
        /// </summary>
        /// <returns> 出力済みフォルダの絶対パス。出力先が無い場合は空。 </returns>
        internal static IReadOnlyList<string> GetExportDirectories()
        {
            string exportRoot = GetExportRootPath();
            if (string.IsNullOrEmpty(exportRoot) || !Directory.Exists(exportRoot))
            {
                return Array.Empty<string>();
            }

            return Directory
                .GetDirectories(exportRoot)
                .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        ///     パッケージ対象フォルダ配下から、現在導入されているリビジョンを集める。
        /// </summary>
        /// <returns> ディレクトリ名からリビジョンへの対応。未導入のディレクトリは含まない。 </returns>
        internal static IReadOnlyDictionary<string, int> LoadLocalVersions()
        {
            Dictionary<string, int> versions = new(StringComparer.OrdinalIgnoreCase);

            string root = AssetStoreToolsPackagerData.AssetStoreToolsPath;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return versions;
            }

            foreach (string directory in Directory.GetDirectories(root))
            {
                if (AssetStoreToolsVersionLogStore.TryReadExportedVersion(
                        directory.Replace("\\", "/"),
                        out int version))
                {
                    versions[Path.GetFileName(directory)] = version;
                }
            }

            return versions;
        }

        /// <summary>
        ///     出力済みフォルダのマニフェストからインポート候補を組み立てる。
        /// </summary>
        /// <param name="exportDirectoryPath"> 対象の出力済みフォルダ。 </param>
        /// <returns> インポート候補。マニフェストが無い場合は空。 </returns>
        internal static IReadOnlyList<AssetStoreToolsImportCandidate> BuildCandidates(
            string exportDirectoryPath)
        {
            AssetStoreToolsPackageManifest manifest =
                AssetStoreToolsVersionLogStore.LoadManifest(exportDirectoryPath);

            return AssetStoreToolsImportPlanner.Build(manifest, LoadLocalVersions());
        }

        /// <summary>
        ///     選択されたパッケージを順にインポートする。
        /// </summary>
        /// <remarks>
        ///     インポートはUnityがアセットを取り込むため、完了後にパッケージ同梱の
        ///     ExportedVersion.jsonが更新され、次回の比較へ反映される。
        /// </remarks>
        /// <param name="exportDirectoryPath"> パッケージが置かれた出力済みフォルダ。 </param>
        /// <param name="candidates"> インポート候補。選択されているものだけを取り込む。 </param>
        /// <returns> インポートを開始した件数。 </returns>
        internal static int Import(
            string exportDirectoryPath,
            IEnumerable<AssetStoreToolsImportCandidate> candidates)
        {
            if (string.IsNullOrEmpty(exportDirectoryPath) || candidates == null)
            {
                return 0;
            }

            int importedCount = 0;
            foreach (AssetStoreToolsImportCandidate candidate in candidates)
            {
                if (candidate == null || !candidate.IsSelected)
                {
                    continue;
                }

                string packagePath = Path.Combine(exportDirectoryPath, candidate.FileName);
                if (!File.Exists(packagePath))
                {
                    Debug.LogError(
                        $"{LOG_PREFIX}\nパッケージが見つかりませんでした: {packagePath}");
                    continue;
                }

                try
                {
                    AssetDatabase.ImportPackage(packagePath, interactive: false);
                    importedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"{LOG_PREFIX}\nパッケージのインポートに失敗しました: {packagePath}\n{e}");
                }
            }

            if (importedCount > 0)
            {
                Debug.Log($"{LOG_PREFIX}\n{importedCount}件のパッケージをインポートしました。");
            }

            return importedCount;
        }

        /// <summary> 出力先フォルダの絶対パスを組み立てる。 </summary>
        /// <returns> 出力先の絶対パス。未設定の場合は空文字。 </returns>
        internal static string GetExportRootPath()
        {
            string exportedPackagesPath = AssetStoreToolsPackagerData.ExportedPackagesPath;
            if (string.IsNullOrEmpty(exportedPackagesPath))
            {
                return string.Empty;
            }

            // 設定値はプロジェクトルートからの相対パスとして扱う。
            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", exportedPackagesPath));
        }

        private const string LOG_PREFIX = "[" + nameof(AssetStoreToolsPackageImporter) + "]";
    }
}
