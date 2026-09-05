using NUnit.Framework;

using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.System;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     IPausableの登録と通知の契約を検証する。
    /// </summary>
    /// <remarks>
    ///     staticな状態を持つため、テストごとに初期化と破棄を対で行う。
    ///     Domain Reloadが無効なプロジェクトのため、後始末を怠ると次のテストへ状態が漏れる。
    /// </remarks>
    public sealed class IPausableTests
    {
        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IGameplayCategory : PauseManager.IPausable { }

        /// <summary> 通知の回数を数える検証用のポーズ対象。 </summary>
        private sealed class CountingPausable : IGameplayCategory
        {
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }

            public void Pause() => PauseCount++;
            public void Resume() => ResumeCount++;
        }

        /// <summary> 前のテストのstatic状態を持ち越さない。 </summary>
        [SetUp]
        public void SetUp()
        {
            PauseManager.ResetRuntimeState();
        }

        /// <summary> 次のテストへstatic状態を残さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            PauseManager.ResetRuntimeState();
        }

        /// <summary> 未初期化での登録は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void RegisterPauseManager_NotInitialized_Throws()
        {
            Assert.That(
                () => PauseManager.IPausable.RegisterPauseManager(new CountingPausable()),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary> 登録した対象へポーズと解除が通知される。 </summary>
        [Test]
        public void RegisterPauseManager_Registered_ReceivesPauseAndResume()
        {
            PauseManager.Initialize();
            CountingPausable pausable = new();

            PauseManager.IPausable.RegisterPauseManager(pausable);

            PauseManager.SetPause<IGameplayCategory>(true);
            Assert.That(pausable.PauseCount, Is.EqualTo(1));
            Assert.That(pausable.ResumeCount, Is.EqualTo(0));

            PauseManager.SetPause<IGameplayCategory>(false);
            Assert.That(pausable.PauseCount, Is.EqualTo(1));
            Assert.That(pausable.ResumeCount, Is.EqualTo(1));
        }

        /// <summary> 全体ポーズは、カテゴリーを問わず登録した対象へ通知される。 </summary>
        [Test]
        public void RegisterPauseManager_SetPauseAll_NotifiesEveryTarget()
        {
            PauseManager.Initialize();
            CountingPausable pausable = new();

            PauseManager.IPausable.RegisterPauseManager(pausable);

            PauseManager.SetPauseAll(true);

            Assert.That(pausable.PauseCount, Is.EqualTo(1));
        }
    }
}
