using SymphonyFrameWork.Core;

using System;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     変更されたアセットのパスから、リビジョンを加算する対象ディレクトリ名を解決する。
    /// </summary>
    /// <remarks>
    ///     Unity APIへ触れない純粋なロジックとして保つ。呼び出し側はこの判定結果だけを見る。
    /// </remarks>
    internal static class AssetStoreToolsVersionPathResolver
    {
        #region 外部向けAPI

        /// <summary>
        ///     アセットパスがどのパッケージ対象ディレクトリに属するかを解決する。
        /// </summary>
        /// <remarks>
        ///     対象フォルダ直下のファイル（設定ファイルやバージョンログ自身）と、
        ///     ディレクトリ自身のパスは対象外にする。
        ///     各ディレクトリ内の出力時バージョンファイルも対象外にする。
        ///     これを加算対象にすると、パッケージ出力で書いたファイルが再び加算を呼び、
        ///     リビジョンが際限なく増える。
        /// </remarks>
        /// <param name="assetPath"> 変更されたアセットのパス。 </param>
        /// <param name="rootPath"> パッケージ対象フォルダのルートパス。 </param>
        /// <param name="directoryName"> 解決したディレクトリ名。解決できない場合はnull。 </param>
        /// <returns> リビジョンの加算対象であればtrue。 </returns>
        internal static bool TryResolveDirectoryName(
            string assetPath,
            string rootPath,
            out string directoryName)
        {
            directoryName = null;

            // 変更パスか基準ルートが無ければ所属を判定できない。
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(rootPath)) { return false; }

            string normalizedAssetPath = assetPath.Replace('\\', '/');
            string normalizedRootPath = rootPath.Replace('\\', '/').TrimEnd('/');
            // 区切り文字だけのルートを、全アセットの基準として扱わない。
            if (normalizedRootPath.Length == 0) { return false; }

            string rootPrefix = normalizedRootPath + "/";
            // 対象ルート外の変更は、このバージョンログへ反映しない。
            if (!normalizedAssetPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)) { return false; }

            string relativePath = normalizedAssetPath.Substring(rootPrefix.Length);

            // 区切りが無い場合は対象フォルダ直下のファイルかディレクトリ自身であり、
            // どのパッケージにも属さない。
            int separatorIndex = relativePath.IndexOf('/');
            if (separatorIndex <= 0) { return false; }

            string name = relativePath.Substring(0, separatorIndex);
            string remainingPath = relativePath.Substring(separatorIndex + 1);

            // 出力処理自身が書くバージョンファイルは、リビジョン加算の再入を防ぐため除外する。
            if (IsExportedVersionFile(remainingPath)) { return false; }

            directoryName = name;
            return true;
        }

        #endregion

        #region 内部処理

        /// <summary> Unityが生成するメタデータファイルの拡張子。 </summary>
        private const string META_EXTENSION = ".meta";

        /// <summary>
        ///     ディレクトリからの相対パスが出力時バージョンファイルを指すか判定する。
        /// </summary>
        /// <param name="remainingPath"> ディレクトリ名を除いた相対パス。 </param>
        /// <returns> バージョンファイルまたはその.metaであればtrue。 </returns>
        private static bool IsExportedVersionFile(string remainingPath)
        {
            string fileName = remainingPath;

            // .metaはコールバックの引数に含まれないが、将来含まれても除外が崩れないようにする。
            if (fileName.EndsWith(META_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - META_EXTENSION.Length);
            }

            return string.Equals(
                fileName,
                EditorSymphonyConstant.ASSET_STORE_TOOLS_EXPORTED_VERSION_FILE_NAME,
                StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
