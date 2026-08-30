using System;

using UnityEngine;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockアセットで編集する依存辺を表す。
    /// </summary>
    [Serializable]
    internal struct SceneBlockEdgeAuthoring
    {
        #region 外部向けAPI

        /// <summary>
        ///     依存元と依存先のシーン識別子からAuthoring値を生成する。
        /// </summary>
        /// <param name="from"> 依存元のシーン識別子。 </param>
        /// <param name="to"> 依存先のシーン識別子。 </param>
        internal SceneBlockEdgeAuthoring(string from, string to)
        {
            _from = from;
            _to = to;
        }

        /// <summary> 依存元のシーン識別子。 </summary>
        internal string From => _from;

        /// <summary> 依存先のシーン識別子。 </summary>
        internal string To => _to;

        #endregion

        #region 内部処理

        [SerializeField] private string _from;
        [SerializeField] private string _to;

        #endregion
    }
}
