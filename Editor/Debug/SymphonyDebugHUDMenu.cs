using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.HUD;

using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Symphony Debug HUDのEditorメニュー入口を提供する。
    /// </summary>
    internal static class SymphonyDebugHUDMenu
    {
        #region 外部向けAPI

        /// <summary>
        ///     Symphony Debug HUDを表示する。
        /// </summary>
        [MenuItem(MENU_PATH + nameof(SymphonyDebugHUD.Show))]
        private static void Show()
        {
            // UnityのMenuItemからRuntime HUDの公開入口へ処理を委譲する。
            SymphonyDebugHUD.Show();
        }

        /// <summary>
        ///     Symphony Debug HUDを非表示にする。
        /// </summary>
        [MenuItem(MENU_PATH + nameof(SymphonyDebugHUD.Hide))]
        private static void Hide()
        {
            // UnityのMenuItemからRuntime HUDの公開入口へ処理を委譲する。
            SymphonyDebugHUD.Hide();
        }

        #endregion

        #region 内部処理

        private const string MENU_PATH =
            SymphonyConstant.TOOL_MENU_PATH + nameof(SymphonyDebugHUD) + "/";

        #endregion
    }
}
