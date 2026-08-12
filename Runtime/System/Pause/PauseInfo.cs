using System;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ機構の管理状態を取得時点の不変値として表す。
    /// </summary>
    public readonly struct PauseInfo : IEquatable<PauseInfo>
    {
        #region 外部向けAPI

        /// <summary>
        ///     ポーズ状態と購読件数から情報を生成する。
        /// </summary>
        /// <remarks> <see cref="PauseQuery"/>が生成し、利用側は<see cref="PauseManager.GetPauseInfo"/>で取得する。 </remarks>
        /// <param name="isPaused"> 現在ポーズ中かどうか。 </param>
        /// <param name="pausableSubscriberCount"> ポーズ通知を購読している対象の件数。 </param>
        internal PauseInfo(bool isPaused, int pausableSubscriberCount)
        {
            IsPaused = isPaused;
            PausableSubscriberCount = pausableSubscriberCount;
        }

        /// <summary> 現在ポーズ中かどうか。 </summary>
        public bool IsPaused { get; }

        /// <summary> <see cref="PauseManager.IPausable"/>としてポーズ通知を購読している対象の件数。 </summary>
        /// <remarks> 解除し忘れが積み上がっていないかの確認に使う。 </remarks>
        public int PausableSubscriberCount { get; }

        /// <summary>
        ///     管理状態が等しいか判定する。
        /// </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> ポーズ状態と購読件数がいずれも等しい場合はtrue。 </returns>
        public bool Equals(PauseInfo other) =>
            IsPaused == other.IsPaused
            && PausableSubscriberCount == other.PausableSubscriberCount;

        /// <summary>
        ///     管理状態が等しいか判定する。
        /// </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同じ型で管理状態が等しい場合はtrue。 </returns>
        public override bool Equals(object obj) => obj is PauseInfo other && Equals(other);

        /// <summary>
        ///     管理状態に基づくハッシュコードを返す。
        /// </summary>
        /// <returns> ハッシュコード。 </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (IsPaused.GetHashCode() * 397) ^ PausableSubscriberCount;
            }
        }

        /// <summary>
        ///     2つの管理状態が等しいか判定する。
        /// </summary>
        /// <param name="left"> 左辺。 </param>
        /// <param name="right"> 右辺。 </param>
        /// <returns> 等しい場合はtrue。 </returns>
        public static bool operator ==(PauseInfo left, PauseInfo right) => left.Equals(right);

        /// <summary>
        ///     2つの管理状態が異なるか判定する。
        /// </summary>
        /// <param name="left"> 左辺。 </param>
        /// <param name="right"> 右辺。 </param>
        /// <returns> 異なる場合はtrue。 </returns>
        public static bool operator !=(PauseInfo left, PauseInfo right) => !left.Equals(right);

        #endregion
    }
}
