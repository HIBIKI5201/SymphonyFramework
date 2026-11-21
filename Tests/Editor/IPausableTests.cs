using NUnit.Framework;

using SymphonyFramework.Exceptions;
using SymphonyFramework.Pause;

namespace SymphonyFramework.Tests
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
        private interface IGameplayCategory : IPausable { }

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
            Fermata.ResetRuntimeState();
        }

        /// <summary> 次のテストへstatic状態を残さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            Fermata.ResetRuntimeState();
        }

        /// <summary> 未初期化での登録は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void Register_NotInitialized_Throws()
        {
            Assert.That(
                () => Fermata.Register(new CountingPausable()),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary> 登録した対象へポーズと解除が通知される。 </summary>
        [Test]
        public void Register_Registered_ReceivesPauseAndResume()
        {
            Fermata.Initialize();
            CountingPausable pausable = new();

            Fermata.Register(pausable);

            Fermata.SetPause<IGameplayCategory>(true);
            Assert.That(pausable.PauseCount, Is.EqualTo(1));
            Assert.That(pausable.ResumeCount, Is.EqualTo(0));

            Fermata.SetPause<IGameplayCategory>(false);
            Assert.That(pausable.PauseCount, Is.EqualTo(1));
            Assert.That(pausable.ResumeCount, Is.EqualTo(1));
        }

        /// <summary> 全体ポーズは、カテゴリーを問わず登録した対象へ通知される。 </summary>
        [Test]
        public void Register_SetPauseAll_NotifiesEveryTarget()
        {
            Fermata.Initialize();
            CountingPausable pausable = new();

            Fermata.Register(pausable);

            Fermata.SetPauseAll(true);

            Assert.That(pausable.PauseCount, Is.EqualTo(1));
        }
    }
}
