namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     アセット移動に対する保護の強さを表す。
    /// </summary>
    public enum AssetProtectionModeEnum
    {
        #region 外部向けAPI

        /// <summary> 移動を常に差し戻す。 </summary>
        Enabled,

        /// <summary> ダイアログで続行するか差し戻すかを選ばせる。 </summary>
        Warning,

        /// <summary> 移動を通し、Consoleへ通常ログを出す。 </summary>
        Disabled,

        #endregion
    }
}
