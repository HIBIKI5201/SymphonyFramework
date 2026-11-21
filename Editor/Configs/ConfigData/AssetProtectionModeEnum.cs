using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     アセット移動に対する保護の強さを表す。
    /// </summary>
    // 6.x の名前（SymphonyFrameWork.Editor）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Editor",
        sourceAssembly: "SymphonyFrameWork.Editor")]
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
