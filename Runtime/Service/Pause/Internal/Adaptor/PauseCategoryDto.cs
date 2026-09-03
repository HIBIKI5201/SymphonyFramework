using System;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     カテゴリー1件の表示用更新値を表す。
    /// </summary>
    internal readonly struct PauseCategoryDto : IEquatable<PauseCategoryDto>
    {
        #region 外部向けAPI

        /// <summary>
        ///     表示に必要な値を指定して生成する。
        /// </summary>
        /// <param name="categoryName"> カテゴリーの表示名。 </param>
        /// <param name="isPaused"> そのカテゴリーがポーズ中かどうか。 </param>
        /// <param name="pausableCount"> そのカテゴリーに属する対象の件数。 </param>
        internal PauseCategoryDto(string categoryName, bool isPaused, int pausableCount)
        {
            CategoryName = categoryName;
            IsPaused = isPaused;
            PausableCount = pausableCount;
        }

        /// <summary> カテゴリーの表示名。 </summary>
        internal string CategoryName { get; }

        /// <summary> そのカテゴリーがポーズ中かどうか。 </summary>
        internal bool IsPaused { get; }

        /// <summary> そのカテゴリーに属する対象の件数。 </summary>
        internal int PausableCount { get; }

        /// <summary>
        ///     表示値が等しいか判定する。
        /// </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> 全ての表示値が等しい場合はtrue。 </returns>
        public bool Equals(PauseCategoryDto other) =>
            string.Equals(CategoryName, other.CategoryName, StringComparison.Ordinal)
            && IsPaused == other.IsPaused
            && PausableCount == other.PausableCount;

        /// <summary>
        ///     表示値が等しいか判定する。
        /// </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同じ型で表示値が等しい場合はtrue。 </returns>
        public override bool Equals(object obj) => obj is PauseCategoryDto other && Equals(other);

        /// <summary>
        ///     表示値に基づくハッシュコードを返す。
        /// </summary>
        /// <returns> ハッシュコード。 </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = CategoryName != null ? CategoryName.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ IsPaused.GetHashCode();
                return (hashCode * 397) ^ PausableCount;
            }
        }

        /// <summary>
        ///     2つの表示値が等しいか判定する。
        /// </summary>
        /// <param name="left"> 左辺。 </param>
        /// <param name="right"> 右辺。 </param>
        /// <returns> 等しい場合はtrue。 </returns>
        public static bool operator ==(PauseCategoryDto left, PauseCategoryDto right) =>
            left.Equals(right);

        /// <summary>
        ///     2つの表示値が異なるか判定する。
        /// </summary>
        /// <param name="left"> 左辺。 </param>
        /// <param name="right"> 右辺。 </param>
        /// <returns> 異なる場合はtrue。 </returns>
        public static bool operator !=(PauseCategoryDto left, PauseCategoryDto right) =>
            !left.Equals(right);

        #endregion
    }
}
