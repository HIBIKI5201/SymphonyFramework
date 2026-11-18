using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFramework.Scenes;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     ブロックの追跡と、シーンごとの保持元の記録を検証する。
    /// </summary>
    public sealed class SuiteRegistryTests
    {
        /// <summary>
        ///     登録したブロックを名前で取得できる。
        /// </summary>
        [Test]
        public void Register_ThenTryGet_ReturnsEntity()
        {
            SuiteRegistry registry = new();
            registry.Register(CreateEntity("Town"));

            Assert.That(registry.TryGet("Town", out SuiteLoadEntity entity), Is.True);
            Assert.That(entity.SuiteName, Is.EqualTo("Town"));
        }

        /// <summary>
        ///     追跡していないブロックは取得できない。
        /// </summary>
        [Test]
        public void TryGet_UnknownBlock_ReturnsFalse()
        {
            SuiteRegistry registry = new();

            Assert.That(registry.TryGet("Town", out _), Is.False);
        }

        /// <summary>
        ///     保持元が残っている間は解放しても空にならない。
        /// </summary>
        [Test]
        public void ReleaseHolder_OtherHolderRemains_ReturnsFalse()
        {
            SuiteRegistry registry = new();
            registry.AddHolder("Shared", "Town");
            registry.AddHolder("Shared", "Dungeon");

            Assert.That(registry.ReleaseHolder("Shared", "Town"), Is.False);
            Assert.That(registry.IsHeldByAnyBlock("Shared"), Is.True);
        }

        /// <summary>
        ///     最後の保持元を解くと保持が空になる。
        /// </summary>
        [Test]
        public void ReleaseHolder_LastHolder_ReturnsTrue()
        {
            SuiteRegistry registry = new();
            registry.AddHolder("Base", "Town");

            Assert.That(registry.ReleaseHolder("Base", "Town"), Is.True);
            Assert.That(registry.IsHeldByAnyBlock("Base"), Is.False);
        }

        /// <summary>
        ///     保持していないシーンの解放は空として扱う。
        /// </summary>
        [Test]
        public void ReleaseHolder_UnknownScene_ReturnsTrue()
        {
            SuiteRegistry registry = new();

            Assert.That(registry.ReleaseHolder("Base", "Town"), Is.True);
        }

        /// <summary>
        ///     ブロック外のロードとして記録したシーンを判別できる。
        /// </summary>
        [Test]
        public void MarkExternallyHeld_ThenIsExternallyHeld_ReturnsTrue()
        {
            SuiteRegistry registry = new();
            registry.MarkExternallyHeld("Shared");

            Assert.That(registry.IsExternallyHeld("Shared"), Is.True);
            Assert.That(registry.IsExternallyHeld("Base"), Is.False);
        }

        /// <summary>
        ///     ブロックが保持しているシーンをOrdinal順で返す。
        /// </summary>
        [Test]
        public void GetHeldSceneNames_ReturnsOrdinalOrderedScenes()
        {
            SuiteRegistry registry = new();
            registry.AddHolder("Props", "Town");
            registry.AddHolder("Base", "Town");
            registry.AddHolder("Cave", "Dungeon");

            Assert.That(registry.GetHeldSceneNames("Town"), Is.EqualTo(new[] { "Base", "Props" }));
            Assert.That(registry.GetHeldSceneNames("Dungeon"), Is.EqualTo(new[] { "Cave" }));
        }

        /// <summary>
        ///     Clearで追跡と保持をすべて捨てる。
        /// </summary>
        [Test]
        public void Clear_RemovesEntitiesAndHolds()
        {
            SuiteRegistry registry = new();
            registry.Register(CreateEntity("Town"));
            registry.AddHolder("Base", "Town");
            registry.MarkExternallyHeld("Shared");

            registry.Clear();

            Assert.That(registry.Entities, Is.Empty);
            Assert.That(registry.IsHeldByAnyBlock("Base"), Is.False);
            Assert.That(registry.IsExternallyHeld("Shared"), Is.False);
        }

        /// <summary>
        ///     検証用のブロックを作る。
        /// </summary>
        /// <param name="suiteName"> ブロック名。 </param>
        /// <returns> 1層1シーンのブロック。 </returns>
        private static SuiteLoadEntity CreateEntity(string suiteName)
        {
            return new SuiteLoadEntity(
                suiteName,
                1,
                new IReadOnlyList<string>[] { new[] { "Base" } },
                new Dictionary<string, int>(StringComparer.Ordinal),
                Array.Empty<string>());
        }
    }
}
