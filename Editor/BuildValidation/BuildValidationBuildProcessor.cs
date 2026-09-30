using System.Text;

using SymphonyFrameWork.Debugger.Logger;

using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     有効な検証だけをビルド前に実行する。
    /// </summary>
    internal sealed class BuildValidationBuildProcessor : IPreprocessBuildWithReport
    {
        #region 外部向けAPI

        /// <summary> 他のビルド前処理より先に検証する実行順。 </summary>
        public int callbackOrder => VALIDATION_ORDER;

        /// <summary>
        ///     全検証結果を集めてからビルドを停止する。
        /// </summary>
        /// <exception cref="BuildFailedException"> 有効な検証で不備を検出した場合。 </exception>
        public void OnPreprocessBuild(BuildReport report)
        {
            BuildValidationConfig config = BuildValidationConfig.instance;
            StringBuilder errors = new();
            if (config.IsUxmlValidationOnBuild)
            {
                CollectReport(UxmlDependencyValidator.Validate(config), errors);
            }

            if (config.IsFontValidationOnBuild)
            {
                CollectReport(FontSettingsValidator.Validate(config), errors);
            }

            if (errors.Length != 0) { throw new BuildFailedException(errors.ToString()); }
        }

        #endregion

        #region 内部処理

        private const int VALIDATION_ORDER = -300;

        /// <summary>
        ///     不備をまとめ、成功時も検査を省略した理由をログへ残す。
        /// </summary>
        private static void CollectReport(BuildValidationReport report, StringBuilder errors)
        {
            if (report.HasErrors) { errors.AppendLine(report.CreateMessage()); }
            else { SymphonyDebugLogger.LogDirect(report.CreateMessage()); }
        }

        #endregion
    }
}
