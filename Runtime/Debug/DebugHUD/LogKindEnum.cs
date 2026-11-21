using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Debugger.Logger
{
    /// <summary>
    ///     出力するログの重要度を表す。
    /// </summary>
    // 6.x の名前（SymphonyFrameWork.Debugger.Logger）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Debugger.Logger",
        sourceAssembly: "SymphonyFrameWork")]
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
