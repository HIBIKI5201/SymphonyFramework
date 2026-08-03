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
        /// <summary> 各テストの後でポーズ状態を戻し、次のテストへ持ち越さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            if (PauseManager.IsInitialized)
            {
                PauseManager.Pause = false;
            }
        }

        /// <summary> PausableNextFrameAsyncを待機するとフレームが進む。 </summary>
        [UnityTest]
        public IEnumerator PausableNextFrameAsync_AdvancesFrame()
        {
            int startFrame = Time.frameCount;
            Task task = SymphonyAwaitable.AsTask(PauseManager.PausableNextFrameAsync());

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
                PauseManager.PausableWaitForSecondAsync(0.1f));

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
            PauseManager.Pause = true;

            Task task = SymphonyAwaitable.AsTask(
                PauseManager.PausableWaitForSecondAsync(0.1f));

            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }

            Assert.That(task.IsCompleted, Is.False, "ポーズ中に待機が完了してはいけない。");

            PauseManager.Pause = false;

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
                    await PauseManager.PausableWaitForSecondAsync(0.1f, cancellation.Token);
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

        /// <summary> PausableWaitUntilは条件成立で完了する。 </summary>
        [UnityTest]
        public IEnumerator PausableWaitUntil_CompletesWhenConditionMet()
        {
            bool condition = false;
            Task task = SymphonyAwaitable.AsTask(
                PauseManager.PausableWaitUntil(() => condition));

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
