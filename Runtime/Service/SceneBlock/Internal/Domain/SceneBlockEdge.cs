using System;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     シーン間のロード依存関係を表す。
    /// </summary>
    internal readonly struct SceneBlockEdge : IEquatable<SceneBlockEdge>
    {
        #region 外部向けAPI

        /// <summary>
        ///     先行するシーンと後続するシーンを指定して依存辺を生成する。
        /// </summary>
        /// <param name="from"> 先にロードを完了するシーン識別子。 </param>
        /// <param name="to"> 後からロードできるシーン識別子。 </param>
        internal SceneBlockEdge(string from, string to)
        {
            // グラフ全体の検証で欠落参照をまとめて報告できるよう、識別子はそのまま保持する。
            From = from;
            To = to;
        }

        /// <summary> 先にロードを完了するシーン識別子。 </summary>
        internal string From { get; }

        /// <summary> 後からロードできるシーン識別子。 </summary>
        internal string To { get; }

        /// <summary>
        ///     指定した依存辺と同値か判定する。
        /// </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> 始点と終点が一致する場合はtrue。 </returns>
        public bool Equals(SceneBlockEdge other) =>
            string.Equals(From, other.From, StringComparison.Ordinal)
            && string.Equals(To, other.To, StringComparison.Ordinal);

        /// <summary>
        ///     指定したオブジェクトと同値か判定する。
        /// </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同値のSceneBlockEdgeの場合はtrue。 </returns>
        public override bool Equals(object obj) =>
            obj is SceneBlockEdge other && Equals(other);

        /// <summary>
        ///     始点と終点に基づくハッシュコードを返す。
        /// </summary>
        /// <returns> ハッシュコード。 </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                // Equalsと同じOrdinal比較の値を同じ順序で合成する。
                int hashCode = From != null ? StringComparer.Ordinal.GetHashCode(From) : 0;
                hashCode = (hashCode * 397) ^ (To != null ? StringComparer.Ordinal.GetHashCode(To) : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
