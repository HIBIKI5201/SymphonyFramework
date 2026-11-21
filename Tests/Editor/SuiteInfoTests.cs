using System;

using NUnit.Framework;

using SymphonyFramework.Scenes;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     SuiteInfoが取得時点の管理状態を値として表す契約を検証する。
    /// </summary>
    public sealed class SuiteInfoTests
    {
        /// <summary>
        ///     生成時の値をそのまま公開する。
        /// </summary>
        [Test]
        public void Constructor_ExposesValues()
        {
            SuiteInfo info = new(
                "Town",
                SuiteLoadStateEnum.Complete,
                1f,
                new[] { "Base", "Props" },
                2);

            Assert.That(info.SuiteName, Is.EqualTo("Town"));
            Assert.That(info.State, Is.EqualTo(SuiteLoadStateEnum.Complete));
            Assert.That(info.Progress, Is.EqualTo(1f));
            Assert.That(info.HeldSceneNames, Is.EqualTo(new[] { "Base", "Props" }));
            Assert.That(info.LayerCount, Is.EqualTo(2));
        }

        /// <summary>
        ///     保持シーンが未指定でも空の一覧として扱う。
        /// </summary>
        [Test]
        public void HeldSceneNames_Null_IsEmpty()
        {
            SuiteInfo info = new("Town", SuiteLoadStateEnum.Loading, 0f, null, 1);

            Assert.That(info.HeldSceneNames, Is.Empty);
        }

        /// <summary>
        ///     同じ値なら等しい。
        /// </summary>
        [Test]
        public void Equals_SameValues_AreEqual()
        {
            SuiteInfo left = new("Town", SuiteLoadStateEnum.Complete, 1f, new[] { "Base" }, 1);
            SuiteInfo right = new("Town", SuiteLoadStateEnum.Complete, 1f, new[] { "Base" }, 1);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        /// <summary>
        ///     保持シーンの内容が違えば等しくない。
        /// </summary>
        [Test]
        public void Equals_DifferentHeldScenes_AreNotEqual()
        {
            SuiteInfo left = new("Town", SuiteLoadStateEnum.Complete, 1f, new[] { "Base" }, 1);
            SuiteInfo right = new("Town", SuiteLoadStateEnum.Complete, 1f, new[] { "Props" }, 1);

            Assert.That(left == right, Is.False);
        }

        /// <summary>
        ///     状態が違えば等しくない。
        /// </summary>
        [Test]
        public void Equals_DifferentState_AreNotEqual()
        {
            SuiteInfo left = new("Town", SuiteLoadStateEnum.Complete, 1f, Array.Empty<string>(), 1);
            SuiteInfo right = new("Town", SuiteLoadStateEnum.Loading, 1f, Array.Empty<string>(), 1);

            Assert.That(left == right, Is.False);
        }

        /// <summary>
        ///     異なる型とは等しくない。
        /// </summary>
        [Test]
        public void Equals_OtherType_IsNotEqual()
        {
            SuiteInfo info = new("Town", SuiteLoadStateEnum.Complete, 1f, Array.Empty<string>(), 1);

            Assert.That(info.Equals("Town"), Is.False);
        }
    }
}
