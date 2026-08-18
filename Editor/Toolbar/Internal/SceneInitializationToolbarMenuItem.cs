#if UNITY_6000_3_OR_NEWER
using System;

using SymphonyFrameWork.Config;

using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     次回のPlay Mode開始時に初期シーン処理を行うか切り替える。
    /// </summary>
    [SymphonyToolbarMenuItem]
    internal sealed class SceneInitializationToolbarMenuItem
        : ISymphonyToolbarMenuItem
    {
        #region 外部向けAPI

        /// <summary> メニュー内の表示パス。 </summary>
        public string Path => "Scene Init";

        /// <summary> Scene Load操作の表示優先度。 </summary>
        public int Priority => 100;

        /// <summary> 初期シーン処理が有効な場合はtrue。 </summary>
        public bool IsChecked =>
            _config != null && _config.IsResetAndLoadOnPlay;

        /// <summary> 設定を取得でき、操作可能な場合はtrue。 </summary>
        public bool IsEnabled => _config != null;

        /// <summary>
        ///     初期シーン処理の有効状態を反転して保存する。
        /// </summary>
        public void Execute()
        {
            if (!ApplySceneInitializationValue(_config, !IsChecked))
            {
                return;
            }

            _saveAssets();
        }

        /// <summary>
        ///     初期シーン処理の有効状態をConfigへ適用する。
        /// </summary>
        /// <param name="config"> 変更するScene Load設定。 </param>
        /// <param name="value"> 適用する有効状態。 </param>
        /// <returns> Configへ値を適用できた場合はtrue。 </returns>
        internal static bool ApplySceneInitializationValue(
            SceneLoadConfig config,
            bool value)
        {
            if (config == null) { return false; }

            SerializedObject serializedConfig = new(config);
            serializedConfig.Update();
            SerializedProperty property = serializedConfig.FindProperty(
                "_isResetAndLoadOnPlay");
            if (property == null) { return false; }

            Undo.RecordObject(config, "Toggle Symphony Scene Initialization");
            property.boolValue = value;
            serializedConfig.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
            return true;
        }

        #endregion

        #region 内部処理

        private readonly SceneLoadConfig _config;
        private readonly Action _saveAssets;

        /// <summary>
        ///     現在のScene Load設定を使用する項目を生成する。
        /// </summary>
        internal SceneInitializationToolbarMenuItem()
            : this(
                SymphonyConfigLocator.GetConfig<SceneLoadConfig>(),
                AssetDatabase.SaveAssets)
        {
        }

        /// <summary>
        ///     指定したScene Load設定を使用する項目を生成する。
        /// </summary>
        /// <param name="config"> 表示と変更に使用する設定。 </param>
        /// <param name="saveAssets"> 変更後の保存処理。 </param>
        internal SceneInitializationToolbarMenuItem(
            SceneLoadConfig config,
            Action saveAssets)
        {
            _config = config;
            _saveAssets = saveAssets ?? throw new ArgumentNullException(
                nameof(saveAssets));
        }

        #endregion
    }
}
#endif
