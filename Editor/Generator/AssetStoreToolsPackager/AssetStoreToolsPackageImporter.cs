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
        #region 外部向けAPI

        /// <summary>
        ///     出力先フォルダにある出力済みフォルダを新しい順に取得する。
        /// </summary>
        /// <returns> 出力済みフォルダの絶対パス。出力先が無い場合は空。 </returns>
        internal static IReadOnlyList<string> GetExportDirectories()
        {
            string exportRoot = GetExportRootPath();
            // 出力先が未設定か未作成なら、利用可能な履歴は無いものとして扱う。
            if (string.IsNullOrEmpty(exportRoot) || !Directory.Exists(exportRoot)) { return Array.Empty<string>(); }

            // フォルダ名には出力日時が含まれるため、辞書順の降順で新しい出力を先頭へ置く。
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
            // 対象ルートが利用できなければ、全パッケージを未導入として扱う。
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) { return versions; }

            // 出力時バージョンを持つディレクトリだけを、導入済みとして記録する。
            foreach (string directory in Directory.GetDirectories(root))
            {
                // 出力時バージョンを読み取れたディレクトリだけを導入済みとして登録する。
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
            // 出力元か候補が無ければ、インポート処理を開始できない。
            if (string.IsNullOrEmpty(exportDirectoryPath) || candidates == null) { return 0; }

            int importedCount = 0;
            // 利用者が選択した候補だけを、マニフェストの順序で取り込む。
            foreach (AssetStoreToolsImportCandidate candidate in candidates)
            {
                // null要素と未選択項目は、インポート対象として扱わない。
                if (candidate == null || !candidate.IsSelected) { continue; }

                string packagePath = Path.Combine(exportDirectoryPath, candidate.FileName);
                // 一部の出力ファイルが欠けていても、残りの選択項目は継続して取り込む。
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

            // 1件も開始できなかった場合は、成功を示すログを出さない。
            if (importedCount > 0) { Debug.Log($"{LOG_PREFIX}\n{importedCount}件のパッケージをインポートしました。"); }

            return importedCount;
        }

        /// <summary>
        ///     出力先フォルダの絶対パスを組み立てる。
        /// </summary>
        /// <returns> 出力先の絶対パス。未設定の場合は空文字。 </returns>
        internal static string GetExportRootPath()
        {
            string exportedPackagesPath = AssetStoreToolsPackagerData.ExportedPackagesPath;
            // 未設定値を絶対パスへ変換するとプロジェクトルートを誤認するため空で返す。
            if (string.IsNullOrEmpty(exportedPackagesPath)) { return string.Empty; }

            // 設定値はプロジェクトルートからの相対パスとして扱う。
            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", exportedPackagesPath));
        }

        #endregion

        #region 内部処理

        private const string LOG_PREFIX = "[" + nameof(AssetStoreToolsPackageImporter) + "]";

        #endregion
    }
}
