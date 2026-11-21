using System.Threading;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework
{
    /// <summary>
    ///     GameObjectを所有するUnityオブジェクトの共通参照を公開する。
    /// </summary>
    // 6.x の名前（SymphonyFrameWork）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork",
        sourceAssembly: "SymphonyFrameWork")]
    public interface IGameObject
    {
        #region 外部向けAPI

        /// <summary> 所有しているGameObject。 </summary>
        GameObject gameObject { get; }

        /// <summary> 所有しているTransform。 </summary>
        Transform transform { get; }

        /// <summary> 所有GameObjectの破棄時にキャンセルされるトークン。 </summary>
        CancellationToken destroyCancellationToken { get; }

        #endregion
    }
}
