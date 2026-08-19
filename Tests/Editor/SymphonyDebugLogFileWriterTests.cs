using System;
using System.IO;

using SymphonyFrameWork.Core;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     Debugログファイルの保存先を検証する。
    /// </summary>
    public sealed class SymphonyDebugLogFileWriterTests
    {
        /// <summary>
        ///     プロジェクトのAssetsパスからLibrary配下のログパスを解決する。
        /// </summary>
        [Test]
        public void ResolveDebugLogFileAbsolutePath_ProjectAssetsPath_ReturnsLibraryLogPath()
        {
            string projectRootPath = Path.Combine(Path.GetTempPath(), "SymphonyProject");
            string assetsPath = Path.Combine(projectRootPath, "Assets");
            string expectedPath = Path.GetFullPath(
                Path.Combine(projectRootPath, EditorSymphonyConstant.DEBUG_LOG_FILE_PATH));

            string actualPath = EditorSymphonyConstant.ResolveDebugLogFileAbsolutePath(assetsPath);

            Assert.That(actualPath, Is.EqualTo(expectedPath));
        }

        /// <summary>
        ///     解決したログパスをAssetsディレクトリ配下へ置かない。
        /// </summary>
        [Test]
        public void ResolveDebugLogFileAbsolutePath_ProjectAssetsPath_DoesNotReturnPathUnderAssets()
        {
            string projectRootPath = Path.Combine(Path.GetTempPath(), "SymphonyProject");
            string assetsPath = Path.GetFullPath(Path.Combine(projectRootPath, "Assets"));
            string assetsPrefix = assetsPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            string actualPath = EditorSymphonyConstant.ResolveDebugLogFileAbsolutePath(assetsPath);

            Assert.That(
                actualPath.StartsWith(assetsPrefix, StringComparison.OrdinalIgnoreCase),
                Is.False);
        }
    }
}
