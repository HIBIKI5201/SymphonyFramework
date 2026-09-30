using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;

using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     ビルド検証を手動で実行するメニューを提供する。
    /// </summary>
    internal static class BuildValidationMenu
    {
        #region 内部処理

        /// <summary>
        ///     ビルド時の有効設定にかかわらずUXMLを検証する。
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + "Build Validation/Validate UXML Dependencies")]
        private static void ValidateUxml()
        {
            ShowReport(UxmlDependencyValidator.Validate(BuildValidationConfig.instance));
        }

        /// <summary>
        ///     ビルド時の有効設定にかかわらずフォントを検証する。
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + "Build Validation/Validate Fonts")]
        private static void ValidateFonts()
        {
            ShowReport(FontSettingsValidator.Validate(BuildValidationConfig.instance));
        }

        /// <summary>
        ///     検証結果をログとダイアログへ表示する。
        /// </summary>
        private static void ShowReport(BuildValidationReport report)
        {
            string message = report.CreateMessage();
            SymphonyDebugLogger.LogDirect(message, report.HasErrors ? LogKindEnum.Error : LogKindEnum.Normal);
            EditorUtility.DisplayDialog("Build Validation", message, "OK");
        }

        #endregion
    }
}
