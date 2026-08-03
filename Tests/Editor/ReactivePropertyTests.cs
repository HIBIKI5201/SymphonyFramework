using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFrameWork.Core;

using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     ReactivePropertyが満たすべき契約を検証する。
    ///     DesignPhilosophyの「### ReactiveProperty」に列挙された条件へ対応する。
    /// </summary>
    public sealed class ReactivePropertyTests
    {
        /// <summary> 購読者例外の記録でテストが失敗しないよう、期待するログ検査を戻す。 </summary>
        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
        }

        #region 値と比較

        /// <summary> 初期値がValueへ反映される。 </summary>
        [Test]
        public void Constructor_InitialValue_IsExposed()
        {
            var property = new ReactiveProperty<int>(42);

            Assert.That(property.Value, Is.EqualTo(42));
        }

        /// <summary> 値が変化した場合はtrueを返し、Valueが更新される。 </summary>
        [Test]
        public void SetValue_Changed_ReturnsTrueAndUpdatesValue()
        {
            var property = new ReactiveProperty<int>(1);

            bool changed = property.SetValue(2);

            Assert.That(changed, Is.True);
            Assert.That(property.Value, Is.EqualTo(2));
        }

        /// <summary> 同値の場合はfalseを返し、通知しない。 </summary>
        [Test]
        public void SetValue_SameValue_ReturnsFalseAndDoesNotNotify()
        {
            var property = new ReactiveProperty<int>(1);
            int notifiedCount = 0;
            property.Subscribe(_ => notifiedCount++, notifyCurrent: false);

            bool changed = property.SetValue(1);

            Assert.That(changed, Is.False);
            Assert.That(notifiedCount, Is.Zero);
        }

        /// <summary> comparerを指定した場合、既定の比較ではなくそれが使われる。 </summary>
        [Test]
        public void SetValue_WithCustomComparer_UsesComparer()
        {
            // 大文字小文字を区別しない比較器を渡すと、"a" と "A" は同値になる。
            var property = new ReactiveProperty<string>("a", StringComparer.OrdinalIgnoreCase);

            bool changed = property.SetValue("A");

            Assert.That(changed, Is.False);
            Assert.That(property.Value, Is.EqualTo("a"));
        }

        /// <summary> comparer未指定時は既定の比較器が使われる。 </summary>
        [Test]
        public void SetValue_WithoutComparer_UsesDefaultComparer()
        {
            var property = new ReactiveProperty<string>("a");

            bool changed = property.SetValue("A");

            Assert.That(changed, Is.True);
            Assert.That(property.Value, Is.EqualTo("A"));
        }

        /// <summary> 配列は既定では参照比較になり、内容が同じでも変更として扱われる。 </summary>
        [Test]
        public void SetValue_ArrayWithDefaultComparer_TreatsEqualContentAsChanged()
        {
            var property = new ReactiveProperty<int[]>(new[] { 1, 2 });

            bool changed = property.SetValue(new[] { 1, 2 });

            Assert.That(changed, Is.True, "既定では参照比較になることを明示する。");
        }

        #endregion

        #region 購読と通知

        /// <summary> notifyCurrentがtrueの場合、購読時に現在値が通知される。 </summary>
        [Test]
        public void Subscribe_NotifyCurrentTrue_NotifiesImmediately()
        {
            var property = new ReactiveProperty<int>(7);
            var received = new List<int>();

            property.Subscribe(received.Add, notifyCurrent: true);

            Assert.That(received, Is.EqualTo(new[] { 7 }));
        }

        /// <summary> notifyCurrentがfalseの場合、購読時には通知されない。 </summary>
        [Test]
        public void Subscribe_NotifyCurrentFalse_DoesNotNotifyImmediately()
        {
            var property = new ReactiveProperty<int>(7);
            var received = new List<int>();

            property.Subscribe(received.Add, notifyCurrent: false);

            Assert.That(received, Is.Empty);
        }

        /// <summary> 値の変更が全購読者へ通知される。 </summary>
        [Test]
        public void SetValue_NotifiesAllSubscribers()
        {
            var property = new ReactiveProperty<int>(0);
            var first = new List<int>();
            var second = new List<int>();
            property.Subscribe(first.Add, notifyCurrent: false);
            property.Subscribe(second.Add, notifyCurrent: false);

            property.SetValue(5);

            Assert.That(first, Is.EqualTo(new[] { 5 }));
            Assert.That(second, Is.EqualTo(new[] { 5 }));
        }

        /// <summary> 購読を解除すると以後通知されない。 </summary>
        [Test]
        public void Subscription_Dispose_StopsNotification()
        {
            var property = new ReactiveProperty<int>(0);
            var received = new List<int>();
            IDisposable subscription = property.Subscribe(received.Add, notifyCurrent: false);

            subscription.Dispose();
            property.SetValue(1);

            Assert.That(received, Is.Empty);
        }

        /// <summary> 同じ購読解除ハンドルを複数回Disposeしても無害である。 </summary>
        [Test]
        public void Subscription_DisposeTwice_IsHarmless()
        {
            var property = new ReactiveProperty<int>(0);
            var remaining = new List<int>();
            IDisposable target = property.Subscribe(_ => { }, notifyCurrent: false);
            property.Subscribe(remaining.Add, notifyCurrent: false);

            target.Dispose();
            Assert.DoesNotThrow(() => target.Dispose());

            property.SetValue(1);

            Assert.That(remaining, Is.EqualTo(new[] { 1 }), "無関係な購読を巻き込まない。");
        }

        /// <summary> Subscribeへnullを渡すと例外になる。 </summary>
        [Test]
        public void Subscribe_NullObserver_Throws()
        {
            var property = new ReactiveProperty<int>(0);

            Assert.Throws<ArgumentNullException>(() => property.Subscribe(null));
        }

        #endregion

        #region 通知中の購読変更

        /// <summary> 通知中に購読を追加してもコレクション変更例外にならない。 </summary>
        [Test]
        public void SetValue_SubscribeDuringNotification_DoesNotThrow()
        {
            var property = new ReactiveProperty<int>(0);
            property.Subscribe(
                _ => property.Subscribe(__ => { }, notifyCurrent: false),
                notifyCurrent: false);

            Assert.DoesNotThrow(() => property.SetValue(1));
        }

        /// <summary> 通知中に購読を解除してもコレクション変更例外にならない。 </summary>
        [Test]
        public void SetValue_UnsubscribeDuringNotification_DoesNotThrow()
        {
            var property = new ReactiveProperty<int>(0);
            IDisposable subscription = null;
            subscription = property.Subscribe(_ => subscription.Dispose(), notifyCurrent: false);

            Assert.DoesNotThrow(() => property.SetValue(1));
        }

        #endregion

        #region 購読者の例外

        /// <summary> 1人の購読者が例外を投げても、残りの購読者へ通知が続く。 </summary>
        [Test]
        public void SetValue_ObserverThrows_ContinuesNotifyingOthers()
        {
            LogAssert.ignoreFailingMessages = true;

            var property = new ReactiveProperty<int>(0);
            var received = new List<int>();
            property.Subscribe(_ => throw new InvalidOperationException("観測用"), notifyCurrent: false);
            property.Subscribe(received.Add, notifyCurrent: false);

            Assert.DoesNotThrow(() => property.SetValue(3));
            Assert.That(received, Is.EqualTo(new[] { 3 }));
        }

        #endregion

        #region 破棄

        /// <summary> 破棄後のSetValueは例外になる。 </summary>
        [Test]
        public void SetValue_AfterDispose_Throws()
        {
            var property = new ReactiveProperty<int>(0);
            property.Dispose();

            Assert.Throws<ObjectDisposedException>(() => property.SetValue(1));
        }

        /// <summary> 破棄後のSubscribeは例外になる。 </summary>
        [Test]
        public void Subscribe_AfterDispose_Throws()
        {
            var property = new ReactiveProperty<int>(0);
            property.Dispose();

            Assert.Throws<ObjectDisposedException>(() => property.Subscribe(_ => { }));
        }

        /// <summary> 破棄すると既存の購読も解除される。 </summary>
        [Test]
        public void Dispose_ReleasesExistingSubscriptions()
        {
            var property = new ReactiveProperty<int>(0);
            var received = new List<int>();
            IDisposable subscription = property.Subscribe(received.Add, notifyCurrent: false);

            property.Dispose();

            Assert.DoesNotThrow(() => subscription.Dispose(), "解除ハンドルの後始末も安全である。");
            Assert.That(received, Is.Empty);
        }

        /// <summary> 破棄を繰り返しても例外にならない。 </summary>
        [Test]
        public void Dispose_Twice_IsHarmless()
        {
            var property = new ReactiveProperty<int>(0);

            property.Dispose();

            Assert.DoesNotThrow(() => property.Dispose());
        }

        #endregion

        #region メインスレッド制約

        /// <summary> メインスレッド以外からのSetValueは拒否される。 </summary>
        [Test]
        public void SetValue_FromBackgroundThread_Throws()
        {
            var property = new ReactiveProperty<int>(0);

            Exception captured = CaptureFromBackgroundThread(() => property.SetValue(1));

            Assert.That(captured, Is.TypeOf<InvalidOperationException>());
        }

        /// <summary> メインスレッド以外からのSubscribeは拒否される。 </summary>
        [Test]
        public void Subscribe_FromBackgroundThread_Throws()
        {
            var property = new ReactiveProperty<int>(0);

            Exception captured = CaptureFromBackgroundThread(() => property.Subscribe(_ => { }));

            Assert.That(captured, Is.TypeOf<InvalidOperationException>());
        }

        /// <summary> バックグラウンドスレッドで処理を実行し、送出された例外を返す。 </summary>
        /// <param name="action"> 実行する処理。 </param>
        /// <returns> 送出された例外。送出されなかった場合はnull。 </returns>
        private static Exception CaptureFromBackgroundThread(Action action)
        {
            Exception captured = null;

            Task task = Task.Run(() =>
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception exception)
                {
                    captured = exception;
                }
            });

            Assert.That(task.Wait(TimeSpan.FromSeconds(5)), Is.True, "バックグラウンド処理が完了しない。");
            return captured;
        }

        #endregion
    }
}
