using NUnit.Framework;

using SymphonyFrameWork.System.SceneBlock;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SceneBlockEdgeAuthoringの値保持を検証する。
    /// </summary>
    public sealed class SceneBlockEdgeAuthoringTests
    {
        /// <summary> コンストラクタに渡した依存元と依存先を公開する。 </summary>
        [Test]
        public void Constructor_ExposesFromAndTo()
        {
            SceneBlockEdgeAuthoring edge = new("A", "B");

            Assert.That(edge.From, Is.EqualTo("A"));
            Assert.That(edge.To, Is.EqualTo("B"));
        }
    }
}
