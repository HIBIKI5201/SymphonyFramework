using System;
using System.Collections.Generic;

using UnityEngine;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockのシーン一覧と依存辺を保持するAuthoringアセット。
    /// </summary>
    internal sealed class SceneBlockAsset: ScriptableObject
    {
        #region 外部向けAPI

        /// <summary> アセットに宣言されたシーン識別子。 </summary>
        internal IReadOnlyList<string> SceneIds => _sceneIds;

        /// <summary> アセットに宣言された依存辺。 </summary>
        internal IReadOnlyList<SceneBlockEdgeAuthoring> Edges => _edges;

        /// <summary> 現在のAuthoringデータからScene Blockの実行計画を作成する。 </summary>
        /// <returns> 検証済みの実行計画または検出した全エラー。 </returns>
        internal SceneBlockPlanResult Plan() => SceneBlockAssetPlanner.Plan(_sceneIds, _edges);

        #endregion

        #region 内部処理

        [SerializeField] private string[] _sceneIds = Array.Empty<string>();
        [SerializeField] private SceneBlockEdgeAuthoring[] _edges = Array.Empty<SceneBlockEdgeAuthoring>();

        #endregion
    }
}
