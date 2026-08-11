namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     Service LocatorのEditor向けログ出力可否を保持する。
    /// </summary>
    internal static class ServiceLocateLogOption
    {
        #region 外部向けAPI

        /// <summary> インスタンス登録ログを出力するか。 </summary>
        internal static bool IsSetInstanceLogEnabled { get; set; } = true;

        /// <summary> インスタンス取得ログを出力するか。 </summary>
        internal static bool IsGetInstanceLogEnabled { get; set; }

        /// <summary> インスタンス破棄ログを出力するか。 </summary>
        internal static bool IsDestroyInstanceLogEnabled { get; set; } = true;

        #endregion
    }
}
