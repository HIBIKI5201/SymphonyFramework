using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     追跡中Scene Blockの状態を表す不変なスナップショット。
    /// </summary>
    public readonly struct SceneBlockInfo : IEquatable<SceneBlockInfo>
    {
        #region 外部向けAPI

        /// <summary>
        ///     Queryが抽出した追跡状態からスナップショットを生成する。
        /// </summary>
        /// <param name="blockName"> ブロック名。 </param>
        /// <param name="state"> ロード状態。 </param>
        /// <param name="progress"> 0から1の範囲に正規化された処理進捗。 </param>
        /// <param name="heldSceneNames"> このブロックが保持しているシーン名。 </param>
        /// <param name="layerCount"> 依存順に分けた実行層の数。 </param>
        internal SceneBlockInfo(
            string blockName,
            SceneBlockLoadStateEnum state,
            float progress,
            IReadOnlyList<string> heldSceneNames,
            int layerCount)
        {
            // Queryが取得した同一時点の公開値を、不変なスナップショットへまとめる。
            BlockName = blockName;
            State = state;
            Progress = progress;
            HeldSceneNames = heldSceneNames ?? Array.Empty<string>();
            LayerCount = layerCount;
        }

        /// <summary> ブロック名。 </summary>
        public string BlockName { get; }

        /// <summary> 現在のロード状態。 </summary>
        public SceneBlockLoadStateEnum State { get; }

        /// <summary> 0から1の範囲に正規化された処理進捗。 </summary>
        public float Progress { get; }

        /// <summary> このブロックが保持しているシーン名。 </summary>
        public IReadOnlyList<string> HeldSceneNames { get; }

        /// <summary> 依存順に分けた実行層の数。 </summary>
        public int LayerCount { get; }

        /// <summary>
        ///     2つのスナップショットが同じ値を持つか判定する。
        /// </summary>
        /// <param name="left"> 左辺のスナップショット。 </param>
        /// <param name="right"> 右辺のスナップショット。 </param>
        /// <returns> 同値の場合はtrue。 </returns>
        public static bool operator ==(SceneBlockInfo left, SceneBlockInfo right) =>
            left.Equals(right);

        /// <summary>
        ///     2つのスナップショットが異なる値を持つか判定する。
        /// </summary>
        /// <param name="left"> 左辺のスナップショット。 </param>
        /// <param name="right"> 右辺のスナップショット。 </param>
        /// <returns> 異なる場合はtrue。 </returns>
        public static bool operator !=(SceneBlockInfo left, SceneBlockInfo right) =>
            !left.Equals(right);

        /// <summary>
        ///     指定したスナップショットと同値か判定する。
        /// </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> すべての値が一致する場合はtrue。 </returns>
        public bool Equals(SceneBlockInfo other)
        {
            // 保持シーンは参照ではなく内容で比較する。順序も含めて同じ場合だけ同値とする。
            if (!string.Equals(BlockName, other.BlockName, StringComparison.Ordinal)) { return false; }
            if (State != other.State) { return false; }
            if (!Progress.Equals(other.Progress)) { return false; }
            if (LayerCount != other.LayerCount) { return false; }

            return HasSameSceneNames(HeldSceneNames, other.HeldSceneNames);
        }

        /// <summary>
        ///     指定したオブジェクトと同値か判定する。
        /// </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同値のSceneBlockInfoの場合はtrue。 </returns>
        public override bool Equals(object obj) =>
            obj is SceneBlockInfo other && Equals(other);

        /// <summary>
        ///     スナップショットの全値に基づくハッシュコードを返す。
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

        #region 内部処理

        /// <summary>
        ///     2つのシーン名一覧が同じ内容と順序か判定する。
        /// </summary>
        /// <param name="left"> 左辺の一覧。 </param>
        /// <param name="right"> 右辺の一覧。 </param>
        /// <returns> 内容と順序が一致する場合はtrue。 </returns>
        private static bool HasSameSceneNames(
            IReadOnlyList<string> left,
            IReadOnlyList<string> right)
        {
            IReadOnlyList<string> leftNames = left ?? Array.Empty<string>();
            IReadOnlyList<string> rightNames = right ?? Array.Empty<string>();

            if (leftNames.Count != rightNames.Count) { return false; }

            for (int index = 0; index < leftNames.Count; index++)
            {
                if (!string.Equals(leftNames[index], rightNames[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
