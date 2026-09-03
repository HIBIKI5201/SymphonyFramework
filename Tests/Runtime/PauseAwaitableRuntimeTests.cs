using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFrameWork.System;
using SymphonyFrameWork.Utility;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     PauseManagerの非同期APIがAwaitableへ移行してもタイミングとキャンセルの
    ///     契約を保つことを検証する。フレーム進行を伴うためPlayModeで実行する。
    ///     PauseManagerはSymphonyOrchestratorがPlay Mode開始時に初期化する。
    /// </summary>
    public sealed class PauseAwaitableRuntimeTests
    {
        /// <summary> 待機APIの検証用カテゴリー。 </summary>
        private interface IWaitCategory : PauseManager.IPausable { }

        /// <summary> 各テストの後でポーズ状態を戻し、次のテストへ持ち越さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            if (PauseManager.IsInitialized)
            {
                PauseManager.SetPauseAll(false);
            }
        }

        /// <summary> PausableNextFrameAsyncを待機するとフレームが進む。 </summary>
        [UnityTest]
        public IEnumerator PausableNextFrameAsync_AdvancesFrame()
        {
            int startFrame = Time.frameCount;
            Task task = SymphonyAwaitable.AsTask(PauseManager.PausableNextFrameAsync<IWaitCategory>());

            yield return new WaitUntil(() => task.IsCompleted);

            task.GetAwaiter().GetResult();
            Assert.That(Time.frameCount, Is.GreaterThan(startFrame));
        }

        /// <summary> PausableWaitForSecondAsyncは指定秒の経過後に完了する。 </summary>
        [UnityTest]
        public IEnumerator PausableWaitForSecondAsync_CompletesAfterDuration()
        {
            float startedAt = Time.realtimeSinceStartup;
            Task task = SymphonyAwaitable.AsTask(
                PauseManager.PausableWaitForSecondAsync<IWaitCategory>(0.1f));

            yield return new WaitUntil(() => task.IsCompleted);

            task.GetAwaiter().GetResult();
            Assert.That(
                Time.realtimeSinceStartup - startedAt,
                Is.GreaterThanOrEqualTo(0.05f));
        }

        /// <summary>
        ///     **ポーズ中は待機が進まない。**
        ///     Awaitableへの移行で最も壊れやすいのがこの時間の扱いであり、
        ///     戻り値型ではなくタイミングの契約を検証する。
        /// </summary>
        [UnityTest]
        public IEnumerator PausableWaitForSecondAsync_DoesNotAdvanceWhilePaused()
        {
            PauseManager.SetPause<IWaitCategory>(true);

            Task task = SymphonyAwaitable.AsTask(
                PauseManager.PausableWaitForSecondAsync<IWaitCategory>(0.1f));

            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }

            Assert.That(task.IsCompleted, Is.False, "ポーズ中に待機が完了してはいけない。");

            PauseManager.SetPause<IWaitCategory>(false);

            yield return new WaitUntil(() => task.IsCompleted);
            task.GetAwaiter().GetResult();
        }

        /// <summary>
        ///     キャンセル済みトークンではOperationCanceledExceptionになる。
        ///     **Awaitableを直接awaitして観測する。** AsTaskで包むとTask側の
        ///     TaskCanceledExceptionに化けてしまい、利用側が実際に受け取る型を測れない。
        /// </summary>
        [UnityTest]
        public IEnumerator PausableWaitForSecondAsync_Canceled_ThrowsOperationCanceled()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Exception captured = null;

            async Task AwaitDirectlyAsync()
            {
                try
                {
                    await PauseManager.PausableWaitForSecondAsync<IWaitCategory>(0.1f, cancellation.Token);
                }
                catch (Exception exception)
                {
                    captured = exception;
                }
            }

            Task task = AwaitDirectlyAsync();

            yield return new WaitUntil(() => task.IsCompleted);

            Assert.That(captured, Is.InstanceOf<OperationCanceledException>());
        }

        /// <summary> PausableInvokeを待機すると、指定秒後に処理が実行されている。 </summary>
        [UnityTest]
        public IEnumerator PausableInvoke_Awaited_RunsAction()
        {
            bool invoked = false;
            Task task = SymphonyAwaitable.AsTask(
                PauseManager.PausableInvoke<IWaitCategory>(() => invoked = true, 0.05f));

            yield return new WaitUntil(() => task.IsCompleted);

            task.GetAwaiter().GetResult();
            Assert.That(invoked, Is.True);
        }

        /// <summary>
        ///     **待機せずに呼んでも処理は実行される。**
        ///     fire-and-forgetとして使える契約を、Awaitableを返す形にしても保つ。
        /// </summary>
        [UnityTest]
        public IEnumerator PausableInvoke_NotAwaited_StillRunsAction()
        {
            bool invoked = false;

            _ = PauseManager.PausableInvoke<IWaitCategory>(() => invoked = true, 0.05f);

            float startedAt = Time.realtimeSinceStartup;
            yield return new WaitUntil(
                () => invoked || Time.realtimeSinceStartup - startedAt > 2f);

            Assert.That(invoked, Is.True);
        }

        /// <summary>
        ///     **引数の誤りは同期的に投げられる。**
        ///     async voidだった頃は非同期メソッド内で投げられ、呼び出し元の
        ///     try/catchでは捕まえられなかった。
        /// </summary>
        [Test]
        public void PausableInvoke_InvalidArguments_ThrowSynchronously()
        {
            Assert.Throws<ArgumentNullException>(
                () => PauseManager.PausableInvoke<IWaitCategory>(null, 0.05f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => PauseManager.PausableInvoke<IWaitCategory>(() => { }, -1f));
        }

        /// <summary> PausableDestroyは指定秒後にGameObjectを破棄する。 </summary>
        [UnityTest]
        public IEnumerator PausableDestroy_Awaited_DestroysObject()
        {
            var target = new GameObject(nameof(PausableDestroy_Awaited_DestroysObject));
            Task task = SymphonyAwaitable.AsTask(
                PauseManager.PausableDestroy<IWaitCategory>(target, 0.05f));

            yield return new WaitUntil(() => task.IsCompleted);

            task.GetAwaiter().GetResult();

            // Destroyは次のフレームで反映される。
            yield return null;

            Assert.That(target == null, Is.True, "破棄されたGameObjectはnull相当になる。");
        }

        /// <summary> 破棄対象がnullなら同期的に例外になる。 </summary>
        [Test]
        public void PausableDestroy_NullObject_ThrowsSynchronously()
        {
            Assert.Throws<ArgumentNullException>(
                () => PauseManager.PausableDestroy<IWaitCategory>(null, 0.05f));
        }

        /// <summary> PausableWaitUntilは条件成立で完了する。 </summary>
        [UnityTest]
        public IEnumerator PausableWaitUntil_CompletesWhenConditionMet()
        {
            bool condition = false;
            Task task = SymphonyAwaitable.AsTask(
                PauseManager.PausableWaitUntil<IWaitCategory>(() => condition));

            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }

            Assert.That(task.IsCompleted, Is.False, "条件が成立するまで完了してはいけない。");

            condition = true;

            yield return new WaitUntil(() => task.IsCompleted);
            task.GetAwaiter().GetResult();
        }
    }
}
