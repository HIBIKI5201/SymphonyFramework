using NUnit.Framework;

using SymphonyFramework.Scenes;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     SuiteEdgeの値等値性を検証する。
    /// </summary>
    public sealed class SuiteEdgeTests
    {
        /// <summary>
        ///     始点と終点が同じ依存辺を同値と判定する。
        /// </summary>
        [Test]
        public void Equals_SameFromAndTo_ReturnsTrue()
        {
            SuiteEdge left = new("A", "B");
            SuiteEdge right = new("A", "B");

            Assert.That(left, Is.EqualTo(right));
        }

        /// <summary>
        ///     終点が異なる依存辺を別の値と判定する。
        /// </summary>
        [Test]
        public void Equals_DifferentTo_ReturnsFalse()
        {
            SuiteEdge left = new("A", "B");
            SuiteEdge right = new("A", "C");

            Assert.That(left, Is.Not.EqualTo(right));
        }
    }
}
