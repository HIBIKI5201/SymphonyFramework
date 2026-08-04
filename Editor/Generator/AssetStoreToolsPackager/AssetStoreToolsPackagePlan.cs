using System.Collections.Generic;
using System.Linq;
using static SymphonyFrameWork.Editor.AssetStoreToolsPackager;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     1回のパッケージ出力で何をどう出力するかを確定した計画。
    /// </summary>
    /// <remarks>
    ///     出力前に内容を提示するため、実行と切り離して保持できる形にしている。
    /// </remarks>
    internal sealed class AssetStoreToolsPackagePlan
    {
        /// <summary> 個別出力と統合出力のどちらを行うか。 </summary>
        public PackageModeEnum Mode;

        /// <summary> 出力後にZIP化するか。 </summary>
        public bool CreateZip;

        /// <summary> 使用中アセットと強制包含拡張子だけへ絞るか。 </summary>
        public bool UsedDependencies;

        /// <summary> ディレクトリごとの出力内容。 </summary>
        public IReadOnlyList<AssetStoreToolsPackagePlanEntry> Entries = new List<AssetStoreToolsPackagePlanEntry>();

        /// <summary> 出力に含まれるアセットの総数。ディレクトリ間の重複は数えない。 </summary>
        public int TotalAssetCount => Entries
            .SelectMany(entry => entry.AssetPaths)
            .Distinct()
            .Count();
    }

    /// <summary> 出力単位となるディレクトリ1件分の内容。 </summary>
    internal sealed class AssetStoreToolsPackagePlanEntry
    {
        /// <summary> 出力単位となるディレクトリのパス。 </summary>
        public string DirectoryPath;

        /// <summary> パッケージ名と表示に使うディレクトリ名。 </summary>
        public string Name;

        /// <summary> このディレクトリから出力されるアセットのパス一覧。 </summary>
        public IReadOnlyList<string> AssetPaths = new List<string>();
    }
}
