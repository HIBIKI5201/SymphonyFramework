using System.IO;

using SymphonyFramework.Core;

using NUnit.Framework;

namespace SymphonyFramework.Tests
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
            "Pause",
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

        /// <summary> Partitura は Runtime/Services/ にあり、旧置き場には残っていない。 </summary>
        [Test]
        public void Partitura_LivesInRuntimeServices()
        {
            string runtime = Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "Runtime");

            Assert.That(
                File.Exists(Path.Combine(runtime, "Services", "Partitura.cs")),
                Is.True,
                "Partitura.cs が Runtime/Services/ にありません。");
            Assert.That(
                Directory.Exists(Path.Combine(runtime, "Service", "ServiceLocator")),
                Is.False,
                "Runtime/Service/ServiceLocator/ は Runtime/Services/ へ移しました（7.0.0、#225）。");
        }

        /// <summary> Attacca と Suite は Runtime/Scenes/ にあり、旧置き場には残っていない。 </summary>
        [Test]
        public void AttaccaAndSuite_LiveInRuntimeScenes()
        {
            string runtime = Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "Runtime");

            Assert.That(
                File.Exists(Path.Combine(runtime, "Scenes", "Attacca", "Attacca.cs")),
                Is.True,
                "Attacca.cs が Runtime/Scenes/Attacca/ にありません。");
            Assert.That(
                File.Exists(Path.Combine(runtime, "Scenes", "Suite", "Suite.cs")),
                Is.True,
                "Suite.cs が Runtime/Scenes/Suite/ にありません。");
            Assert.That(
                Directory.Exists(Path.Combine(runtime, "Service", "SceneLoader")),
                Is.False,
                "Runtime/Service/SceneLoader/ は Runtime/Scenes/Attacca/ へ移しました（7.0.0、#232）。");
            Assert.That(
                Directory.Exists(Path.Combine(runtime, "Service", "SceneBlock")),
                Is.False,
                "Runtime/Service/SceneBlock/ は Runtime/Scenes/Suite/ へ移しました（7.0.0、#232）。");
        }

        /// <summary> Manuscript と Sonority は Runtime/SaveData/ と Runtime/Audio/ にあり、旧置き場には残っていない。 </summary>
        [Test]
        public void ManuscriptAndSonority_LiveInOwnRuntimeFolders()
        {
            string runtime = Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "Runtime");

            Assert.That(
                File.Exists(Path.Combine(runtime, "SaveData", "Manuscript.cs")),
                Is.True,
                "Manuscript.cs が Runtime/SaveData/ にありません。");
            Assert.That(
                File.Exists(Path.Combine(runtime, "Audio", "Sonority.cs")),
                Is.True,
                "Sonority.cs が Runtime/Audio/ にありません。");
            Assert.That(
                Directory.Exists(Path.Combine(runtime, "Service", "SaveSystem")),
                Is.False,
                "Runtime/Service/SaveSystem/ は Runtime/SaveData/ へ移しました（7.0.0、#225）。");
            Assert.That(
                Directory.Exists(Path.Combine(runtime, "Service", "Audio")),
                Is.False,
                "Runtime/Service/Audio/ は Runtime/Audio/ へ移しました（7.0.0、#225）。");
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
