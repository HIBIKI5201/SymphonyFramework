using System;
using System.Collections.Generic;
using System.Linq;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     1回のパッケージ出力で何をどう出力するかを確定した計画。
    /// </summary>
    /// <remarks>
    ///     出力前に内容を提示するため、実行と切り離して保持できる形にしている。
    ///     計画を組み立てられるのは<see cref="AssetStoreToolsPackagePipelineRunner" />だけで、
    ///     利用側のStrategyからは<see cref="AssetStoreToolsPackagePlanEntry.FilterAssetPaths" />で
    ///     出力対象を絞り込むことだけができる。
    /// </remarks>
    public sealed class AssetStoreToolsPackagePlan
    {
        /// <summary> 計画を組み立てたパイプラインの名前。 </summary>
        public string PipelineName { get; }

        /// <summary> 実行する手順。null要素は除去済み。 </summary>
        public IReadOnlyList<AssetStoreToolsPackageStepStrategy> Steps { get; }

        /// <summary> ディレクトリごとの出力内容。 </summary>
        public IReadOnlyList<AssetStoreToolsPackagePlanEntry> Entries { get; }

        /// <summary> 依存関係に含まれなくても強制的にパッケージへ含める拡張子。 </summary>
        public IReadOnlyList<string> ForceIncludeExtensions { get; }

        /// <summary> 出力に含まれるアセットの総数。ディレクトリ間の重複は数えない。 </summary>
        public int TotalAssetCount => Entries
            .SelectMany(entry => entry.AssetPaths)
            .Distinct(StringComparer.Ordinal)
            .Count();

        /// <summary>
        ///     いずれかのディレクトリでPlan段階の絞り込みが行われたかを示す。
        /// </summary>
        /// <remarks>
        ///     ディレクトリ単位ではなく計画全体で1つの出力を作るStrategyが、
        ///     ディレクトリ丸ごとの出力と明示指定の出力を切り替えるために使う。
        ///     1件でも絞り込まれていれば明示指定を選ぶ。丸ごと出力を選ぶと、
        ///     確認ウィンドウで提示した内容より多くのアセットが出力され得るため。
        /// </remarks>
        public bool IsFiltered => Entries.Any(entry => entry.IsFiltered);

        /// <summary> ランナーが確定した内容から計画を生成する。 </summary>
        /// <param name="pipelineName"> 計画を組み立てたパイプラインの名前。 </param>
        /// <param name="steps"> 実行する手順。null要素は呼び出し側で除去しておく。 </param>
        /// <param name="entries"> ディレクトリごとの出力内容。 </param>
        /// <param name="forceIncludeExtensions"> 強制的にパッケージへ含める拡張子。 </param>
        internal AssetStoreToolsPackagePlan(
            string pipelineName,
            IReadOnlyList<AssetStoreToolsPackageStepStrategy> steps,
            IReadOnlyList<AssetStoreToolsPackagePlanEntry> entries,
            IReadOnlyList<string> forceIncludeExtensions)
        {
            PipelineName = pipelineName ?? string.Empty;
            Steps = steps ?? Array.Empty<AssetStoreToolsPackageStepStrategy>();
            Entries = entries ?? Array.Empty<AssetStoreToolsPackagePlanEntry>();
            ForceIncludeExtensions = forceIncludeExtensions ?? Array.Empty<string>();
        }
    }

    /// <summary> 出力単位となるディレクトリ1件分の内容。 </summary>
    public sealed class AssetStoreToolsPackagePlanEntry
    {
        /// <summary> 出力単位となるディレクトリのパス。 </summary>
        public string DirectoryPath { get; }

        /// <summary> パッケージ名と表示に使うディレクトリ名。 </summary>
        public string Name { get; }

        /// <summary> 計画を組んだ時点のリビジョン。出力時バージョンとマニフェストへ記録する。 </summary>
        public int Version { get; }

        /// <summary> このディレクトリから出力されるアセットのパス一覧。 </summary>
        public IReadOnlyList<string> AssetPaths { get; private set; }

        /// <summary> Plan段階で出力対象が絞り込まれたかを示す。 </summary>
        public bool IsFiltered { get; private set; }

        /// <summary>
        ///     条件に合わないアセットを出力対象から外す。
        /// </summary>
        /// <remarks>
        ///     絞り込みしかできない。アセットを追加できると、確認ウィンドウで提示した内容より
        ///     多くのものが出力され得るため。
        /// </remarks>
        /// <param name="predicate"> 出力対象として残す場合にtrueを返す判定。 </param>
        /// <exception cref="ArgumentNullException"> <paramref name="predicate" />がnullの場合。 </exception>
        public void FilterAssetPaths(Func<string, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            AssetPaths = AssetPaths
                .Where(path => predicate(path))
                .ToArray();
            IsFiltered = true;
        }

        /// <summary> ランナーが収集した内容から出力単位を生成する。 </summary>
        /// <param name="directoryPath"> 出力単位となるディレクトリのパス。 </param>
        /// <param name="name"> パッケージ名と表示に使うディレクトリ名。 </param>
        /// <param name="version"> 計画を組んだ時点のリビジョン。 </param>
        /// <param name="assetPaths"> 絞り込み前の出力対象アセット。 </param>
        internal AssetStoreToolsPackagePlanEntry(
            string directoryPath,
            string name,
            int version,
            IReadOnlyList<string> assetPaths)
        {
            DirectoryPath = directoryPath;
            Name = name;
            Version = version;
            AssetPaths = assetPaths ?? Array.Empty<string>();
        }
    }
}
