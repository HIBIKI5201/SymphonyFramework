namespace SymphonyFrameWork.Core
{
    /// <summary>
    ///     ランタイム用の定数値を持つ
    /// </summary>
    public static class SymphonyConstant
    {
        /// <summary> Unity Package Managerで使用するパッケージ名。 </summary>
        public const string SYMPHONY_PACKAGE = "symphonyframework";

        /// <summary> Frameworkの表示名およびAssets配下のルート名。 </summary>
        public const string SYMPHONY_FRAMEWORK = "SymphonyFrameWork";

        /// <summary> Runtime設定アセットを配置するResourcesディレクトリ。 </summary>
        public const string RESOURCES_RUNTIME_PATH = "Assets/Resources/" + SYMPHONY_FRAMEWORK;

        /// <summary> FrameworkのToolsメニュー基準パス。 </summary>
        public const string TOOL_MENU_PATH = "Tools/" + SYMPHONY_FRAMEWORK + "/";

        /// <summary> Framework設定用Toolsメニューの基準パス。 </summary>
        public const string TOOL_MENU_SETTING_PATH = TOOL_MENU_PATH + "Settings/";

        /// <summary> FrameworkのEditorWindowメニュー基準パス。 </summary>
        public const string WINDOW_MENU_PATH = "Window/" + SYMPHONY_FRAMEWORK + "/";
    }
}
