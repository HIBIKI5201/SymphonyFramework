using SymphonyFramework.Core;

using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     開発者ごとのFramework設定を保持する。
    /// </summary>
    [FilePath(
        EditorSymphonyConstant.USER_SETTING_FILE_PATH + nameof(SymphonyUserSettingConfig) + ".asset",
        FilePathAttribute.Location.ProjectFolder)]
    // 6.x の名前（SymphonyFrameWork.Editor）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Editor",
        sourceAssembly: "SymphonyFrameWork.Editor")]
    public sealed class SymphonyUserSettingConfig : ScriptableSingleton<SymphonyUserSettingConfig>
    {
        #region 外部向けAPI

        /// <summary> Framework配下のアセット移動に対する保護モード。 </summary>
        public AssetProtectionModeEnum AssetProtectionMode
        {
            get => _assetProtectionMode;
            set
            {
                _assetProtectionMode = value;
                Save();
            }
        }

        /// <summary> Partituraへのインスタンス登録ログを出力するか。 </summary>
        public bool IsPartituraRegisterLogEnabled
        {
            get => _isServiceLocatorSetInstanceLogEnabled;
            set
            {
                _isServiceLocatorSetInstanceLogEnabled = value;
                Save();
            }
        }

        /// <summary> Partituraからのインスタンス取得ログを出力するか。 </summary>
        public bool IsPartituraGetLogEnabled
        {
            get => _isServiceLocatorGetInstanceLogEnabled;
            set
            {
                _isServiceLocatorGetInstanceLogEnabled = value;
                Save();
            }
        }

        /// <summary> Partituraからのインスタンス破棄ログを出力するか。 </summary>
        public bool IsPartituraUnregisterLogEnabled
        {
            get => _isServiceLocatorDestroyInstanceLogEnabled;
            set
            {
                _isServiceLocatorDestroyInstanceLogEnabled = value;
                Save();
            }
        }

        /// <summary> 非接続で編集したセーブデータを、Play Mode突入時に保存先へ書き出すか。 </summary>
        public bool IsSaveDataPlayModeCarryOverEnabled
        {
            get => _isSaveDataPlayModeCarryOverEnabled;
            set
            {
                _isSaveDataPlayModeCarryOverEnabled = value;
                Save();
            }
        }

        #endregion

        #region 内部処理

        [SerializeField, Tooltip("Framework配下のアセット移動に対する保護モード。")]
        private AssetProtectionModeEnum _assetProtectionMode = AssetProtectionModeEnum.Enabled;

        // 保存済みの UserSettings を読めるよう、シリアライズ名は 6.x の名前のまま残す。
        [SerializeField, Tooltip("Partituraへのインスタンス登録ログを出力するか。")]
        private bool _isServiceLocatorSetInstanceLogEnabled = true;

        [SerializeField, Tooltip("Partituraからのインスタンス取得ログを出力するか。")]
        private bool _isServiceLocatorGetInstanceLogEnabled;

        [SerializeField, Tooltip("Partituraからのインスタンス破棄ログを出力するか。")]
        private bool _isServiceLocatorDestroyInstanceLogEnabled = true;

        [SerializeField, Tooltip("非接続で編集したセーブデータをPlay Modeへ持ち越すか。")]
        private bool _isSaveDataPlayModeCarryOverEnabled;

        /// <summary>
        ///     現在の設定値をUserSettingsへ保存する。
        /// </summary>
        private void Save() => Save(true);

        #endregion
    }
}
