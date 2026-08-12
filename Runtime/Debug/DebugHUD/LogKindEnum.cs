namespace SymphonyFrameWork.Debugger.Logger
{
    /// <summary>
    ///     出力するログの重要度を表す。
    /// </summary>
    public enum LogKindEnum
    {
        #region 外部向けAPI

        /// <summary> 通常ログとして出力する。 </summary>
        Normal,

        /// <summary> 警告ログとして出力する。 </summary>
        Warning,

        /// <summary> エラーログとして出力する。 </summary>
        Error,

        #endregion
    }
}
