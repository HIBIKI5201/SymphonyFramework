using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     マニフェストのリビジョンとローカルの出力時バージョンを突き合わせ、
    ///     インポート候補とその状態を組み立てる。
    /// </summary>
    /// <remarks>
    ///     Unity APIへ触れない純粋なロジックとして保つ。描画側はこの結果を読むだけにする。
    /// </remarks>
    internal static class AssetStoreToolsImportPlanner
    {
        /// <summary>
        ///     マニフェストとローカルのリビジョンからインポート候補を組み立てる。
        /// </summary>
        /// <param name="manifest"> 出力先フォルダのマニフェスト。 </param>
        /// <param name="localVersions">
        ///     ディレクトリ名から現在導入されているリビジョンへの対応。未導入の名前は含めない。
        /// </param>
        /// <returns> マニフェストの並び順を保った候補の一覧。 </returns>
        internal static IReadOnlyList<AssetStoreToolsImportCandidate> Build(
            AssetStoreToolsPackageManifest manifest,
            IReadOnlyDictionary<string, int> localVersions)
        {
            List<AssetStoreToolsImportCandidate> candidates = new();

            if (manifest?.Packages == null)
            {
                return candidates;
            }

            foreach (AssetStoreToolsPackageManifestEntry entry in manifest.Packages)
            {
                // 名前かファイル名が欠けたエントリはインポート先を特定できない。
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.Name)
                    || string.IsNullOrWhiteSpace(entry.FileName))
                {
                    continue;
                }

                int? localVersion = FindLocalVersion(localVersions, entry.Name);
                AssetStoreToolsImportStateEnum state = ResolveState(entry.Version, localVersion);

                candidates.Add(new AssetStoreToolsImportCandidate
                {
                    Name = entry.Name.Trim(),
                    FileName = entry.FileName.Trim(),
                    ManifestVersion = entry.Version,
                    LocalVersion = localVersion,
                    State = state,
                    IsSelected = IsSelectedByDefault(state),
                });
            }

            return candidates;
        }

        /// <summary>
        ///     マニフェストとローカルのリビジョンから状態を判定する。
        /// </summary>
        /// <param name="manifestVersion"> マニフェストが持つリビジョン。 </param>
        /// <param name="localVersion"> 現在導入されているリビジョン。未導入の場合はnull。 </param>
        /// <returns> インポートの要否を表す状態。 </returns>
        internal static AssetStoreToolsImportStateEnum ResolveState(
            int manifestVersion,
            int? localVersion)
        {
            if (localVersion == null)
            {
                return AssetStoreToolsImportStateEnum.New;
            }

            if (localVersion.Value < manifestVersion)
            {
                return AssetStoreToolsImportStateEnum.Updated;
            }

            if (localVersion.Value > manifestVersion)
            {
                return AssetStoreToolsImportStateEnum.Newer;
            }

            return AssetStoreToolsImportStateEnum.UpToDate;
        }

        /// <summary>
        ///     状態に対して既定で選択するかを返す。
        /// </summary>
        /// <remarks>
        ///     Newerを既定で選ばないのは、ローカルの方が新しいものを取り込むと
        ///     意図しない巻き戻しになるためである。
        /// </remarks>
        /// <param name="state"> インポートの要否を表す状態。 </param>
        /// <returns> 新規または更新の場合はtrue。 </returns>
        internal static bool IsSelectedByDefault(AssetStoreToolsImportStateEnum state)
            => state == AssetStoreToolsImportStateEnum.New
               || state == AssetStoreToolsImportStateEnum.Updated;

        /// <summary> ディレクトリ名からローカルのリビジョンを探す。大文字小文字は区別しない。 </summary>
        /// <param name="localVersions"> ディレクトリ名からリビジョンへの対応。 </param>
        /// <param name="name"> 探すディレクトリ名。 </param>
        /// <returns> 見つかったリビジョン。未導入の場合はnull。 </returns>
        private static int? FindLocalVersion(
            IReadOnlyDictionary<string, int> localVersions,
            string name)
        {
            if (localVersions == null)
            {
                return null;
            }

            string trimmedName = name.Trim();
            foreach (KeyValuePair<string, int> pair in localVersions)
            {
                if (string.Equals(pair.Key, trimmedName, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }

            return null;
        }
    }
}
