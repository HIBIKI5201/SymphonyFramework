namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ状態そのものを表す。
    ///     状態が実際に変化したかどうかの判定をここへ閉じ込め、
    ///     同じ値の再設定で通知が重複しないようにする。
    /// </summary>
    internal sealed class PauseStateEntity
    {
        /// <summary> 現在ポーズ中かどうか。 </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        ///     ポーズ状態を設定する。
        /// </summary>
        /// <param name="isPaused"> 設定するポーズ状態。 </param>
        /// <returns> 状態が変化した場合はtrue。同じ値の場合はfalse。 </returns>
        public bool SetPaused(bool isPaused)
        {
            if (IsPaused == isPaused)
            {
                return false;
            }

            IsPaused = isPaused;
            return true;
        }

        /// <summary> 非ポーズ状態へ戻す。 </summary>
        public void Reset()
        {
            IsPaused = false;
        }
    }
}
