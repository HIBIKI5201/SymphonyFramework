using System.Collections.Generic;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene BlockのAuthoringデータをDomainモデルへ変換して実行計画を作成する。
    /// </summary>
    internal static class SceneBlockAssetPlanner
    {
        #region 外部向けAPI

        /// <summary>
        ///     入力途中の空欄を除外してScene Blockの実行計画を作成する。
        /// </summary>
        /// <param name="sceneIds"> アセットに宣言されたシーン識別子。 </param>
        /// <param name="edges"> アセットに宣言された依存辺。 </param>
        /// <returns> 検証済みの実行計画または検出した全エラー。 </returns>
        internal static SceneBlockPlanResult Plan(
            IReadOnlyList<string> sceneIds,
            IReadOnlyList<SceneBlockEdgeAuthoring> edges)
        {
            // Inspectorで入力途中の空欄はノードとして扱わず、有効な識別子だけをDomainへ渡す。
            List<string> normalizedSceneIds = new();
            if (sceneIds != null)
            {
                foreach (string sceneId in sceneIds)
                {
                    if (!string.IsNullOrWhiteSpace(sceneId)) { normalizedSceneIds.Add(sceneId); }
                }
            }

            // 片端が未入力の辺は編集途中として除外し、両端が確定した辺だけを変換する。
            List<SceneBlockEdge> normalizedEdges = new();
            if (edges != null)
            {
                foreach (SceneBlockEdgeAuthoring edge in edges)
                {
                    if (string.IsNullOrWhiteSpace(edge.From) || string.IsNullOrWhiteSpace(edge.To)) { continue; }

                    normalizedEdges.Add(new SceneBlockEdge(edge.From, edge.To));
                }
            }

            return SceneBlockGraphPlanner.Plan(normalizedSceneIds, normalizedEdges);
        }

        #endregion
    }
}
