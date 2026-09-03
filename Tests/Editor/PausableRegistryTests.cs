using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.System;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     PausableRegistryの契約を検証する。
    ///     Unity APIに依存しないため、EditModeで直接検証できる。
    /// </summary>
    public sealed class PausableRegistryTests
    {
        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IGameplayCategory : PauseManager.IPausable { }

        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IUiCategory : PauseManager.IPausable { }

        /// <summary> 検証用のポーズ対象。 </summary>
        private sealed class TestPausable : PauseManager.IPausable
        {
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }

            public void Pause() => PauseCount++;
            public void Resume() => ResumeCount++;
        }

        /// <summary> 生成直後は登録が無い。 </summary>
        [Test]
        public void InitialState_IsEmpty()
        {
            PausableRegistry registry = new();

            Assert.That(registry.Count, Is.Zero);
        }

        /// <summary> 初回登録は成功し、件数が増える。 </summary>
        [Test]
        public void TryRegister_FirstTime_Succeeds()
        {
            PausableRegistry registry = new();

            bool registered = registry.TryRegister(new TestPausable(), Gameplay, 0);

            Assert.That(registered, Is.True);
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        /// <summary> 同じ対象の重複登録は拒否され、件数が増えない。 </summary>
        [Test]
        public void TryRegister_Duplicate_IsRejected()
        {
            PausableRegistry registry = new();
            TestPausable pausable = new();
            registry.TryRegister(pausable, Gameplay, 0);

            bool registered = registry.TryRegister(pausable, Gameplay, 0);

            Assert.That(registered, Is.False);
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        /// <summary> 登録した対象のカテゴリーを取り出せる。 </summary>
        [Test]
        public void GetCategories_Registered_ReturnsCategories()
        {
            PausableRegistry registry = new();
            TestPausable pausable = new();
            registry.TryRegister(pausable, GameplayAndUi, 0);

            Assert.That(
                registry.GetCategories(pausable),
                Is.EquivalentTo(new[] { typeof(IGameplayCategory), typeof(IUiCategory) }));
        }

        /// <summary> 未登録の対象のカテゴリーは空になる。 </summary>
        [Test]
        public void GetCategories_NotRegistered_IsEmpty()
        {
            PausableRegistry registry = new();

            Assert.That(registry.GetCategories(new TestPausable()), Is.Empty);
        }

        /// <summary> カテゴリーに属する対象の件数を数えられる。 </summary>
        [Test]
        public void CountIn_CountsOnlyMembers()
        {
            PausableRegistry registry = new();
            registry.TryRegister(new TestPausable(), Gameplay, 0);
            registry.TryRegister(new TestPausable(), GameplayAndUi, 0);

            Assert.That(registry.CountIn(typeof(IGameplayCategory)), Is.EqualTo(2));
            Assert.That(registry.CountIn(typeof(IUiCategory)), Is.EqualTo(1));
        }

        /// <summary> 解除で登録が消える。 </summary>
        [Test]
        public void TryUnregister_Registered_Removes()
        {
            PausableRegistry registry = new();
            TestPausable pausable = new();
            registry.TryRegister(pausable, Gameplay, 0);

            bool unregistered = registry.TryUnregister(pausable);

            Assert.That(unregistered, Is.True);
            Assert.That(registry.Count, Is.Zero);
        }

        /// <summary> 未登録の解除は失敗する。 </summary>
        [Test]
        public void TryUnregister_NotRegistered_ReturnsFalse()
        {
            PausableRegistry registry = new();

            Assert.That(registry.TryUnregister(new TestPausable()), Is.False);
        }

        /// <summary> 属さないカテゴリーの状態変化では通知対象にならない。 </summary>
        [Test]
        public void ApplyCategoryState_OtherCategory_IsNotAffected()
        {
            PausableRegistry registry = new();
            TestPausable pausable = new();
            registry.TryRegister(pausable, Gameplay, 0);

            Assert.That(registry.ApplyCategoryState(typeof(IUiCategory), true), Is.Empty);
        }

        /// <summary> 最初の停止要因で停止へ切り替わる。 </summary>
        [Test]
        public void ApplyCategoryState_FirstPause_ReportsTransition()
        {
            PausableRegistry registry = new();
            TestPausable pausable = new();
            registry.TryRegister(pausable, GameplayAndUi, 0);

            IReadOnlyList<PauseManager.IPausable> affected =
                registry.ApplyCategoryState(typeof(IGameplayCategory), true);

            Assert.That(affected, Is.EqualTo(new[] { pausable }));
        }

        /// <summary>
        ///     2つ目の停止要因では切り替わらない。既に停止しているため。
        /// </summary>
        [Test]
        public void ApplyCategoryState_SecondPause_ReportsNoTransition()
        {
            PausableRegistry registry = new();
            TestPausable pausable = new();
            registry.TryRegister(pausable, GameplayAndUi, 0);
            registry.ApplyCategoryState(typeof(IGameplayCategory), true);

            IReadOnlyList<PauseManager.IPausable> affected =
                registry.ApplyCategoryState(typeof(IUiCategory), true);

            Assert.That(affected, Is.Empty);
        }

        /// <summary>
        ///     **2つの停止要因のうち片方だけ解除しても再開しない。**
        ///     件数で持たずに真偽値1つで持つと、ここで誤って再開してしまう。
        /// </summary>
        [Test]
        public void ApplyCategoryState_TwoCategories_ResumesAfterBothCleared()
        {
            PausableRegistry registry = new();
            TestPausable pausable = new();
            registry.TryRegister(pausable, GameplayAndUi, 0);
            registry.ApplyCategoryState(typeof(IGameplayCategory), true);
            registry.ApplyCategoryState(typeof(IUiCategory), true);

            IReadOnlyList<PauseManager.IPausable> afterFirstResume =
                registry.ApplyCategoryState(typeof(IGameplayCategory), false);
            IReadOnlyList<PauseManager.IPausable> afterSecondResume =
                registry.ApplyCategoryState(typeof(IUiCategory), false);

            Assert.That(afterFirstResume, Is.Empty, "まだ停止要因が残っている間は再開しない。");
            Assert.That(afterSecondResume, Is.EqualTo(new[] { pausable }));
        }

        /// <summary>
        ///     登録時点の停止要因数を引き継ぐ。
        /// </summary>
        /// <remarks>
        ///     ポーズ中に登録した対象は、解除されたときに再開の通知を受け取る。
        /// </remarks>
        [Test]
        public void TryRegister_WhilePaused_ResumesOnRelease()
        {
            PausableRegistry registry = new();
            TestPausable pausable = new();
            registry.TryRegister(pausable, Gameplay, 1);

            IReadOnlyList<PauseManager.IPausable> affected =
                registry.ApplyCategoryState(typeof(IGameplayCategory), false);

            Assert.That(affected, Is.EqualTo(new[] { pausable }));
        }

        /// <summary> 停止していない対象への解除は切り替わりにならない。 </summary>
        [Test]
        public void ApplyCategoryState_ResumeWithoutPause_ReportsNoTransition()
        {
            PausableRegistry registry = new();
            registry.TryRegister(new TestPausable(), Gameplay, 0);

            Assert.That(registry.ApplyCategoryState(typeof(IGameplayCategory), false), Is.Empty);
        }

        /// <summary> 全消去で件数が0になる。 </summary>
        [Test]
        public void Clear_RemovesAllRegistrations()
        {
            PausableRegistry registry = new();
            registry.TryRegister(new TestPausable(), Gameplay, 0);
            registry.TryRegister(new TestPausable(), Gameplay, 0);

            registry.Clear();

            Assert.That(registry.Count, Is.Zero);
        }

        /// <summary> nullを渡した操作は拒否される。 </summary>
        [Test]
        public void NullArguments_Throw()
        {
            PausableRegistry registry = new();

            Assert.Throws<ArgumentNullException>(() => registry.TryRegister(null, Gameplay, 0));
            Assert.Throws<ArgumentNullException>(
                () => registry.TryRegister(new TestPausable(), null, 0));
            Assert.Throws<ArgumentNullException>(() => registry.TryUnregister(null));
            Assert.Throws<ArgumentNullException>(() => registry.ApplyCategoryState(null, true));
            Assert.Throws<ArgumentNullException>(() => registry.GetCategories(null));
            Assert.Throws<ArgumentNullException>(() => registry.CountIn(null));
        }

        /// <summary> 空のカテゴリー一覧での登録は拒否される。 </summary>
        [Test]
        public void TryRegister_NoCategory_Throws()
        {
            PausableRegistry registry = new();

            Assert.Throws<ArgumentException>(
                () => registry.TryRegister(new TestPausable(), Array.Empty<Type>(), 0));
        }

        /// <summary> カテゴリー数を超える停止要因数は拒否される。 </summary>
        [Test]
        public void TryRegister_PausedCountOutOfRange_Throws()
        {
            PausableRegistry registry = new();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => registry.TryRegister(new TestPausable(), Gameplay, -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => registry.TryRegister(new TestPausable(), Gameplay, 2));
        }

        /// <summary> 単一カテゴリーの検証用一覧。 </summary>
        private static readonly Type[] Gameplay = { typeof(IGameplayCategory) };

        /// <summary> 2カテゴリーの検証用一覧。 </summary>
        private static readonly Type[] GameplayAndUi =
        {
            typeof(IGameplayCategory), typeof(IUiCategory),
        };
    }
}
