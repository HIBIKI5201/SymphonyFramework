using System;
using System.Collections.Generic;

using SymphonyFrameWork.Attribute;

using UnityEngine;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockが読み込むシーン1件と、その先行依存を表す。
    /// </summary>
    [Serializable]
    public sealed class SceneBlockEntry
    {
        #region 外部向けAPI

        /// <summary>
        ///     Unityのシリアライズと Inspector からの生成に使う。
        /// </summary>
        public SceneBlockEntry()
        {
        }

        /// <summary>
        ///     値を指定してエントリを生成する。
        /// </summary>
        /// <param name="sceneName"> ロードするシーン名。 </param>
        /// <param name="dependsOn"> このシーンより先にロードするシーン名。null可。 </param>
        /// <param name="priority"> Active Scene選択に使用する優先度。 </param>
        /// <param name="isPersistent"> ブロックのアンロードでも残す場合はtrue。 </param>
        /// <remarks>
        ///     Inspector以外からエントリを組み立てるのはフレームワーク内部とテストだけであるため internal にする。
        /// </remarks>
        internal SceneBlockEntry(
            string sceneName,
            IReadOnlyList<string> dependsOn = null,
            int priority = 0,
            bool isPersistent = false)
        {
            // シリアライズ対象と同じ形で保持し、Inspectorから作った場合と挙動を揃える。
            _sceneName = sceneName;
            _dependsOn = CopyOrEmpty(dependsOn);
            _priority = priority;
            _isPersistent = isPersistent;
        }

        /// <summary> ロードするシーン名。 </summary>
        public string SceneName => _sceneName;

        /// <summary> このシーンより先にロードするシーン名。 </summary>
        public IReadOnlyList<string> DependsOn => _dependsOn ?? Array.Empty<string>();

        /// <summary> Active Scene選択に使用する優先度。 </summary>
        public int Priority => _priority;

        /// <summary> ブロックのアンロードでもシーンを残す場合はtrue。 </summary>
        public bool IsPersistent => _isPersistent;

        #endregion

        #region 内部処理

        [SerializeField, SceneNameSelector, Tooltip("このエントリがロードするシーン名。")]
        private string _sceneName;

        // 依存先はブロックの中にあるシーンだけを選べる。外のシーンを選べても MissingReference になるため、
        // 選択の時点で候補から外す。フィルターは同じアセットの SceneBlockAsset 側が持つ。
        [SerializeField,
         SceneNameSelector(nameof(SceneBlockAsset.CanDependOnScene)),
         Tooltip("このシーンより先にロードを完了させるシーン名。同じブロックのシーンだけを選べる。")]
        private string[] _dependsOn = Array.Empty<string>();

        [SerializeField, Tooltip("Active Scene選択に使用する優先度。SceneLoadRequestの優先度と同じ意味を持つ。")]
        private int _priority;

        [SerializeField, Tooltip("ブロックをアンロードしてもこのシーンを残す場合に有効化する。")]
        private bool _isPersistent;

        /// <summary>
        ///     依存一覧を配列へ写す。
        /// </summary>
        /// <param name="source"> コピー元の一覧。null可。 </param>
        /// <returns> 呼び出し側から変更できないコピー。 </returns>
        private static string[] CopyOrEmpty(IReadOnlyList<string> source)
        {
            // 未指定と空の一覧を同じ「依存なし」へ寄せ、後段でnull判定を持たせない。
            if (source == null || source.Count == 0) { return Array.Empty<string>(); }

            string[] snapshot = new string[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                snapshot[index] = source[index];
            }

            return snapshot;
        }

        #endregion
    }
}
