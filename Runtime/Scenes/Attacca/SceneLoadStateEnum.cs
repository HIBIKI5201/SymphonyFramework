using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Scenes
{
    /// <summary>
    ///     追跡中シーンのロードおよびアンロード状態を表す。
    /// </summary>
    // 6.x の名前（SymphonyFrameWork.System.SceneLoad）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.System.SceneLoad",
        sourceAssembly: "SymphonyFrameWork")]
    public enum SceneLoadStateEnum : int
    {
        #region 外部向けAPI

        /// <summary> 追跡されていない状態。 </summary>
        None = -1,

        /// <summary> シーンをロードしている状態。 </summary>
        Loading = 0,

        /// <summary> シーンのロードが完了した状態。 </summary>
        Complete = 1,

        /// <summary> シーンをアンロードしている状態。 </summary>
        Unloading = 2

        #endregion
    }
}
