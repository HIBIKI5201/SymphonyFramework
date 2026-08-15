using System.IO;

using SymphonyFrameWork.Core;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     サブシステムの置き場が <c>Runtime/Service/</c> である契約を検証する。
    /// </summary>
    /// <remarks>
    ///     フォルダ名の変更は、移動漏れや旧名の再作成に気づきにくい。
    ///     <c>Runtime/System/</c> が復活していないことも併せて見る。
    /// </remarks>
    public sealed class RuntimeFolderLayoutTests
    {
        /// <summary> <c>Runtime/Service/</c> の直下にある想定のサブシステム。 </summary>
        private static readonly string[] SubsystemDirectories =
        {
            "Audio", "Pause", "SaveSystem", "SceneLoader", "ServiceLocator",
        };

        /// <summary> サブシステムはすべて Runtime/Service/ の下にある。 </summary>
        [Test]
        public void RuntimeService_Subsystems_AllExist()
        {
            string root = Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "Runtime", "Service");

            Assert.That(Directory.Exists(root), Is.True, $"'{root}' が見つかりません。");

            foreach (string subsystem in SubsystemDirectories)
            {
                Assert.That(
                    Directory.Exists(Path.Combine(root, subsystem)),
                    Is.True,
                    $"'{subsystem}' が Runtime/Service/ の下にありません。");
            }
        }

        /// <summary> 旧名の Runtime/System/ は残っていない。 </summary>
        [Test]
        public void RuntimeSystem_OldFolderName_DoesNotExist()
        {
            Assert.That(
                Directory.Exists(Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "Runtime", "System")),
                Is.False,
                "Runtime/System/ は Runtime/Service/ へ移しました（Issue #106）。");
        }
    }
}
