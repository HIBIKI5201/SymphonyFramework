using System;

namespace SymphonyFrameWork.System
{
    /// <summary> ポーズ機構の表示用更新値。 </summary>
    internal readonly struct PauseDto : IEquatable<PauseDto>
    {
        /// <summary>
        ///     表示に必要な値を指定して生成する。
        /// </summary>
        /// <param name="isPaused"> 現在ポーズ中かどうか。 </param>
        /// <param name="pausableSubscriberCount"> ポーズ通知を購読している対象の件数。 </param>
        internal PauseDto(bool isPaused, int pausableSubscriberCount)
        {
            IsPaused = isPaused;
            PausableSubscriberCount = pausableSubscriberCount;
        }

        /// <summary> 現在ポーズ中かどうか。 </summary>
        internal bool IsPaused { get; }

        /// <summary> ポーズ通知を購読している対象の件数。 </summary>
        internal int PausableSubscriberCount { get; }

        /// <summary> 表示値が等しいか判定する。 </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> 全ての表示値が等しい場合はtrue。 </returns>
        public bool Equals(PauseDto other) =>
            IsPaused == other.IsPaused
            && PausableSubscriberCount == other.PausableSubscriberCount;

        /// <summary> 表示値が等しいか判定する。 </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同じ型で表示値が等しい場合はtrue。 </returns>
        public override bool Equals(object obj) => obj is PauseDto other && Equals(other);

        /// <summary> 表示値に基づくハッシュコードを返す。 </summary>
        /// <returns> ハッシュコード。 </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (IsPaused.GetHashCode() * 397) ^ PausableSubscriberCount;
            }
        }

        /// <summary> 2つの表示値が等しいか判定する。 </summary>
        /// <param name="left"> 左辺。 </param>
        /// <param name="right"> 右辺。 </param>
        /// <returns> 等しい場合はtrue。 </returns>
        public static bool operator ==(PauseDto left, PauseDto right) => left.Equals(right);

        /// <summary> 2つの表示値が異なるか判定する。 </summary>
        /// <param name="left"> 左辺。 </param>
        /// <param name="right"> 右辺。 </param>
        /// <returns> 異なる場合はtrue。 </returns>
        public static bool operator !=(PauseDto left, PauseDto right) => !left.Equals(right);
    }
}
