using NUnit.Framework;

using SymphonyFrameWork.System.SceneBlock;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SceneBlockEdgeの値等値性を検証する。
    /// </summary>
    public sealed class SceneBlockEdgeTests
    {
        /// <summary>
        ///     始点と終点が同じ依存辺を同値と判定する。
        /// </summary>
        [Test]
        public void Equals_SameFromAndTo_ReturnsTrue()
        {
            SceneBlockEdge left = new("A", "B");
            SceneBlockEdge right = new("A", "B");

            Assert.That(left, Is.EqualTo(right));
        }

        /// <summary>
        ///     終点が異なる依存辺を別の値と判定する。
        /// </summary>
        [Test]
        public void Equals_DifferentTo_ReturnsFalse()
        {
            SceneBlockEdge left = new("A", "B");
            SceneBlockEdge right = new("A", "C");

            Assert.That(left, Is.Not.EqualTo(right));
        }
    }
}
