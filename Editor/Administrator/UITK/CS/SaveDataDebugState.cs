using SymphonyFramework.SaveData;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     SerializedObjectでセーブデータを編集するための一時コンテナ。
    /// </summary>
    // 6.x の名前（SymphonyFrameWork.Editor）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Editor",
        sourceAssembly: "SymphonyFrameWork.Editor")]
    public sealed class SaveDataDebugState : ScriptableObject
    {
        #region 外部向けAPI

        /// <summary>
        ///     現在保持しているデバッグ対象データを取得する。
        /// </summary>
        public SaveDataContent GetData() => _data;

        /// <summary>
        ///     管理パネルで表示・編集するデータを設定する。
        /// </summary>
        public void SetData(SaveDataContent data)
        {
            _data = data;
        }

        #endregion

        #region 内部処理

        [SerializeReference, Tooltip("管理パネルで表示・編集するセーブデータの一時参照。")]
        private SaveDataContent _data;

        #endregion
    }
}
