using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFramework.Scenes;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     Suite 1件の実行計画と状態遷移を検証する。
    /// </summary>
    public sealed class SuiteLoadEntityTests
    {
        /// <summary>
        ///     生成時の値と、層から求めたシーン総数を公開する。
        /// </summary>
        [Test]
        public void Constructor_ExposesValuesAndSceneCount()
        {
            SuiteLoadEntity entity = CreateEntity();

            Assert.That(entity.SuiteName, Is.EqualTo("Town"));
            Assert.That(entity.AssetInstanceId, Is.EqualTo(42));
            Assert.That(entity.Layers.Count, Is.EqualTo(2));
            Assert.That(entity.SceneCount, Is.EqualTo(3));
            Assert.That(entity.State, Is.EqualTo(SuiteLoadStateEnum.None));
        }

        /// <summary>
        ///     ロードの開始と完了で状態と進捗が進む。
        /// </summary>
        [Test]
        public void BeginLoading_ThenCompleteLoading_MovesStateAndProgress()
        {
            SuiteLoadEntity entity = CreateEntity();

            entity.BeginLoading();
            Assert.That(entity.State, Is.EqualTo(SuiteLoadStateEnum.Loading));
            Assert.That(entity.Progress, Is.Zero);

            entity.CompleteLoading();
            Assert.That(entity.State, Is.EqualTo(SuiteLoadStateEnum.Complete));
            Assert.That(entity.Progress, Is.EqualTo(1f));
        }

        /// <summary>
        ///     アンロードの開始で状態が変わる。
        /// </summary>
        [Test]
        public void BeginUnloading_MovesStateToUnloading()
        {
            SuiteLoadEntity entity = CreateEntity();
            entity.BeginLoading();
            entity.CompleteLoading();

            entity.BeginUnloading();

            Assert.That(entity.State, Is.EqualTo(SuiteLoadStateEnum.Unloading));
        }

        /// <summary>
        ///     範囲外の進捗を0から1へ丸める。
        /// </summary>
        [Test]
        public void ReportProgress_OutOfRange_IsClamped()
        {
            SuiteLoadEntity entity = CreateEntity();

            entity.ReportProgress(-1f);
            Assert.That(entity.Progress, Is.Zero);

            entity.ReportProgress(2f);
            Assert.That(entity.Progress, Is.EqualTo(1f));
        }

        /// <summary>
        ///     同じ進捗の再通知は変化なしとして扱う。
        /// </summary>
        [Test]
        public void ReportProgress_SameValue_ReturnsFalse()
        {
            SuiteLoadEntity entity = CreateEntity();

            Assert.That(entity.ReportProgress(0.5f), Is.True);
            Assert.That(entity.ReportProgress(0.5f), Is.False);
        }

        /// <summary>
        ///     永続指定のシーンを判別できる。
        /// </summary>
        [Test]
        public void IsPersistent_DeclaredScene_ReturnsTrue()
        {
            SuiteLoadEntity entity = CreateEntity();

            Assert.That(entity.IsPersistent("Base"), Is.True);
            Assert.That(entity.IsPersistent("Props"), Is.False);
        }

        /// <summary>
        ///     優先度は指定が無ければ0を返す。
        /// </summary>
        [Test]
        public void GetPriority_UnknownScene_ReturnsZero()
        {
            SuiteLoadEntity entity = CreateEntity();

            Assert.That(entity.GetPriority("Base"), Is.EqualTo(10));
            Assert.That(entity.GetPriority("Unknown"), Is.Zero);
        }

        /// <summary>
        ///     実行計画がnullの場合は生成できない。
        /// </summary>
        [Test]
        public void Constructor_NullLayers_Throws()
        {
            Assert.That(
                () => new SuiteLoadEntity(
                    "Town",
                    1,
                    null,
                    new Dictionary<string, int>(StringComparer.Ordinal),
                    Array.Empty<string>()),
                Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        ///     検証用のブロックを作る。
        /// </summary>
        /// <returns> 2層3シーン、Baseが永続で優先度10のブロック。 </returns>
        private static SuiteLoadEntity CreateEntity()
        {
            return new SuiteLoadEntity(
                "Town",
                42,
                new IReadOnlyList<string>[]
                {
                    new[] { "Base" },
                    new[] { "Npc", "Props" },
                },
                new Dictionary<string, int>(StringComparer.Ordinal) { { "Base", 10 } },
                new[] { "Base" });
        }
    }
}
