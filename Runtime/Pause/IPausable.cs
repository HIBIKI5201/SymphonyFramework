namespace SymphonyFramework.Pause
{
    /// <summary>
    ///     ポーズ状態の変更へ追従する対象を表す。
    /// </summary>
    /// <remarks>
    ///     <see cref="Fermata.Register"/>で登録すると、実装しているカテゴリーのポーズ状態の変化が通知される。
    ///     カテゴリーはこのinterfaceを継承した空のinterfaceで表す。
    /// </remarks>
    public interface IPausable
    {
        #region 外部向けAPI

        /// <summary>
        ///     ポーズ開始時の処理を実行する。
        /// </summary>
        void Pause();

        /// <summary>
        ///     ポーズ解除時の処理を実行する。
        /// </summary>
        void Resume();

        #endregion
    }
}
