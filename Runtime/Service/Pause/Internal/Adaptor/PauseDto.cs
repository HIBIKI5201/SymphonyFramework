using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ機構の表示用更新値を表す。
    /// </summary>
    internal readonly struct PauseDto : IEquatable<PauseDto>
    {
        #region 外部向けAPI

        /// <summary>
        ///     表示に必要な値を指定して生成する。
        /// </summary>
        /// <param name="isPaused"> 現在ポーズ中かどうか。 </param>
        /// <param name="pausableSubscriberCount"> ポーズ通知を購読している対象の件数。 </param>
        /// <param name="categories"> カテゴリーごとの表示値。null可。 </param>
        internal PauseDto(
            bool isPaused,
            int pausableSubscriberCount,
            IReadOnlyList<PauseCategoryDto> categories = null)
        {
            IsPaused = isPaused;
            PausableSubscriberCount = pausableSubscriberCount;
            Categories = categories ?? Array.Empty<PauseCategoryDto>();
        }

        /// <summary> どれか1つでもポーズ中かどうか。 </summary>
        internal bool IsPaused { get; }

        /// <summary> ポーズ通知を購読している対象の件数。 </summary>
        internal int PausableSubscriberCount { get; }

        /// <summary> カテゴリーごとの表示値。 </summary>
        /// <remarks>
        ///     **表示名の昇順で並ぶ。** 辞書の列挙順は保証されないため、
        ///     並びが揺れると内容が同じでもViewModelが変化として通知してしまう。
        /// </remarks>
        internal IReadOnlyList<PauseCategoryDto> Categories { get; }

        /// <summary>
        ///     表示値が等しいか判定する。
        /// </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> 全ての表示値が等しい場合はtrue。 </returns>
        public bool Equals(PauseDto other) =>
            IsPaused == other.IsPaused
            && PausableSubscriberCount == other.PausableSubscriberCount
            && HasSameCategories(other);

        /// <summary>
        ///     表示値が等しいか判定する。
        /// </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同じ型で表示値が等しい場合はtrue。 </returns>
        public override bool Equals(object obj) => obj is PauseDto other && Equals(other);

        /// <summary>
        ///     表示値に基づくハッシュコードを返す。
        /// </summary>
        /// <returns> ハッシュコード。 </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (IsPaused.GetHashCode() * 397) ^ PausableSubscriberCount;

                // 件数だけを混ぜる。要素まで混ぜても等価判定は要素比較で行うため、
                // ハッシュの分布を細かくする利得より、表示更新ごとの計算量を抑える方を採る。
                return (hashCode * 397) ^ Categories.Count;
            }
        }

        /// <summary>
        ///     2つの表示値が等しいか判定する。
        /// </summary>
        /// <param name="left"> 左辺。 </param>
        /// <param name="right"> 右辺。 </param>
        /// <returns> 等しい場合はtrue。 </returns>
        public static bool operator ==(PauseDto left, PauseDto right) => left.Equals(right);

        /// <summary>
        ///     2つの表示値が異なるか判定する。
        /// </summary>
        /// <param name="left"> 左辺。 </param>
        /// <param name="right"> 右辺。 </param>
        /// <returns> 異なる場合はtrue。 </returns>
        public static bool operator !=(PauseDto left, PauseDto right) => !left.Equals(right);

        #endregion

        #region 内部処理

        /// <summary>
        ///     カテゴリーの表示値が順序も含めて等しいか判定する。
        /// </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> 全ての要素が同じ順序で等しい場合はtrue。 </returns>
        /// <remarks>
        ///     **参照比較にしない。** ViewModelは内容の変化だけを通知するため、
        ///     毎回新しい一覧を作る Query の戻り値を参照で比べると常に変化扱いになる。
        /// </remarks>
        private bool HasSameCategories(PauseDto other)
        {
            if (Categories.Count != other.Categories.Count) { return false; }

            for (int index = 0; index < Categories.Count; index++)
            {
                if (!Categories[index].Equals(other.Categories[index])) { return false; }
            }

            return true;
        }

        #endregion
    }
}
