using System;

using NUnit.Framework;

using SymphonyFrameWork.System.SceneLoad;

namespace SymphonyFrameWork.Tests
{
    /// <summary> SceneLoadRequestの入力検証と値としての等値性を検証する。 </summary>
    public sealed class SceneLoadRequestTests
    {
        /// <summary> constructorへ渡したScene名と優先度を保持する。 </summary>
        [Test]
        public void Constructor_ValidValues_StoresValues()
        {
            var request = new SceneLoadRequest("Game", 10);

            Assert.That(request.SceneName, Is.EqualTo("Game"));
            Assert.That(request.Priority, Is.EqualTo(10));
        }

        /// <summary> null、空、空白のScene名を拒否する。 </summary>
        /// <param name="sceneName"> 無効なScene名。 </param>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidSceneName_Throws(string sceneName)
        {
            Assert.Throws<ArgumentException>(() => new SceneLoadRequest(sceneName));
        }

        /// <summary> Scene名と優先度が一致するRequestを同値と判定する。 </summary>
        [Test]
        public void Equals_SameSceneNameAndPriority_ReturnsTrue()
        {
            var left = new SceneLoadRequest("Game", 3);
            var right = new SceneLoadRequest("Game", 3);

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
        }

        /// <summary> 優先度が異なるRequestを別の値と判定する。 </summary>
        [Test]
        public void Equals_DifferentPriority_ReturnsFalse()
        {
            var left = new SceneLoadRequest("Game", 1);
            var right = new SceneLoadRequest("Game", 2);

            Assert.That(left, Is.Not.EqualTo(right));
            Assert.That(left != right, Is.True);
        }
    }
}
