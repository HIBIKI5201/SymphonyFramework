using NUnit.Framework;

using SymphonyFrameWork.System.SceneLoad;

namespace SymphonyFrameWork.Tests
{
    /// <summary> SceneLoadInfoの保持値と値等値性を検証する。 </summary>
    public sealed class SceneLoadInfoTests
    {
        /// <summary> Queryから渡されたScene状態を変更せず保持する。 </summary>
        [Test]
        public void Constructor_ValidValues_StoresSnapshot()
        {
            var sceneInfo = new SceneLoadInfo(
                "Game",
                SceneLoadState.Loading,
                5,
                0.25f,
                true);

            Assert.That(sceneInfo.SceneName, Is.EqualTo("Game"));
            Assert.That(sceneInfo.State, Is.EqualTo(SceneLoadState.Loading));
            Assert.That(sceneInfo.Priority, Is.EqualTo(5));
            Assert.That(sceneInfo.Progress, Is.EqualTo(0.25f));
            Assert.That(sceneInfo.IsActive, Is.True);
        }

        /// <summary> 全値が同じスナップショットは等値になる。 </summary>
        [Test]
        public void Equality_SameValues_ReturnsTrue()
        {
            var left = new SceneLoadInfo(
                "Game",
                SceneLoadState.Complete,
                2,
                1f,
                false);
            var right = new SceneLoadInfo(
                "Game",
                SceneLoadState.Complete,
                2,
                1f,
                false);

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        /// <summary> いずれかの値が異なるスナップショットは非等値になる。 </summary>
        [Test]
        public void Equality_DifferentProgress_ReturnsFalse()
        {
            var left = new SceneLoadInfo(
                "Game",
                SceneLoadState.Loading,
                2,
                0.25f,
                false);
            var right = new SceneLoadInfo(
                "Game",
                SceneLoadState.Loading,
                2,
                0.5f,
                false);

            Assert.That(left, Is.Not.EqualTo(right));
            Assert.That(left != right, Is.True);
        }

        /// <summary> 既定値同士も安全に比較できる。 </summary>
        [Test]
        public void Equality_DefaultValues_ReturnsTrue()
        {
            Assert.That(default(SceneLoadInfo), Is.EqualTo(default(SceneLoadInfo)));
        }
    }
}
