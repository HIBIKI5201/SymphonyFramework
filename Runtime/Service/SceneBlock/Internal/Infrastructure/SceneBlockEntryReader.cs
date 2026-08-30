using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Authoringのエントリ一覧を検証し、依存順の実行層へ変換する。
    /// </summary>
    /// <remarks>
    ///     <see cref="SceneBlockAsset" /> ではなくエントリ一覧を受け取るため、
    ///     Unityアセットを作らずに単体テストできる。
    /// </remarks>
    internal static class SceneBlockEntryReader
    {
        #region 外部向けAPI

        /// <summary>
        ///     エントリ一覧から依存順の実行層を作る。
        /// </summary>
        /// <param name="entries"> ブロックが持つエントリ一覧。 </param>
        /// <param name="layers"> 依存順に並んだ実行層。失敗時は空。 </param>
        /// <param name="errorDescriptions"> 検出した全異常の説明。成功時は空。 </param>
        /// <returns> 実行層を作れた場合はtrue。 </returns>
        /// <exception cref="ArgumentNullException"> entriesがnullの場合。 </exception>
        /// <exception cref="ArgumentException"> entriesが空の場合。 </exception>
        internal static bool TryCreateLayers(
            IReadOnlyList<SceneBlockEntry> entries,
            out IReadOnlyList<IReadOnlyList<string>> layers,
            out IReadOnlyList<string> errorDescriptions)
        {
            // 一覧そのものの欠落と空は、アセットの記述内容ではなく呼び出し契約の違反として扱う。
            if (entries == null) { throw new ArgumentNullException(nameof(entries)); }
            if (entries.Count == 0)
            {
                throw new ArgumentException("エントリを1件以上指定してください。", nameof(entries));
            }

            List<string> descriptions = new();

            // シーン名が空のエントリはグラフのノードにできないため、除いたうえで異常として報告する。
            List<string> nodeIds = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                SceneBlockEntry entry = entries[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.SceneName))
                {
                    descriptions.Add($"{index}番目のエントリにシーン名がありません。");
                    continue;
                }

                nodeIds.Add(entry.SceneName);
            }

            // 依存名が空の要素も辺にできないため、同じく除いて報告する。
            List<SceneBlockEdge> edges = new();
            foreach (SceneBlockEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.SceneName)) { continue; }

                foreach (string dependency in entry.DependsOn)
                {
                    if (string.IsNullOrWhiteSpace(dependency))
                    {
                        descriptions.Add($"シーン {entry.SceneName} の依存に空の要素があります。");
                        continue;
                    }

                    // Authoringの「先にロードするシーン」を、Domainの先行→後続の辺へ写す。
                    edges.Add(new SceneBlockEdge(dependency, entry.SceneName));
                }
            }

            // 有効なノードが1つも無い場合、Plannerへ渡しても追加の情報が得られない。
            if (nodeIds.Count == 0)
            {
                layers = Array.Empty<IReadOnlyList<string>>();
                errorDescriptions = descriptions.AsReadOnly();
                return false;
            }

            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(nodeIds, edges);
            foreach (SceneBlockPlanError error in result.Errors)
            {
                descriptions.Add(Describe(error));
            }

            // シーン名の異常だけがある場合もグラフは成功し得るため、説明の有無で最終判定する。
            if (descriptions.Count > 0)
            {
                layers = Array.Empty<IReadOnlyList<string>>();
                errorDescriptions = descriptions.AsReadOnly();
                return false;
            }

            layers = result.Layers;
            errorDescriptions = Array.Empty<string>();
            return true;
        }

        /// <summary>
        ///     依存グラフの異常を、利用側が読める説明へ変換する。
        /// </summary>
        /// <param name="error"> 変換する異常。 </param>
        /// <returns> 異常の説明。 </returns>
        internal static string Describe(SceneBlockPlanError error)
        {
            string nodeIds = string.Join(", ", error.NodeIds);

            // 種別ごとに、何が問題でどう直すかが1行で分かる文にする。
            return error.Kind switch
            {
                SceneBlockPlanErrorEnum.DuplicateNode =>
                    $"シーン {nodeIds} が複数のエントリに登録されています。重複を取り除いてください。",
                SceneBlockPlanErrorEnum.SelfDependency =>
                    $"シーン {nodeIds} が自分自身へ依存しています。依存から取り除いてください。",
                SceneBlockPlanErrorEnum.MissingReference =>
                    $"シーン {nodeIds} はこのブロックのエントリに存在しません。エントリへ追加するか依存から取り除いてください。",
                SceneBlockPlanErrorEnum.CyclicDependency =>
                    $"シーン {nodeIds} が循環依存しています。依存のどれかを取り除いてください。",
                _ => $"シーン {nodeIds} に未知の異常({error.Kind})があります。",
            };
        }

        #endregion
    }
}
