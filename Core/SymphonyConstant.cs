namespace SymphonyFrameWork.Core
{
    /// <summary>
    ///     ランタイム用の定数値を持つ。
    /// </summary>
    public static class SymphonyConstant
    {
        #region 外部向けAPI

        /// <summary> Unity Package Managerで使用するパッケージ名。 </summary>
        public const string SYMPHONY_PACKAGE = "symphonyframework";

        /// <summary> Frameworkの表示名およびAssets配下のルート名。 </summary>
        public const string SYMPHONY_FRAMEWORK = "SymphonyFrameWork";

        /// <summary> Frameworkのバージョン。 </summary>
        /// <remarks>
        ///     <c>package.json</c> の <c>version</c> と同じ値を持つ。Playerビルドに
        ///     <c>package.json</c> は含まれないため、実行時に読み取ることができない。
        ///     更新は <c>scripts/release_round.py bump</c> が行い、
        ///     <c>preflight</c> が <c>package.json</c> との一致を検査する。手で書き換えないこと。
        /// </remarks>
        public const string VERSION = "6.3.2";

#if UNITY_EDITOR
        /// <summary>
        ///     Frameworkの絶対パスを取得する。
        /// </summary>
        /// <remarks> UPM経由とAssets直置きの両方に対応する。 </remarks>
        /// <returns> Frameworkルートフォルダの絶対パス。 </returns>
        public static string GetFrameworkAbsolutePath()
        {
            // UPMが解決した配置先を優先し、PackageCache内の実ファイルへ到達できるようにする。
            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo
                .FindForAssembly(typeof(SymphonyConstant).Assembly);

            // 対象アセンブリがUPMパッケージとして認識されている場合は解決済みパスを使用する。
            if (packageInfo != null) { return packageInfo.resolvedPath; }

            // UPMで解決できない場合は、開発用にAssets直下へ配置されているとみなす。
            return System.IO.Path.Combine(UnityEngine.Application.dataPath, SYMPHONY_FRAMEWORK);
        }
#endif

        /// <summary> Runtime設定アセットを配置するResourcesディレクトリ。 </summary>
        public const string RESOURCES_RUNTIME_PATH = "Assets/Resources/" + SYMPHONY_FRAMEWORK;

        /// <summary> FrameworkのToolsメニュー基準パス。 </summary>
        public const string TOOL_MENU_PATH = "Tools/" + SYMPHONY_FRAMEWORK + "/";

        /// <summary> Framework設定用Toolsメニューの基準パス。 </summary>
        public const string TOOL_MENU_SETTING_PATH = TOOL_MENU_PATH + "Settings/";

        /// <summary> FrameworkのEditorWindowメニュー基準パス。 </summary>
        public const string WINDOW_MENU_PATH = "Window/" + SYMPHONY_FRAMEWORK + "/";

        #endregion
    }
}
