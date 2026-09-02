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

        /// <summary>
        ///     依存先として選べるシーン名かを判定する。
        /// </summary>
        /// <param name="sceneName"> 判定するシーン名。 </param>
        /// <returns> 依存先の候補に残す場合はtrue。 </returns>
        /// <remarks>
        ///     <see cref="SceneBlockEntry" /> の依存欄が <c>SceneNameSelector</c> のフィルターとして呼ぶ。
        ///     **ブロックの外にあるシーンは依存先にできないため、そもそも選べないようにする。**
        ///     Build Settings の全シーンを並べると、選んだ後に
        ///     <c>MissingReference</c> で弾かれることに気づく形になる。
        ///     既に依存として保存済みのシーン名も候補に残す。**候補から外すと、
        ///     Drawerが保存済みの値を先頭の候補へ書き換えてしまい、記述が黙って変わる。**
        ///     外れた依存であること自体は <see cref="SceneBlockEntryReader" /> の検証が知らせる。
        /// </remarks>
        internal bool CanDependOnScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) { return false; }

            foreach (SceneBlockEntry entry in Entries)
            {
                if (entry == null) { continue; }
                if (string.Equals(entry.SceneName, sceneName, StringComparison.Ordinal)) { return true; }

                foreach (string dependency in entry.DependsOn)
                {
                    if (string.Equals(dependency, sceneName, StringComparison.Ordinal)) { return true; }
                }
            }

            return false;
        }

        #endregion
    }
}
