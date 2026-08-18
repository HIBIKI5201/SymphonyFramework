#if UNITY_6000_3_OR_NEWER
using SymphonyFrameWork.Core;

using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Symphony Frameworkのメインツールバー入口を登録する。
    /// </summary>
    internal static class SymphonyMainToolbar
    {
        #region 外部向けAPI

        /// <summary> プルダウンの登録パス。 </summary>
        internal const string ToolbarMenuElementPath =
            "Symphony Framework/Menu";

        /// <summary> プルダウンへ表示する名前。 </summary>
        internal const string ToolbarMenuLabel = "Symphony Framework";

        /// <summary>
        ///     Symphony Frameworkの操作をまとめたプルダウンを生成する。
        /// </summary>
        /// <returns> メインツールバーへ表示するプルダウン。 </returns>
        [MainToolbarElement(
            ToolbarMenuElementPath,
            defaultDockPosition = MainToolbarDockPosition.Right)]
        internal static MainToolbarDropdown CreateToolbarMenu()
        {
            Texture2D icon = LoadToolbarIcon(EditorGUIUtility.isProSkin);
            MainToolbarContent content = new(
                ToolbarMenuLabel,
                icon,
                "Symphony FrameworkのEditor操作を開きます。");

            return new MainToolbarDropdown(content, ShowMenu);
        }

        /// <summary>
        ///     現在のEditorテーマに対応するツールバーアイコンを読み込む。
        /// </summary>
        /// <param name="isProSkin"> 暗色テーマの場合はtrue。 </param>
        /// <returns> 対応するアイコン。読み込めない場合はnull。 </returns>
        internal static Texture2D LoadToolbarIcon(bool isProSkin)
        {
            string fileName = isProSkin
                ? "music-2-dark.png"
                : "music-2-light.png";
            string path = EditorSymphonyConstant.FRAMEWORK_PATH
                + "/Editor/Toolbar/Icons/"
                + fileName;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     登録済みの操作を現在状態からメニューへ構築する。
        /// </summary>
        /// <param name="position"> プルダウンを表示するツールバー上の位置。 </param>
        private static void ShowMenu(Rect position)
        {
            GenericMenu menu = new();
            foreach (ISymphonyToolbarMenuItem item
                     in SymphonyToolbarMenuCatalog.CreateItems())
            {
                GUIContent content = new(item.Path);
                if (item.IsEnabled)
                {
                    menu.AddItem(content, item.IsChecked, item.Execute);
                }
                else
                {
                    menu.AddDisabledItem(content, item.IsChecked);
                }
            }

            menu.DropDown(position);
        }

        #endregion
    }
}
#endif
