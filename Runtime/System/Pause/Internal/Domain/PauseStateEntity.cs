namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ状態と変更判定を保持する。
    /// </summary>
    /// <remarks> 同じ値の再設定を変更なしとして扱い、通知の重複を防ぐ。 </remarks>
    internal sealed class PauseStateEntity
    {
        #region 外部向けAPI

        /// <summary> 現在ポーズ中かどうか。 </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        ///     ポーズ状態を設定する。
        /// </summary>
        /// <param name="isPaused"> 設定するポーズ状態。 </param>
        /// <returns> 状態が変化した場合はtrue。同じ値の場合はfalse。 </returns>
        public bool SetPaused(bool isPaused)
        {
            // 現在値と同じ設定では、状態変更通知を発行する必要がない。
            if (IsPaused == isPaused) { return false; }

            // 変更の確定後にServiceが通知できるよう、新しい状態を先に保持する。
            IsPaused = isPaused;
            return true;
        }

        /// <summary>
        ///     非ポーズ状態へ戻す。
        /// </summary>
        public void Reset()
        {
            IsPaused = false;
        }

        #endregion
    }
}
