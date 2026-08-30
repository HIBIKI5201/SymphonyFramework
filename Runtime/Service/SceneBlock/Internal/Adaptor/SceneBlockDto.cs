using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockの表示状態をViewModelへ渡す不変な更新データ。
    /// </summary>
    internal readonly struct SceneBlockDto : IEquatable<SceneBlockDto>
    {
        #region 外部向けAPI

        /// <summary>
        ///     Queryが抽出した表示値から更新データを生成する。
        /// </summary>
        /// <param name="blockName"> ブロック名。 </param>
        /// <param name="state"> ロード状態。 </param>
        /// <param name="progress"> 0から1の範囲に正規化された処理進捗。 </param>
        /// <param name="heldSceneNames"> このブロックが保持しているシーン名。 </param>
        /// <param name="layerCount"> 依存順に分けた実行層の数。 </param>
        internal SceneBlockDto(
            string blockName,
            SceneBlockLoadStateEnum state,
            float progress,
            IReadOnlyList<string> heldSceneNames,
            int layerCount)
        {
            BlockName = blockName;
            State = state;
            Progress = progress;
            HeldSceneNames = heldSceneNames ?? Array.Empty<string>();
            LayerCount = layerCount;
        }

        /// <summary> ブロック名。 </summary>
        internal string BlockName { get; }

        /// <summary> 現在のロード状態。 </summary>
        internal SceneBlockLoadStateEnum State { get; }

        /// <summary> 0から1の範囲に正規化された処理進捗。 </summary>
        internal float Progress { get; }

        /// <summary> このブロックが保持しているシーン名。 </summary>
        internal IReadOnlyList<string> HeldSceneNames { get; }

        /// <summary> 依存順に分けた実行層の数。 </summary>
        internal int LayerCount { get; }

        /// <summary>
        ///     指定した更新データと同値か判定する。
        /// </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> すべての値が一致する場合はtrue。 </returns>
        public bool Equals(SceneBlockDto other)
        {
            // 同じ内容の再通知で表示を作り直さないよう、保持シーンも内容で比較する。
            if (!string.Equals(BlockName, other.BlockName, StringComparison.Ordinal)) { return false; }
            if (State != other.State) { return false; }
            if (!Progress.Equals(other.Progress)) { return false; }
            if (LayerCount != other.LayerCount) { return false; }

            IReadOnlyList<string> left = HeldSceneNames ?? Array.Empty<string>();
            IReadOnlyList<string> right = other.HeldSceneNames ?? Array.Empty<string>();
            if (left.Count != right.Count) { return false; }

            for (int index = 0; index < left.Count; index++)
            {
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal)) { return false; }
            }

            return true;
        }

        /// <summary>
        ///     指定したオブジェクトと同値か判定する。
        /// </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同値のSceneBlockDtoの場合はtrue。 </returns>
        public override bool Equals(object obj) =>
            obj is SceneBlockDto other && Equals(other);

        /// <summary>
        ///     更新データの全値に基づくハッシュコードを返す。
        /// </summary>
        /// <returns> ハッシュコード。 </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                // Equalsで比較する全値を同じ順序で合成する。
                int hashCode = BlockName != null
                    ? StringComparer.Ordinal.GetHashCode(BlockName)
                    : 0;
                hashCode = (hashCode * 397) ^ (int)State;
                hashCode = (hashCode * 397) ^ Progress.GetHashCode();
                hashCode = (hashCode * 397) ^ LayerCount;

                IReadOnlyList<string> sceneNames = HeldSceneNames ?? Array.Empty<string>();
                hashCode = (hashCode * 397) ^ sceneNames.Count;
                foreach (string sceneName in sceneNames)
                {
                    hashCode = (hashCode * 397)
                        ^ (sceneName != null ? StringComparer.Ordinal.GetHashCode(sceneName) : 0);
                }

                return hashCode;
            }
        }

        #endregion
    }
}
