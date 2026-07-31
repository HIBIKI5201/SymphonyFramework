using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.HUD;

using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    /// <summary> Symphony Debug HUDのEditorメニュー入口を提供する。 </summary>
    internal static class SymphonyDebugHUDMenu
    {
        private const string MENU_PATH =
            SymphonyConstant.TOOL_MENU_PATH + nameof(SymphonyDebugHUD) + "/";

        /// <summary> Symphony Debug HUDを表示する。 </summary>
        [MenuItem(MENU_PATH + nameof(SymphonyDebugHUD.Show))]
        private static void Show()
        {
            SymphonyDebugHUD.Show();
        }

        /// <summary> Symphony Debug HUDを非表示にする。 </summary>
        [MenuItem(MENU_PATH + nameof(SymphonyDebugHUD.Hide))]
        private static void Hide()
        {
            SymphonyDebugHUD.Hide();
        }
    }
}
