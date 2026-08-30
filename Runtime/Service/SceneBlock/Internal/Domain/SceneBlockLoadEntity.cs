using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     追跡中Scene Block 1件の実行計画と進行状態を保持する。
    /// </summary>
    /// <remarks>
    ///     アセットの同一性は <see cref="AssetInstanceId" /> として整数で保持し、
    ///     Unityオブジェクトへの参照はDomainへ持ち込まない。
    /// </remarks>
    internal sealed class SceneBlockLoadEntity
    {
        #region 外部向けAPI

        /// <summary>
        ///     ブロック名と実行計画を指定して生成する。
        /// </summary>
        /// <param name="blockName"> ブロック名。 </param>
        /// <param name="assetInstanceId"> 由来するアセットのインスタンスID。 </param>
        /// <param name="layers"> 依存順に並んだ実行層。 </param>
        /// <param name="priorities"> シーン名ごとのActive Scene選択の優先度。 </param>
        /// <param name="persistentSceneNames"> ブロックのアンロードでも残すシーン名。 </param>
        /// <exception cref="ArgumentNullException"> layers、priorities、persistentSceneNamesのいずれかがnullの場合。 </exception>
        internal SceneBlockLoadEntity(
            string blockName,
            int assetInstanceId,
            IReadOnlyList<IReadOnlyList<string>> layers,
            IReadOnlyDictionary<string, int> priorities,
            IReadOnlyCollection<string> persistentSceneNames)
        {
            // 実行計画はロード開始時に確定し、以後は変化しない値として保持する。
            if (layers == null) { throw new ArgumentNullException(nameof(layers)); }
            if (priorities == null) { throw new ArgumentNullException(nameof(priorities)); }
            if (persistentSceneNames == null) { throw new ArgumentNullException(nameof(persistentSceneNames)); }

            BlockName = blockName;
            AssetInstanceId = assetInstanceId;
            Layers = layers;
            State = SceneBlockLoadStateEnum.None;

            _priorities = priorities;
            _persistentSceneNames = new HashSet<string>(persistentSceneNames, StringComparer.Ordinal);

            // 進捗の分母になるシーン総数を、層の合計から一度だけ求める。
            int sceneCount = 0;
            foreach (IReadOnlyList<string> layer in layers) { sceneCount += layer.Count; }

            SceneCount = sceneCount;
        }

        /// <summary> ブロック名。 </summary>
        internal string BlockName { get; }

        /// <summary> 由来するアセットのインスタンスID。 </summary>
        internal int AssetInstanceId { get; }

        /// <summary> 依存順に並んだ実行層。 </summary>
        internal IReadOnlyList<IReadOnlyList<string>> Layers { get; }

        /// <summary> ブロックが対象とするシーンの総数。 </summary>
        internal int SceneCount { get; }

        /// <summary> 現在のロード状態。 </summary>
        internal SceneBlockLoadStateEnum State { get; private set; }

        /// <summary> 0から1の範囲に正規化された処理進捗。 </summary>
        internal float Progress { get; private set; }

        /// <summary>
        ///     ロードの開始を記録する。
        /// </summary>
        internal void BeginLoading()
        {
            State = SceneBlockLoadStateEnum.Loading;
            Progress = 0f;
        }

        /// <summary>
        ///     全シーンのロード完了を記録する。
        /// </summary>
        internal void CompleteLoading()
        {
            State = SceneBlockLoadStateEnum.Complete;
            Progress = 1f;
        }

        /// <summary>
        ///     アンロードの開始を記録する。
        /// </summary>
        internal void BeginUnloading()
        {
            State = SceneBlockLoadStateEnum.Unloading;
        }

        /// <summary>
        ///     進捗を更新する。
        /// </summary>
        /// <param name="value"> 0から1の範囲に正規化された進捗。 </param>
        /// <returns> 値が変化した場合はtrue。 </returns>
        internal bool ReportProgress(float value)
        {
            // 外部の進捗実装が範囲外の値を返してもEntityの契約を維持する。
            float normalized = Math.Max(0f, Math.Min(1f, value));
            if (Progress == normalized) { return false; }

            Progress = normalized;
            return true;
        }

        /// <summary>
        ///     指定したシーンがブロックのアンロードでも残る対象か確認する。
        /// </summary>
        /// <param name="sceneName"> 確認するシーン名。 </param>
        /// <returns> 残す対象の場合はtrue。 </returns>
        internal bool IsPersistent(string sceneName) =>
            _persistentSceneNames.Contains(sceneName);

        /// <summary>
        ///     指定したシーンのActive Scene選択の優先度を取得する。
        /// </summary>
        /// <param name="sceneName"> 取得するシーン名。 </param>
        /// <returns> 指定が無い場合は0。 </returns>
        internal int GetPriority(string sceneName) =>
            _priorities.TryGetValue(sceneName, out int priority) ? priority : 0;

        #endregion

        #region 内部処理

        private readonly IReadOnlyDictionary<string, int> _priorities;
        private readonly HashSet<string> _persistentSceneNames;

        #endregion
    }
}
