using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     カテゴリーごとのポーズ状態と変更判定を保持する。
    /// </summary>
    /// <remarks> 同じ値の再設定を変更なしとして扱い、通知の重複を防ぐ。 </remarks>
    internal sealed class PauseStateEntity
    {
        #region 外部向けAPI

        /// <summary> 状態を持っているカテゴリー。 </summary>
        /// <remarks>
        ///     一度も設定していないカテゴリーは含まない。
        ///     **含まないことと非ポーズであることは同じ意味で扱う。**
        /// </remarks>
        public IReadOnlyCollection<Type> Categories => _pausedByCategory.Keys;

        /// <summary> どれか1つでもポーズ中かどうか。 </summary>
        public bool IsPausedAny
        {
            get
            {
                foreach (KeyValuePair<Type, bool> entry in _pausedByCategory)
                {
                    if (entry.Value) { return true; }
                }

                return false;
            }
        }

        /// <summary>
        ///     カテゴリーのポーズ状態を返す。
        /// </summary>
        /// <param name="category"> 対象のカテゴリー。 </param>
        /// <returns> ポーズ中の場合はtrue。未設定のカテゴリーはfalse。 </returns>
        /// <exception cref="ArgumentNullException"> categoryがnullの場合。 </exception>
        public bool IsPaused(Type category)
        {
            if (category == null) { throw new ArgumentNullException(nameof(category)); }

            // 一度も止めていないカテゴリーと、止めてから解除したカテゴリーを区別しない。
            return _pausedByCategory.TryGetValue(category, out bool isPaused) && isPaused;
        }

        /// <summary>
        ///     カテゴリーのポーズ状態を設定する。
        /// </summary>
        /// <param name="category"> 対象のカテゴリー。 </param>
        /// <param name="isPaused"> 設定するポーズ状態。 </param>
        /// <returns> 状態が変化した場合はtrue。同じ値の場合はfalse。 </returns>
        /// <exception cref="ArgumentNullException"> categoryがnullの場合。 </exception>
        public bool SetPaused(Type category, bool isPaused)
        {
            if (category == null) { throw new ArgumentNullException(nameof(category)); }

            // 現在値と同じ設定では、状態変更通知を発行する必要がない。
            if (IsPaused(category) == isPaused) { return false; }

            // 変更の確定後にServiceが通知できるよう、新しい状態を先に保持する。
            // **解除しても行は残す。** 一度でも操作したカテゴリーは、以後の一括操作の対象になる。
            _pausedByCategory[category] = isPaused;
            return true;
        }

        /// <summary>
        ///     全カテゴリーを非ポーズ状態へ戻す。
        /// </summary>
        public void Reset()
        {
            _pausedByCategory.Clear();
        }

        #endregion

        #region 内部処理

        private readonly Dictionary<Type, bool> _pausedByCategory = new();

        #endregion
    }
}
