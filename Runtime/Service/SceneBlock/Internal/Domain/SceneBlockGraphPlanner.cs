using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockの依存グラフを検証してトポロジカル層を算出する。
    /// </summary>
    internal static class SceneBlockGraphPlanner
    {
        #region 外部向けAPI

        /// <summary>
        ///     ノードと依存辺を検証して依存順の実行層を作成する。
        /// </summary>
        /// <param name="nodeIds"> グラフに含まれるノード識別子。 </param>
        /// <param name="edges"> 先行ノードから後続ノードへの依存辺。 </param>
        /// <returns> 成功時はトポロジカル層、失敗時は検出した全エラー。 </returns>
        /// <exception cref="ArgumentNullException"> nodeIdsまたはedgesがnullの場合。 </exception>
        /// <exception cref="ArgumentException"> nodeIdsにnull、空、空白の識別子が含まれる場合。 </exception>
        internal static SceneBlockPlanResult Plan(
            IReadOnlyList<string> nodeIds,
            IReadOnlyList<SceneBlockEdge> edges)
        {
            // 呼び出し側のプログラミング誤りはグラフ検証結果と分けて例外にする。
            ValidateArguments(nodeIds, edges);

            // 重複を除いた有効ノード集合と、重複識別子のエラーを先に確定する。
            List<SceneBlockPlanError> errors = new();
            HashSet<string> validNodeIds = new(StringComparer.Ordinal);
            HashSet<string> duplicateNodeIds = new(StringComparer.Ordinal);
            foreach (string nodeId in nodeIds)
            {
                if (!validNodeIds.Add(nodeId)) { duplicateNodeIds.Add(nodeId); }
            }

            AddSingleNodeErrors(
                errors,
                SceneBlockPlanErrorEnum.DuplicateNode,
                duplicateNodeIds);

            // 自己依存と欠落参照を報告し、それらを除いた辺だけを循環検出へ渡す。
            List<SceneBlockEdge> validEdges = new();
            HashSet<string> missingNodeIds = new(StringComparer.Ordinal);
            foreach (SceneBlockEdge edge in edges)
            {
                bool isSelfDependency = string.Equals(
                    edge.From,
                    edge.To,
                    StringComparison.Ordinal);
                if (isSelfDependency)
                {
                    errors.Add(new SceneBlockPlanError(
                        SceneBlockPlanErrorEnum.SelfDependency,
                        new[] { edge.From }));
                }

                bool hasMissingReference = false;
                if (!validNodeIds.Contains(edge.From))
                {
                    missingNodeIds.Add(edge.From);
                    hasMissingReference = true;
                }

                if (!validNodeIds.Contains(edge.To))
                {
                    missingNodeIds.Add(edge.To);
                    hasMissingReference = true;
                }

                if (!isSelfDependency && !hasMissingReference)
                {
                    validEdges.Add(edge);
                }
            }

            AddSingleNodeErrors(
                errors,
                SceneBlockPlanErrorEnum.MissingReference,
                missingNodeIds);

            // 個別異常を除いたグラフへKahnのアルゴリズムを適用する。
            List<IReadOnlyList<string>> layers = BuildLayers(
                validNodeIds,
                validEdges,
                out IReadOnlyList<string> cyclicNodeIds);
            if (cyclicNodeIds.Count > 0)
            {
                errors.Add(new SceneBlockPlanError(
                    SceneBlockPlanErrorEnum.CyclicDependency,
                    cyclicNodeIds));
            }

            // 異常を1件でも検出した場合は、部分的な実行層を公開せず全エラーを返す。
            return errors.Count > 0
                ? SceneBlockPlanResult.Failure(errors)
                : SceneBlockPlanResult.Success(layers);
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     呼び出し側が渡したノードと辺の一覧が引数契約を満たすか検証する。
        /// </summary>
        /// <param name="nodeIds"> 検証するノード識別子。 </param>
        /// <param name="edges"> 検証する依存辺。 </param>
        /// <exception cref="ArgumentNullException"> nodeIdsまたはedgesがnullの場合。 </exception>
        /// <exception cref="ArgumentException"> nodeIdsにnull、空、空白の識別子が含まれる場合。 </exception>
        private static void ValidateArguments(
            IReadOnlyList<string> nodeIds,
            IReadOnlyList<SceneBlockEdge> edges)
        {
            // 一覧自体の欠落はデータ上のグラフ異常ではなく呼び出し契約違反として扱う。
            if (nodeIds == null) { throw new ArgumentNullException(nameof(nodeIds)); }
            if (edges == null) { throw new ArgumentNullException(nameof(edges)); }

            // ノード識別子が無いグラフは後続処理で意味を持たないため拒否する。
            foreach (string nodeId in nodeIds)
            {
                if (string.IsNullOrWhiteSpace(nodeId))
                {
                    throw new ArgumentException(
                        "ノード識別子にnull、空、空白は使用できません。",
                        nameof(nodeIds));
                }
            }
        }

        /// <summary>
        ///     識別子ごとの単一ノードエラーをOrdinal順で追加する。
        /// </summary>
        /// <param name="errors"> 追加先のエラー一覧。 </param>
        /// <param name="kind"> 追加する異常の種類。 </param>
        /// <param name="nodeIds"> 異常に該当したノード識別子。 </param>
        private static void AddSingleNodeErrors(
            ICollection<SceneBlockPlanError> errors,
            SceneBlockPlanErrorEnum kind,
            IEnumerable<string> nodeIds)
        {
            // 入力順に左右されない診断結果にするため識別子をOrdinal順へ揃える。
            List<string> orderedNodeIds = new(nodeIds);
            orderedNodeIds.Sort(StringComparer.Ordinal);
            foreach (string nodeId in orderedNodeIds)
            {
                errors.Add(new SceneBlockPlanError(kind, new[] { nodeId }));
            }
        }

        /// <summary>
        ///     Kahnのアルゴリズムで依存順の層と循環に残ったノードを算出する。
        /// </summary>
        /// <param name="nodeIds"> 重複を除いた有効ノード集合。 </param>
        /// <param name="edges"> 自己依存と欠落参照を除いた依存辺。 </param>
        /// <param name="cyclicNodeIds"> 入次数が0にならず残ったノード識別子。 </param>
        /// <returns> 依存順に並んだトポロジカル層。 </returns>
        private static List<IReadOnlyList<string>> BuildLayers(
            IReadOnlyCollection<string> nodeIds,
            IReadOnlyList<SceneBlockEdge> edges,
            out IReadOnlyList<string> cyclicNodeIds)
        {
            // 全ノードの入次数と後続ノード一覧を初期化する。
            Dictionary<string, int> inDegrees = new(StringComparer.Ordinal);
            Dictionary<string, List<string>> outgoingNodeIds = new(StringComparer.Ordinal);
            foreach (string nodeId in nodeIds)
            {
                inDegrees.Add(nodeId, 0);
                outgoingNodeIds.Add(nodeId, new List<string>());
            }

            // 有効な依存辺を入次数と後続ノード一覧へ反映する。
            foreach (SceneBlockEdge edge in edges)
            {
                outgoingNodeIds[edge.From].Add(edge.To);
                inDegrees[edge.To]++;
            }

            // 最初に依存を持たないノードを、決定的なOrdinal順で第1層へ置く。
            List<string> currentLayer = new();
            foreach (KeyValuePair<string, int> entry in inDegrees)
            {
                if (entry.Value == 0) { currentLayer.Add(entry.Key); }
            }

            currentLayer.Sort(StringComparer.Ordinal);
            List<IReadOnlyList<string>> layers = new();
            int assignedNodeCount = 0;

            // 各層を取り除き、新たに入次数0になったノードを次の層へまとめる。
            while (currentLayer.Count > 0)
            {
                string[] layerSnapshot = currentLayer.ToArray();
                layers.Add(Array.AsReadOnly(layerSnapshot));
                assignedNodeCount += currentLayer.Count;

                List<string> nextLayer = new();
                foreach (string nodeId in currentLayer)
                {
                    foreach (string outgoingNodeId in outgoingNodeIds[nodeId])
                    {
                        inDegrees[outgoingNodeId]--;
                        if (inDegrees[outgoingNodeId] == 0) { nextLayer.Add(outgoingNodeId); }
                    }
                }

                nextLayer.Sort(StringComparer.Ordinal);
                currentLayer = nextLayer;
            }

            // 未割り当てノードは循環により入次数が0にならなかった集合として返す。
            List<string> remainingNodeIds = new();
            if (assignedNodeCount < nodeIds.Count)
            {
                foreach (KeyValuePair<string, int> entry in inDegrees)
                {
                    if (entry.Value > 0) { remainingNodeIds.Add(entry.Key); }
                }

                remainingNodeIds.Sort(StringComparer.Ordinal);
            }

            cyclicNodeIds = Array.AsReadOnly(remainingNodeIds.ToArray());
            return layers;
        }

        #endregion
    }
}
