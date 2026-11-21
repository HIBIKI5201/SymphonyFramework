using System;

using NUnit.Framework;

using SymphonyFramework.Scenes;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     SuiteDtoがViewModelへ渡す表示値と、内容比較の契約を検証する。
    /// </summary>
    public sealed class SuiteDtoTests
    {
        /// <summary>
        ///     生成時の値をそのまま公開する。
        /// </summary>
        [Test]
        public void Constructor_ExposesValues()
        {
            SuiteDto dto = new(
                "Town",
                SuiteLoadStateEnum.Loading,
                0.5f,
                new[] { "Base" },
                2);

            Assert.That(dto.SuiteName, Is.EqualTo("Town"));
            Assert.That(dto.State, Is.EqualTo(SuiteLoadStateEnum.Loading));
            Assert.That(dto.Progress, Is.EqualTo(0.5f));
            Assert.That(dto.HeldSceneNames, Is.EqualTo(new[] { "Base" }));
            Assert.That(dto.LayerCount, Is.EqualTo(2));
        }

        /// <summary>
        ///     保持シーンが未指定でも空の一覧として扱う。
        /// </summary>
        [Test]
        public void HeldSceneNames_Null_IsEmpty()
        {
            SuiteDto dto = new("Town", SuiteLoadStateEnum.Loading, 0f, null, 1);

            Assert.That(dto.HeldSceneNames, Is.Empty);
        }

        /// <summary>
        ///     同じ内容なら等しい。保持シーンは参照ではなく内容で比較する。
        /// </summary>
        [Test]
        public void Equals_SameContent_AreEqual()
        {
            SuiteDto left = new("Town", SuiteLoadStateEnum.Complete, 1f, new[] { "Base" }, 1);
            SuiteDto right = new("Town", SuiteLoadStateEnum.Complete, 1f, new[] { "Base" }, 1);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        /// <summary>
        ///     保持シーンが違えば等しくない。
        /// </summary>
        [Test]
        public void Equals_DifferentHeldScenes_AreNotEqual()
        {
            SuiteDto left = new("Town", SuiteLoadStateEnum.Complete, 1f, new[] { "Base" }, 1);
            SuiteDto right = new("Town", SuiteLoadStateEnum.Complete, 1f, Array.Empty<string>(), 1);

            Assert.That(left.Equals(right), Is.False);
        }

        /// <summary>
        ///     進捗が違えば等しくない。
        /// </summary>
        [Test]
        public void Equals_DifferentProgress_AreNotEqual()
        {
            SuiteDto left = new("Town", SuiteLoadStateEnum.Loading, 0.5f, null, 1);
            SuiteDto right = new("Town", SuiteLoadStateEnum.Loading, 0.75f, null, 1);

            Assert.That(left.Equals(right), Is.False);
        }
    }
}
