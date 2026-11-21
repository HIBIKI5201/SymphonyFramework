using UnityEditor;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     Editor用ScriptableSingleton設定へのアクセスを提供する。
    /// </summary>
    // 6.x の名前（SymphonyFrameWork.Editor.SymphonyEditorConfigLocator）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Editor",
        sourceAssembly: "SymphonyFrameWork.Editor",
        sourceClassName: "SymphonyEditorConfigLocator")]
    public static class SymphonyEditorConfigProvider
    {
        #region 外部向けAPI

        /// <summary>
        ///     指定した型の設定を取得する。
        /// </summary>
        /// <typeparam name="T"> 取得するEditor設定の型。 </typeparam>
        /// <returns> 対象型の共有設定インスタンス。 </returns>
        public static T GetConfig<T>() where T : ScriptableSingleton<T>
        {
            return ScriptableSingleton<T>.instance;
        }

        #endregion
    }
}
