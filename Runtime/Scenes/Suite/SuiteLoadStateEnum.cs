using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Scenes
{
    /// <summary>
    ///     Suiteのロードおよびアンロード状態を表す。
    /// </summary>
    // 6.x の名前（SymphonyFrameWork.System.SceneBlock.SceneBlockLoadStateEnum）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.System.SceneBlock",
        sourceAssembly: "SymphonyFrameWork",
        sourceClassName: "SceneBlockLoadStateEnum")]
    public enum SuiteLoadStateEnum : int
    {
        #region 外部向けAPI

        /// <summary> 追跡されていない状態。 </summary>
        None = -1,

        /// <summary> ブロックのシーンをロードしている状態。 </summary>
        Loading = 0,

        /// <summary> ブロックの全シーンのロードが完了した状態。 </summary>
        Complete = 1,

        /// <summary> ブロックのシーンをアンロードしている状態。 </summary>
        Unloading = 2

        #endregion
    }
}
