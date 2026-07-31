namespace SymphonyFrameWork.Editor
{
    /// <summary> Framework配下のアセット移動に対する保護の強さ。 </summary>
    public enum AssetProtectionModeEnum
    {
        /// <summary> 移動を常に差し戻す。 </summary>
        Enabled,

        /// <summary> ダイアログで続行するか差し戻すかを選ばせる。 </summary>
        Warning,

        /// <summary> 移動を通し、Consoleへ通常ログを出す。 </summary>
        Disabled,
    }
}
