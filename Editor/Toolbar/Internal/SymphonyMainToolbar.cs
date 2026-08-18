#if UNITY_6000_3_OR_NEWER
using SymphonyFrameWork.Config;

using UnityEditor;
using UnityEditor.Toolbars;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Symphony Frameworkのメインツールバー要素を登録する。
    /// </summary>
    internal static class SymphonyMainToolbar
    {
        #region 外部向けAPI

        /// <summary> 初期シーン処理トグルの登録パス。 </summary>
        internal const string SceneInitializationElementPath =
            "Symphony Framework/Scene Init";

        /// <summary>
        ///     初期シーン処理の有効状態を表すトグルを生成する。
        /// </summary>
        /// <returns> メインツールバーへ表示するトグル。 </returns>
        [MainToolbarElement(
            SceneInitializationElementPath,
            defaultDockPosition = MainToolbarDockPosition.Right)]
        internal static MainToolbarElement CreateSceneInitializationToggle()
        {
            SceneLoadConfig config =
                SymphonyConfigLocator.GetConfig<SceneLoadConfig>();
            bool isEnabled = config != null && config.IsResetAndLoadOnPlay;
            MainToolbarContent content = new(
                "Scene Init",
                "次回のPlay Mode開始時に初期シーン処理を実行するか切り替えます。");

            MainToolbarToggle toggle = new(
                content,
                isEnabled,
                SetSceneInitializationEnabled)
            {
                // Configが未生成でもツールバー全体の構築を止めず、操作だけを無効化する。
                enabled = config != null,
            };
            return toggle;
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

        /// <summary>
        ///     初期シーン処理トグルを現在のConfigから再構築する。
        /// </summary>
        internal static void RefreshSceneInitializationToggle()
        {
            MainToolbar.Refresh(SceneInitializationElementPath);
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     ツールバー操作をScene Load設定へ保存する。
        /// </summary>
        /// <param name="value"> 適用する有効状態。 </param>
        private static void SetSceneInitializationEnabled(bool value)
        {
            SceneLoadConfig config =
                SymphonyConfigLocator.GetConfig<SceneLoadConfig>();
            if (ApplySceneInitializationValue(config, value))
            {
                AssetDatabase.SaveAssets();
            }

            // 保存成否を含む現在値から表示を再生成し、操作値だけが残らないようにする。
            RefreshSceneInitializationToggle();
        }

        #endregion
    }
}
#endif
