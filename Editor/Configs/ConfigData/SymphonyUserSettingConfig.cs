using SymphonyFrameWork.Core;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     開発者ごとのFramework設定を保持する。
    /// </summary>
    [FilePath(
        EditorSymphonyConstant.USER_SETTING_FILE_PATH + nameof(SymphonyUserSettingConfig) + ".asset",
        FilePathAttribute.Location.ProjectFolder)]
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

        /// <summary> Service Locatorへのインスタンス登録ログを出力するか。 </summary>
        public bool IsServiceLocatorSetInstanceLogEnabled
        {
            get => _isServiceLocatorSetInstanceLogEnabled;
            set
            {
                _isServiceLocatorSetInstanceLogEnabled = value;
                Save();
            }
        }

        /// <summary> Service Locatorからのインスタンス取得ログを出力するか。 </summary>
        public bool IsServiceLocatorGetInstanceLogEnabled
        {
            get => _isServiceLocatorGetInstanceLogEnabled;
            set
            {
                _isServiceLocatorGetInstanceLogEnabled = value;
                Save();
            }
        }

        /// <summary> Service Locatorからのインスタンス破棄ログを出力するか。 </summary>
        public bool IsServiceLocatorDestroyInstanceLogEnabled
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

        [SerializeField, Tooltip("Service Locatorへのインスタンス登録ログを出力するか。")]
        private bool _isServiceLocatorSetInstanceLogEnabled = true;

        [SerializeField, Tooltip("Service Locatorからのインスタンス取得ログを出力するか。")]
        private bool _isServiceLocatorGetInstanceLogEnabled;

        [SerializeField, Tooltip("Service Locatorからのインスタンス破棄ログを出力するか。")]
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
