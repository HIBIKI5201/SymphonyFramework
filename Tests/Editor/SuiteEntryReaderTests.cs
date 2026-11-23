using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFramework.Scenes;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     Authoringのエントリ一覧から依存順の実行層を作る変換と、異常の説明を検証する。
    /// </summary>
    public sealed class SuiteEntryReaderTests
    {
        /// <summary>
        ///     依存の無いエントリだけなら1つの層へまとまる。
        /// </summary>
        [Test]
        public void TryCreateLayers_NoDependencies_ProducesSingleLayer()
        {
            SuiteEntry[] entries =
            {
                new("Base"),
                new("Props"),
            };

            bool result = SuiteEntryReader.TryCreateLayers(
                entries,
                out IReadOnlyList<IReadOnlyList<string>> layers,
                out IReadOnlyList<string> descriptions);

            Assert.That(result, Is.True);
            Assert.That(descriptions, Is.Empty);
            Assert.That(layers, Has.Count.EqualTo(1));
            Assert.That(layers[0], Is.EqualTo(new[] { "Base", "Props" }));
        }

        /// <summary>
        ///     DependsOnで指定した先行シーンが前の層になる。
        /// </summary>
        [Test]
        public void TryCreateLayers_DependsOn_OrdersDependencyFirst()
        {
            SuiteEntry[] entries =
            {
                new("Props", new[] { "Base" }),
                new("Base"),
                new("Npc", new[] { "Base" }),
            };

            bool result = SuiteEntryReader.TryCreateLayers(
                entries,
                out IReadOnlyList<IReadOnlyList<string>> layers,
                out IReadOnlyList<string> descriptions);

            Assert.That(result, Is.True);
            Assert.That(descriptions, Is.Empty);
            Assert.That(layers, Has.Count.EqualTo(2));
            Assert.That(layers[0], Is.EqualTo(new[] { "Base" }));
            Assert.That(layers[1], Is.EqualTo(new[] { "Npc", "Props" }));
        }

        /// <summary>
        ///     依存の連鎖は層の数だけ段階を作る。
        /// </summary>
        [Test]
        public void TryCreateLayers_ChainedDependencies_ProducesLayerPerStep()
        {
            SuiteEntry[] entries =
            {
                new("Base"),
                new("Props", new[] { "Base" }),
                new("Ui", new[] { "Props" }),
            };

            bool result = SuiteEntryReader.TryCreateLayers(
                entries,
                out IReadOnlyList<IReadOnlyList<string>> layers,
                out _);

            Assert.That(result, Is.True);
            Assert.That(layers, Has.Count.EqualTo(3));
            Assert.That(layers[0], Is.EqualTo(new[] { "Base" }));
            Assert.That(layers[1], Is.EqualTo(new[] { "Props" }));
            Assert.That(layers[2], Is.EqualTo(new[] { "Ui" }));
        }

        /// <summary>
        ///     循環依存を検出して層を返さない。
        /// </summary>
        [Test]
        public void TryCreateLayers_CyclicDependency_FailsWithDescription()
        {
            SuiteEntry[] entries =
            {
                new("A", new[] { "B" }),
                new("B", new[] { "A" }),
            };

            bool result = SuiteEntryReader.TryCreateLayers(
                entries,
                out IReadOnlyList<IReadOnlyList<string>> layers,
                out IReadOnlyList<string> descriptions);

            Assert.That(result, Is.False);
            Assert.That(layers, Is.Empty);
            Assert.That(descriptions, Has.Count.EqualTo(1));
            Assert.That(descriptions[0], Does.Contain("循環依存"));
        }

        /// <summary>
        ///     エントリに無いシーンへの依存を検出する。
        /// </summary>
        [Test]
        public void TryCreateLayers_MissingReference_FailsWithDescription()
        {
            SuiteEntry[] entries = { new("Props", new[] { "NotDeclared" }) };

            bool result = SuiteEntryReader.TryCreateLayers(
                entries,
                out _,
                out IReadOnlyList<string> descriptions);

            Assert.That(result, Is.False);
            Assert.That(descriptions, Has.Count.EqualTo(1));
            Assert.That(descriptions[0], Does.Contain("NotDeclared"));
        }

        /// <summary>
        ///     複数の異常を1件目で打ち切らずすべて報告する。
        /// </summary>
        [Test]
        public void TryCreateLayers_MultipleErrors_ReportsAll()
        {
            SuiteEntry[] entries =
            {
                new("A", new[] { "A" }),
                new("B"),
                new("B"),
            };

            bool result = SuiteEntryReader.TryCreateLayers(
                entries,
                out _,
                out IReadOnlyList<string> descriptions);

            Assert.That(result, Is.False);
            Assert.That(descriptions, Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(descriptions, Has.Some.Contains("自分自身"));
            Assert.That(descriptions, Has.Some.Contains("複数のエントリ"));
        }

        /// <summary>
        ///     シーン名が空のエントリを位置つきで報告する。
        /// </summary>
        [Test]
        public void TryCreateLayers_BlankSceneName_ReportsEntryIndex()
        {
            SuiteEntry[] entries =
            {
                new("Base"),
                new("   "),
            };

            bool result = SuiteEntryReader.TryCreateLayers(
                entries,
                out _,
                out IReadOnlyList<string> descriptions);

            Assert.That(result, Is.False);
            Assert.That(descriptions, Has.Count.EqualTo(1));
            Assert.That(descriptions[0], Does.Contain("1番目"));
        }

        /// <summary>
        ///     依存に空の要素があるエントリを報告する。
        /// </summary>
        [Test]
        public void TryCreateLayers_BlankDependency_ReportsSceneName()
        {
            SuiteEntry[] entries =
            {
                new("Base"),
                new("Props", new[] { "Base", "" }),
            };

            bool result = SuiteEntryReader.TryCreateLayers(
                entries,
                out _,
                out IReadOnlyList<string> descriptions);

            Assert.That(result, Is.False);
            Assert.That(descriptions, Has.Count.EqualTo(1));
            Assert.That(descriptions[0], Does.Contain("Props"));
        }

        /// <summary>
        ///     エントリがnullでも例外にせず、位置つきで報告する。
        /// </summary>
        [Test]
        public void TryCreateLayers_NullEntry_ReportsEntryIndex()
        {
            SuiteEntry[] entries = { new("Base"), null };

            bool result = SuiteEntryReader.TryCreateLayers(
                entries,
                out _,
                out IReadOnlyList<string> descriptions);

            Assert.That(result, Is.False);
            Assert.That(descriptions, Has.Count.EqualTo(1));
            Assert.That(descriptions[0], Does.Contain("1番目"));
        }

        /// <summary>
        ///     一覧そのものの欠落は呼び出し契約の違反として例外にする。
        /// </summary>
        [Test]
        public void TryCreateLayers_NullEntries_Throws()
        {
            Assert.That(
                () => SuiteEntryReader.TryCreateLayers(null, out _, out _),
                Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        ///     空のブロックは呼び出し契約の違反として例外にする。
        /// </summary>
        [Test]
        public void TryCreateLayers_EmptyEntries_Throws()
        {
            Assert.That(
                () => SuiteEntryReader.TryCreateLayers(
                    Array.Empty<SuiteEntry>(),
                    out _,
                    out _),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        ///     すべての異常の種類に、対象シーン名を含む説明がある。
        /// </summary>
        /// <remarks>
        ///     種別が増えたときに説明の追加漏れで落ちるよう、enumの値を列挙して確認する。
        /// </remarks>
        [Test]
        public void Describe_EveryErrorKind_ContainsNodeIds()
        {
            foreach (SuitePlanErrorEnum kind in Enum.GetValues(typeof(SuitePlanErrorEnum)))
            {
                string description = SuiteEntryReader.Describe(
                    new SuitePlanError(kind, new[] { "TargetScene" }));

                Assert.That(description, Does.Contain("TargetScene"), $"{kind} の説明。");
                Assert.That(description, Does.Not.Contain("未知"), $"{kind} の説明。");
            }
        }
    }
}
