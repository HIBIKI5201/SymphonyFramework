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
        /// <summary> 検証用のポーズ対象。 </summary>
        private sealed class TestPausable : PauseManager.IPausable
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
        public void SetPaused_Changed_NotifiesOnce()
        {
            var received = new List<bool>();
            _service.AddPauseChangedHandler(received.Add);

            _service.SetPaused(true);

            Assert.That(received, Is.EqualTo(new[] { true }));
            Assert.That(_service.IsPaused, Is.True);
        }

        /// <summary>
        ///     **同じ値の再設定では通知しない。**
        ///     従来はPause = trueを2回続けるとIPausable.Pause()が2回呼ばれていた。
        /// </summary>
        [Test]
        public void SetPaused_SameValue_DoesNotNotify()
        {
            var received = new List<bool>();
            _service.SetPaused(true);
            _service.AddPauseChangedHandler(received.Add);

            _service.SetPaused(true);

            Assert.That(received, Is.Empty, "同じ値の再設定で通知してはいけない。");
        }

        /// <summary> 登録した対象へ状態に応じたコールバックが届く。 </summary>
        [Test]
        public void Register_ReceivesPauseAndResume()
        {
            var pausable = new TestPausable();
            _service.Register(pausable);

            _service.SetPaused(true);
            _service.SetPaused(false);

            Assert.That(pausable.PauseCount, Is.EqualTo(1));
            Assert.That(pausable.ResumeCount, Is.EqualTo(1));
        }

        /// <summary> 重複登録しても通知は1回だけ届く。 </summary>
        [Test]
        public void Register_Duplicate_DoesNotDoubleNotify()
        {
            var pausable = new TestPausable();
            _service.Register(pausable);
            _service.Register(pausable);

            _service.SetPaused(true);

            Assert.That(pausable.PauseCount, Is.EqualTo(1));
            Assert.That(_service.PausableSubscriberCount, Is.EqualTo(1));
        }

        /// <summary> 解除後は通知が届かない。 </summary>
        [Test]
        public void Unregister_StopsNotification()
        {
            var pausable = new TestPausable();
            _service.Register(pausable);
            _service.Unregister(pausable);

            _service.SetPaused(true);

            Assert.That(pausable.PauseCount, Is.Zero);
            Assert.That(_service.PausableSubscriberCount, Is.Zero);
        }

        /// <summary> 未登録の解除は何もしない。 </summary>
        [Test]
        public void Unregister_NotRegistered_IsHarmless()
        {
            Assert.DoesNotThrow(() => _service.Unregister(new TestPausable()));
        }

        /// <summary> 登録と解除でも表示向けの状態変更が発行される。購読件数を表示するため。 </summary>
        [Test]
        public void RegisterAndUnregister_RaiseStateChanged()
        {
            int notifiedCount = 0;
            _service.OnStateChanged += () => notifiedCount++;

            var pausable = new TestPausable();
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

            Assert.DoesNotThrow(() => _service.SetPaused(true));
            Assert.That(_service.IsPaused, Is.True);
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

            Assert.Throws<InvalidOperationException>(() => _service.SetPaused(true));
        }

        /// <summary> Resetで状態と登録が消える。 </summary>
        [Test]
        public void Reset_ClearsStateAndRegistrations()
        {
            var pausable = new TestPausable();
            _service.Register(pausable);
            _service.SetPaused(true);
            int pauseCountBeforeReset = pausable.PauseCount;

            _service.Reset();

            Assert.That(_service.IsPaused, Is.False);
            Assert.That(_service.PausableSubscriberCount, Is.Zero);

            _service.SetPaused(true);
            Assert.That(
                pausable.PauseCount,
                Is.EqualTo(pauseCountBeforeReset),
                "Reset後は解除済みの対象へ通知しない。");
        }
    }
}
