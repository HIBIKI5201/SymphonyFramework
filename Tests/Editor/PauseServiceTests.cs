using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using NUnit.Framework;

using SymphonyFrameWork.System;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     PauseServiceの契約を検証する。
    ///     Unityのライフサイクルに依存しないため、EditModeで直接検証できる。
    /// </summary>
    public sealed class PauseServiceTests
    {
        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IGameplayCategory : PauseManager.IPausable { }

        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IUiCategory : PauseManager.IPausable { }

        /// <summary> カテゴリーを実装しない検証用のポーズ対象。 </summary>
        private sealed class TestPausable : PauseManager.IPausable
        {
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }

            public void Pause() => PauseCount++;
            public void Resume() => ResumeCount++;
        }

        /// <summary> ゲームプレイカテゴリーの検証用ポーズ対象。 </summary>
        private sealed class GameplayPausable : IGameplayCategory
        {
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }

            public void Pause() => PauseCount++;
            public void Resume() => ResumeCount++;
        }

        /// <summary> UIカテゴリーの検証用ポーズ対象。 </summary>
        private sealed class UiPausable : IUiCategory
        {
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }

            public void Pause() => PauseCount++;
            public void Resume() => ResumeCount++;
        }

        /// <summary> 2カテゴリーに属する検証用ポーズ対象。 </summary>
        private sealed class GameplayAndUiPausable : IGameplayCategory, IUiCategory
        {
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }

            public void Pause() => PauseCount++;
            public void Resume() => ResumeCount++;
        }

        private PauseService _service;

        /// <summary> 各テストごとに独立したServiceを構築する。 </summary>
        [SetUp]
        public void SetUp()
        {
            _service = new PauseService(new PauseStateEntity(), new PausableRegistry());
        }

        /// <summary> 引数を省略した生成は拒否される。 </summary>
        [Test]
        public void Constructor_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(
                () => new PauseService(null, new PausableRegistry()));
            Assert.Throws<ArgumentNullException>(
                () => new PauseService(new PauseStateEntity(), null));
        }

        /// <summary> ポーズ状態の変更が新しい値とともに1回通知される。 </summary>
        [Test]
        public void SetPausedAll_Changed_NotifiesOnce()
        {
            List<bool> received = new();
            _service.AddPauseChangedHandler(received.Add);

            _service.SetPausedAll(true);

            Assert.That(received, Is.EqualTo(new[] { true }));
            Assert.That(_service.IsPausedAny, Is.True);
        }

        /// <summary>
        ///     **同じ値の再設定では通知しない。**
        ///     従来はPause = trueを2回続けるとIPausable.Pause()が2回呼ばれていた。
        /// </summary>
        [Test]
        public void SetPausedAll_SameValue_DoesNotNotify()
        {
            List<bool> received = new();
            _service.SetPausedAll(true);
            _service.AddPauseChangedHandler(received.Add);

            _service.SetPausedAll(true);

            Assert.That(received, Is.Empty, "同じ値の再設定で通知してはいけない。");
        }

        /// <summary> 登録した対象へ状態に応じたコールバックが届く。 </summary>
        [Test]
        public void Register_ReceivesPauseAndResume()
        {
            TestPausable pausable = new();
            _service.Register(pausable);

            _service.SetPausedAll(true);
            _service.SetPausedAll(false);

            Assert.That(pausable.PauseCount, Is.EqualTo(1));
            Assert.That(pausable.ResumeCount, Is.EqualTo(1));
        }

        /// <summary> 重複登録しても通知は1回だけ届く。 </summary>
        [Test]
        public void Register_Duplicate_DoesNotDoubleNotify()
        {
            TestPausable pausable = new();
            _service.Register(pausable);
            _service.Register(pausable);

            _service.SetPausedAll(true);

            Assert.That(pausable.PauseCount, Is.EqualTo(1));
            Assert.That(_service.PausableSubscriberCount, Is.EqualTo(1));
        }

        /// <summary> 解除後は通知が届かない。 </summary>
        [Test]
        public void Unregister_StopsNotification()
        {
            TestPausable pausable = new();
            _service.Register(pausable);
            _service.Unregister(pausable);

            _service.SetPausedAll(true);

            Assert.That(pausable.PauseCount, Is.Zero);
            Assert.That(_service.PausableSubscriberCount, Is.Zero);
        }

        /// <summary> 未登録の解除は何もしない。 </summary>
        [Test]
        public void Unregister_NotRegistered_IsHarmless()
        {
            Assert.DoesNotThrow(() => _service.Unregister(new TestPausable()));
        }

        /// <summary>
        ///     **カテゴリーを止めても、他のカテゴリーの対象は止まらない。**
        ///     これがこの変更の目的である。
        /// </summary>
        [Test]
        public void SetPaused_OtherCategory_DoesNotNotify()
        {
            GameplayPausable gameplay = new();
            UiPausable ui = new();
            _service.Register(gameplay);
            _service.Register(ui);

            _service.SetPaused(typeof(IGameplayCategory), true);

            Assert.That(gameplay.PauseCount, Is.EqualTo(1));
            Assert.That(ui.PauseCount, Is.Zero, "他のカテゴリーの対象を止めてはいけない。");
        }

        /// <summary>
        ///     カテゴリーを止めても、カテゴリー未指定の対象は止まらない。
        /// </summary>
        [Test]
        public void SetPaused_Category_DoesNotAffectDefaultCategory()
        {
            TestPausable uncategorized = new();
            _service.Register(uncategorized);

            _service.SetPaused(typeof(IGameplayCategory), true);

            Assert.That(uncategorized.PauseCount, Is.Zero);
        }

        /// <summary>
        ///     一括操作は、明示カテゴリーの対象にも既定カテゴリーの対象にも届く。
        /// </summary>
        /// <remarks>
        ///     状態側だけを見ると、まだ誰も止めていないカテゴリーの対象を取りこぼす。
        /// </remarks>
        [Test]
        public void SetPausedAll_NotifiesEveryCategory()
        {
            GameplayPausable gameplay = new();
            UiPausable ui = new();
            TestPausable uncategorized = new();
            _service.Register(gameplay);
            _service.Register(ui);
            _service.Register(uncategorized);

            _service.SetPausedAll(true);

            Assert.That(gameplay.PauseCount, Is.EqualTo(1));
            Assert.That(ui.PauseCount, Is.EqualTo(1));
            Assert.That(uncategorized.PauseCount, Is.EqualTo(1));
        }

        /// <summary>
        ///     **2カテゴリーに属する対象は、片方を解除しても再開しない。**
        ///     停止要因を件数で持たないと、ここで誤って再開する。
        /// </summary>
        [Test]
        public void SetPaused_TwoCategories_ResumesAfterBothCleared()
        {
            GameplayAndUiPausable pausable = new();
            _service.Register(pausable);
            _service.SetPaused(typeof(IGameplayCategory), true);
            _service.SetPaused(typeof(IUiCategory), true);
            int pauseCountAfterSetup = pausable.PauseCount;

            _service.SetPaused(typeof(IGameplayCategory), false);
            int resumeCountAfterFirstRelease = pausable.ResumeCount;
            _service.SetPaused(typeof(IUiCategory), false);

            Assert.That(pauseCountAfterSetup, Is.EqualTo(1), "停止の通知は1回だけ。");
            Assert.That(resumeCountAfterFirstRelease, Is.Zero, "片方が残る間は再開しない。");
            Assert.That(pausable.ResumeCount, Is.EqualTo(1));
        }

        /// <summary>
        ///     ポーズ中に登録した対象は、さかのぼって停止しない。
        /// </summary>
        /// <remarks>
        ///     解除されたときには再開の通知が届く。従来と同じ挙動である。
        /// </remarks>
        [Test]
        public void Register_WhilePaused_DoesNotPauseRetroactively()
        {
            _service.SetPaused(typeof(IGameplayCategory), true);

            GameplayPausable pausable = new();
            _service.Register(pausable);
            int pauseCountAfterRegister = pausable.PauseCount;

            _service.SetPaused(typeof(IGameplayCategory), false);

            Assert.That(pauseCountAfterRegister, Is.Zero, "登録時点では停止を通知しない。");
            Assert.That(pausable.ResumeCount, Is.EqualTo(1));
        }

        /// <summary>
        ///     公開eventは「ポーズ中か否か」の変化でだけ発行する。
        /// </summary>
        /// <remarks>
        ///     カテゴリー単位で毎回発行すると、従来の購読者へ同じ値が連続して届く。
        /// </remarks>
        [Test]
        public void SetPaused_SecondCategory_DoesNotRaiseOnPauseChangedAgain()
        {
            List<bool> received = new();
            _service.SetPaused(typeof(IGameplayCategory), true);
            _service.AddPauseChangedHandler(received.Add);

            _service.SetPaused(typeof(IUiCategory), true);

            Assert.That(received, Is.Empty, "既にポーズ中なら全体の状態は変わらない。");
        }

        /// <summary> 全カテゴリーが解除されたときに公開eventが解除を通知する。 </summary>
        [Test]
        public void SetPaused_LastCategoryReleased_RaisesOnPauseChanged()
        {
            _service.SetPaused(typeof(IGameplayCategory), true);
            _service.SetPaused(typeof(IUiCategory), true);
            List<bool> received = new();
            _service.AddPauseChangedHandler(received.Add);

            _service.SetPaused(typeof(IGameplayCategory), false);
            _service.SetPaused(typeof(IUiCategory), false);

            Assert.That(received, Is.EqualTo(new[] { false }));
        }

        /// <summary> カテゴリー単位のポーズ状態を読み取れる。 </summary>
        [Test]
        public void IsPaused_ReflectsCategoryState()
        {
            _service.SetPaused(typeof(IGameplayCategory), true);

            Assert.That(_service.IsPaused(typeof(IGameplayCategory)), Is.True);
            Assert.That(_service.IsPaused(typeof(IUiCategory)), Is.False);
            Assert.That(_service.IsPausedAny, Is.True);
        }

        /// <summary> 登録と解除でも表示向けの状態変更が発行される。購読件数を表示するため。 </summary>
        [Test]
        public void RegisterAndUnregister_RaiseStateChanged()
        {
            int notifiedCount = 0;
            _service.OnStateChanged += () => notifiedCount++;

            TestPausable pausable = new();
            _service.Register(pausable);
            Assert.That(notifiedCount, Is.EqualTo(1));

            _service.Unregister(pausable);
            Assert.That(notifiedCount, Is.EqualTo(2));
        }

        /// <summary>
        ///     表示向けeventの購読者例外は発行側で止める。
        ///     ViewModelの失敗をゲーム側のポーズ処理の失敗にしない。
        /// </summary>
        [Test]
        public void OnStateChanged_SubscriberException_DoesNotFailSetPaused()
        {
            LogAssert.Expect(LogType.Exception, new Regex("view failure"));

            _service.OnStateChanged += () => throw new InvalidOperationException("view failure");

            Assert.DoesNotThrow(() => _service.SetPausedAll(true));
            Assert.That(_service.IsPausedAny, Is.True);
        }

        /// <summary>
        ///     **公開eventの購読者例外は握らない。**
        ///     OnPauseChangedは利用側のゲームロジックが購読するため、
        ///     例外を握り潰すと不具合が見えなくなる。
        /// </summary>
        [Test]
        public void OnPauseChanged_SubscriberException_Propagates()
        {
            _service.AddPauseChangedHandler(_ => throw new InvalidOperationException("game failure"));

            Assert.Throws<InvalidOperationException>(() => _service.SetPausedAll(true));
        }

        /// <summary> nullのカテゴリーを渡した操作は拒否される。 </summary>
        [Test]
        public void SetPaused_NullCategory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.SetPaused(null, true));
        }

        /// <summary> Resetで状態と登録が消える。 </summary>
        [Test]
        public void Reset_ClearsStateAndRegistrations()
        {
            GameplayPausable pausable = new();
            _service.Register(pausable);
            _service.SetPausedAll(true);
            int pauseCountBeforeReset = pausable.PauseCount;

            _service.Reset();

            Assert.That(_service.IsPausedAny, Is.False);
            Assert.That(_service.IsPaused(typeof(IGameplayCategory)), Is.False);
            Assert.That(_service.PausableSubscriberCount, Is.Zero);

            _service.SetPausedAll(true);
            Assert.That(
                pausable.PauseCount,
                Is.EqualTo(pauseCountBeforeReset),
                "Reset後は解除済みの対象へ通知しない。");
        }
    }
}
