using System;
using System.Collections.Generic;

using UnityEngine;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     まとめてロードするシーンと、その依存関係を定義するアセット。
    /// </summary>
    /// <remarks>
    ///     利用側が <c>Assets &gt; Create &gt; Symphony Framework &gt; Scene Block</c> から作成する。
    ///     値の変更はInspectorからだけ行い、コードからは読み取りだけを行う。
    /// </remarks>
    [CreateAssetMenu(
        menuName = "Symphony Framework/Scene Block",
        fileName = "SceneBlock")]
    public sealed class SceneBlockAsset : ScriptableObject
    {
        #region 外部向けAPI

        /// <summary> ブロックの識別名。未設定の場合はアセット名。 </summary>
        public string BlockName =>
            string.IsNullOrWhiteSpace(_blockName) ? name : _blockName;

        /// <summary> このブロックがロードするシーンの一覧。 </summary>
        public IReadOnlyList<SceneBlockEntry> Entries =>
            _entries ?? (IReadOnlyList<SceneBlockEntry>)Array.Empty<SceneBlockEntry>();

        #endregion

        #region 内部処理

        [SerializeField, Tooltip("ブロックの識別名。空の場合はアセット名を使用する。")]
        private string _blockName;

        [SerializeField, Tooltip("このブロックがロードするシーンと、その先行依存。")]
        private List<SceneBlockEntry> _entries = new();

        #endregion
    }
}
