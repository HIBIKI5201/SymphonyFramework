using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace SymphonyFrameWork.Utility
{
    /// <summary>
    ///     UnityのAwaitableに、完了済み値、合成、条件待機、タイムアウト、Taskブリッジを提供する。
    ///     WhenAnyは未完了側のAwaitableと例外を安全に消費する一般契約を定められないため、
    ///     具体的な利用要件が生じるまで提供しない。
    /// </summary>
    public static class SymphonyAwaitable
    {
        /// <summary>
        ///     指定した処理をバックグラウンドスレッドで実行し、完了後にメインスレッドへ戻る。
        /// </summary>
        /// <param name="action"> バックグラウンドスレッドで実行する処理。 </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/>がnullの場合に送出する。
        /// </exception>
        public static async void BackGroundThreadAction(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            await Awaitable.BackgroundThreadAsync();

            try
            {
                action.Invoke();
            }
            finally
            {
                await Awaitable.MainThreadAsync();
            }

            Debug.Log($"{action.Method} is done");
        }

        /// <summary>
        ///     指定した処理をバックグラウンドスレッドで実行し、完了後にメインスレッドへ戻る。
        /// </summary>
        /// <param name="action"> バックグラウンドスレッドで実行する処理。 </param>
        /// <returns> バックグラウンド処理とメインスレッド復帰の完了を表すAwaitable。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/>がnullの場合に送出する。
        /// </exception>
        public static async Awaitable BackGroundThreadActionAsync(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            await Awaitable.BackgroundThreadAsync();

            try
            {
                action.Invoke();
            }
            finally
            {
                await Awaitable.MainThreadAsync();
            }

            Debug.Log($"{action.Method} is done");
        }

        /// <summary>
        ///     同期的に完了するAwaitableを作成する。
        ///     Awaitableは再awaitできないため、呼び出しごとに新しいインスタンスを返す。
        /// </summary>
        /// <returns> 新しく作成した完了済みAwaitable。 </returns>
        public static Awaitable Completed()
        {
            var completionSource = new AwaitableCompletionSource();
            completionSource.SetResult();
            return completionSource.Awaitable;
        }

        /// <summary>
        ///     指定した結果で同期的に完了するAwaitableを作成する。
        ///     Awaitableは再awaitできないため、呼び出しごとに新しいインスタンスを返す。
        /// </summary>
        /// <typeparam name="T"> 結果の型。 </typeparam>
        /// <param name="result"> 完了結果。 </param>
        /// <returns> 新しく作成した完了済みAwaitable。 </returns>
        public static Awaitable<T> FromResult<T>(T result)
        {
            var completionSource = new AwaitableCompletionSource<T>();
            completionSource.SetResult(result);
            return completionSource.Awaitable;
        }

        /// <summary>
        ///     指定したすべてのAwaitableを一度ずつ待機する。
        ///     渡したAwaitableのconsumerはこのメソッドだけとし、呼び出し側で別途awaitしてはならない。
        ///     空配列は同期的に正常完了する。null配列またはnull要素は待機開始前に例外とする。
        ///     同期完了済みの要素も通常どおり消費する。処理途中で例外またはキャンセルが発生しても
        ///     残りを最後まで消費し、例外があれば最初の例外を、例外がなくキャンセルがあれば
        ///     最初のキャンセルを全要素の終了後に通知する。
        /// </summary>
        /// <param name="awaitables"> 待機するAwaitableの一覧。 </param>
        /// <returns> すべてのAwaitableを消費したときに完了するAwaitable。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="awaitables"/>またはその要素がnullの場合に送出する。
        /// </exception>
        public static async Awaitable WhenAll(params Awaitable[] awaitables)
        {
            ValidateAwaitables(awaitables, nameof(awaitables));

            Exception firstException = null;
            OperationCanceledException firstCancellation = null;

            for (int i = 0; i < awaitables.Length; i++)
            {
                try
                {
                    await awaitables[i];
                }
                catch (OperationCanceledException exception)
                {
                    firstCancellation ??= exception;
                }
                catch (Exception exception)
                {
                    firstException ??= exception;
                }
            }

            if (firstException != null)
            {
                ExceptionDispatchInfo.Capture(firstException).Throw();
            }

            if (firstCancellation != null)
            {
                ExceptionDispatchInfo.Capture(firstCancellation).Throw();
            }
        }

        /// <summary>
        ///     指定したすべてのAwaitableを一度ずつ待機し、入力順に結果を返す。
        ///     渡したAwaitableのconsumerはこのメソッドだけとし、呼び出し側で別途awaitしてはならない。
        ///     空配列は空の結果配列で同期的に正常完了する。null配列またはnull要素は待機開始前に例外とする。
        ///     同期完了済みの要素も通常どおり消費する。処理途中で例外またはキャンセルが発生しても
        ///     残りを最後まで消費し、例外があれば最初の例外を、例外がなくキャンセルがあれば
        ///     最初のキャンセルを全要素の終了後に通知する。
        /// </summary>
        /// <typeparam name="T"> 結果の型。 </typeparam>
        /// <param name="awaitables"> 待機するAwaitableの一覧。 </param>
        /// <returns> 入力と同じ順序で格納した結果配列。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="awaitables"/>またはその要素がnullの場合に送出する。
        /// </exception>
        public static async Awaitable<T[]> WhenAll<T>(params Awaitable<T>[] awaitables)
        {
            ValidateAwaitables(awaitables, nameof(awaitables));

            var results = new T[awaitables.Length];
            Exception firstException = null;
            OperationCanceledException firstCancellation = null;

            for (int i = 0; i < awaitables.Length; i++)
            {
                try
                {
                    results[i] = await awaitables[i];
                }
                catch (OperationCanceledException exception)
                {
                    firstCancellation ??= exception;
                }
                catch (Exception exception)
                {
                    firstException ??= exception;
                }
            }

            if (firstException != null)
            {
                ExceptionDispatchInfo.Capture(firstException).Throw();
            }

            if (firstCancellation != null)
            {
                ExceptionDispatchInfo.Capture(firstCancellation).Throw();
            }

            return results;
        }

        /// <summary>
        ///     指定したAwaitableの完了後に処理を実行する。
        /// </summary>
        /// <param name="awaitable"> 完了を待機するAwaitable。 </param>
        /// <param name="action"> 完了後に実行する処理。nullの場合は何も実行しない。 </param>
        /// <param name="token"> 後続処理を中止するためのトークン。 </param>
        /// <returns> 指定した処理の実行完了を表すAwaitable。 </returns>
        public static async Awaitable OnComplete(
            this Awaitable awaitable,
            Action action,
            CancellationToken token = default)
        {
            await awaitable;

            if (token.IsCancellationRequested)
            {
                return;
            }

            action?.Invoke();
        }

        /// <summary>
        ///     条件がtrueになるまで、次のフレームまで待機する。
        ///     <see cref="WaitWhile"/>へ条件の否定を渡す処理と同等であり、
        ///     条件が最初からtrueの場合は、次のフレームを待たずに同期的に完了する。
        ///     条件評価中の例外はそのまま通知し、キャンセル時はOperationCanceledExceptionを通知する。
        /// </summary>
        /// <param name="predicate"> 待機を完了するときにtrueを返す条件。 </param>
        /// <param name="token"> 待機だけを中断するためのトークン。 </param>
        /// <returns> 条件がtrueになったときに完了するAwaitable。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="predicate"/>がnullの場合に送出する。
        /// </exception>
        public static Awaitable WaitUntil(
            Func<bool> predicate,
            CancellationToken token = default)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return WaitWhile(() => !predicate.Invoke(), token);
        }

        /// <summary>
        ///     条件がtrueの間、次のフレームまで待機する。
        ///     <see cref="WaitUntil"/>へ条件の否定を渡す処理と同等である。
        ///     条件が最初からfalseの場合は、次のフレームを待たずに同期的に完了する。
        ///     条件評価中の例外はそのまま通知し、キャンセル時はOperationCanceledExceptionを通知する。
        /// </summary>
        /// <param name="predicate"> 待機を継続する間trueを返す条件。 </param>
        /// <param name="token"> 待機だけを中断するためのトークン。 </param>
        /// <returns> 条件がfalseになったときに完了するAwaitable。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="predicate"/>がnullの場合に送出する。
        /// </exception>
        public static async Awaitable WaitWhile(
            Func<bool> predicate,
            CancellationToken token = default)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            token.ThrowIfCancellationRequested();

            while (predicate.Invoke())
            {
                await Awaitable.NextFrameAsync(token);
                token.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        ///     operation factoryが生成した処理を、指定時間と呼び出し側のキャンセルに関連付けて待機する。
        ///     factoryは受け取ったtokenを下位処理へ伝播しなければならない。timeoutは協調的であり、
        ///     tokenを無視する処理を強制停止できず、その処理が終了するまでは呼び出しも完了しない。
        ///     timeout後に処理が正常終了してもTimeoutExceptionを通知する。呼び出し側のキャンセルは
        ///     timeoutより優先し、OperationCanceledExceptionとして通知する。完了、例外、キャンセル、
        ///     timeoutのすべての経路でlinked tokenの登録と所有するCancellationTokenSourceを解放する。
        /// </summary>
        /// <param name="operationFactory"> linked tokenを受け取り、開始する処理。 </param>
        /// <param name="timeout"> timeoutまでの時間。無期限の場合はTimeout.InfiniteTimeSpan。 </param>
        /// <param name="token"> 呼び出し側の待機を中断するためのトークン。 </param>
        /// <returns> operation factoryが生成した処理の完了を表すAwaitable。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="operationFactory"/>がnullの場合に送出する。
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="timeout"/>が負数かつTimeout.InfiniteTimeSpanでない場合に送出する。
        /// </exception>
        /// <exception cref="TimeoutException"> 処理が指定時間内に完了しなかった場合に送出する。 </exception>
        public static Awaitable WithTimeout(
            Func<CancellationToken, Awaitable> operationFactory,
            TimeSpan timeout,
            CancellationToken token = default)
        {
            ValidateOperationFactory(operationFactory, nameof(operationFactory));
            ValidateTimeout(timeout);
            return WithTimeoutInternalAsync(operationFactory, timeout, token);
        }

        /// <summary>
        ///     operation factoryが生成した結果付き処理を、指定時間と呼び出し側のキャンセルに関連付けて待機する。
        ///     factoryは受け取ったtokenを下位処理へ伝播しなければならない。timeoutは協調的であり、
        ///     tokenを無視する処理を強制停止できず、その処理が終了するまでは呼び出しも完了しない。
        ///     timeout後に処理が正常終了してもTimeoutExceptionを通知する。呼び出し側のキャンセルは
        ///     timeoutより優先し、OperationCanceledExceptionとして通知する。完了、例外、キャンセル、
        ///     timeoutのすべての経路でlinked tokenの登録と所有するCancellationTokenSourceを解放する。
        /// </summary>
        /// <typeparam name="T"> 結果の型。 </typeparam>
        /// <param name="operationFactory"> linked tokenを受け取り、開始する結果付き処理。 </param>
        /// <param name="timeout"> timeoutまでの時間。無期限の場合はTimeout.InfiniteTimeSpan。 </param>
        /// <param name="token"> 呼び出し側の待機を中断するためのトークン。 </param>
        /// <returns> operation factoryが生成した処理の結果。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="operationFactory"/>がnullの場合に送出する。
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="timeout"/>が負数かつTimeout.InfiniteTimeSpanでない場合に送出する。
        /// </exception>
        /// <exception cref="TimeoutException"> 処理が指定時間内に完了しなかった場合に送出する。 </exception>
        public static Awaitable<T> WithTimeout<T>(
            Func<CancellationToken, Awaitable<T>> operationFactory,
            TimeSpan timeout,
            CancellationToken token = default)
        {
            ValidateOperationFactory(operationFactory, nameof(operationFactory));
            ValidateTimeout(timeout);
            return WithTimeoutInternalAsync(operationFactory, timeout, token);
        }

        /// <summary>
        ///     外部境界から受け取った結果付きTaskを、一度だけ待機できるAwaitableへ変換する。
        ///     nullは待機開始前に例外とし、同期完了、例外、元Taskのキャンセルをそのまま通知する。
        ///     tokenはブリッジの待機だけをキャンセルし、元Task自体をキャンセルしない。
        ///     待機キャンセル後も専用observerが元Taskの完了と例外を観測し続け、
        ///     未観測例外を作らない。ブリッジが完了ソースを所有し、待機完了、例外、
        ///     キャンセルのすべての経路でtoken登録を解放する。元Taskがどのスレッドで
        ///     完了しても待機側はメインスレッドで再開する。完了通知の時点で既に
        ///     メインスレッド上にいる場合は、追加のフレーム待機を行わない。
        /// </summary>
        /// <typeparam name="T"> 結果の型。 </typeparam>
        /// <param name="task"> 変換する外部Task。 </param>
        /// <param name="token"> ブリッジの待機だけを中断するためのトークン。 </param>
        /// <returns> 元Taskの結果を通知するAwaitable。 </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="task"/>がnullの場合に送出する。 </exception>
        public static Awaitable<T> FromTask<T>(
            Task<T> task,
            CancellationToken token = default)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var bridgeState = new TaskBridgeState<T>();
            var registrationOwner = new CancellationRegistrationOwner();
            RegisterCancellation(
                token,
                () => RunOnMainThread(bridgeState.CancelWait),
                registrationOwner);
            ObserveTask(task, bridgeState);
            return AwaitBridgeAsync(bridgeState.Awaitable, registrationOwner);
        }

        /// <summary>
        ///     外部境界から受け取ったTaskを、一度だけ待機できるAwaitableへ変換する。
        ///     nullは待機開始前に例外とし、同期完了、例外、元Taskのキャンセルをそのまま通知する。
        ///     tokenはブリッジの待機だけをキャンセルし、元Task自体をキャンセルしない。
        ///     待機キャンセル後も専用observerが元Taskの完了と例外を観測し続け、
        ///     未観測例外を作らない。ブリッジが完了ソースを所有し、待機完了、例外、
        ///     キャンセルのすべての経路でtoken登録を解放する。元Taskがどのスレッドで
        ///     完了しても待機側はメインスレッドで再開する。完了通知の時点で既に
        ///     メインスレッド上にいる場合は、追加のフレーム待機を行わない。
        /// </summary>
        /// <param name="task"> 変換する外部Task。 </param>
        /// <param name="token"> ブリッジの待機だけを中断するためのトークン。 </param>
        /// <returns> 元Taskの完了を通知するAwaitable。 </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="task"/>がnullの場合に送出する。 </exception>
        public static Awaitable FromTask(
            Task task,
            CancellationToken token = default)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var bridgeState = new TaskBridgeState();
            var registrationOwner = new CancellationRegistrationOwner();
            RegisterCancellation(
                token,
                () => RunOnMainThread(bridgeState.CancelWait),
                registrationOwner);
            ObserveTask(task, bridgeState);
            return AwaitBridgeAsync(bridgeState.Awaitable, registrationOwner);
        }

        /// <summary>
        ///     一度だけ待機できるAwaitableを、外部契約へ渡せるTaskへ変換する。
        ///     nullは待機開始前に例外とし、同期完了、例外、キャンセルをTaskへ通知する。
        ///     tokenは返すTaskの待機だけをキャンセルし、元AwaitableへCancelを要求しない。
        ///     キャンセル後も専用observerが元Awaitableを唯一のconsumerとして最後まで消費し、
        ///     例外を観測する。ブリッジがTaskCompletionSourceを所有し、完了、例外、
        ///     キャンセルのすべての経路でtoken登録を解放する。
        /// </summary>
        /// <param name="awaitable"> 変換するAwaitable。呼び出し側で別途awaitしてはならない。 </param>
        /// <param name="token"> 返すTaskの待機だけを中断するためのトークン。 </param>
        /// <returns> 元Awaitableの完了を通知するTask。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="awaitable"/>がnullの場合に送出する。
        /// </exception>
        public static Task AsTask(
            Awaitable awaitable,
            CancellationToken token = default)
        {
            if (awaitable == null)
            {
                throw new ArgumentNullException(nameof(awaitable));
            }

            var completionSource = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var registrationOwner = new CancellationRegistrationOwner();
            RegisterCancellation(
                token,
                () => completionSource.TrySetCanceled(),
                registrationOwner);
            _ = ObserveAwaitableAsync(awaitable, completionSource);
            _ = DisposeRegistrationAfterCompletionAsync(
                completionSource.Task,
                registrationOwner);
            return completionSource.Task;
        }

        /// <summary>
        ///     一度だけ待機できる結果付きAwaitableを、外部契約へ渡せるTaskへ変換する。
        ///     nullは待機開始前に例外とし、同期完了、結果、例外、キャンセルをTaskへ通知する。
        ///     tokenは返すTaskの待機だけをキャンセルし、元AwaitableへCancelを要求しない。
        ///     キャンセル後も専用observerが元Awaitableを唯一のconsumerとして最後まで消費し、
        ///     例外を観測する。ブリッジがTaskCompletionSourceを所有し、完了、例外、
        ///     キャンセルのすべての経路でtoken登録を解放する。
        /// </summary>
        /// <typeparam name="T"> 結果の型。 </typeparam>
        /// <param name="awaitable"> 変換するAwaitable。呼び出し側で別途awaitしてはならない。 </param>
        /// <param name="token"> 返すTaskの待機だけを中断するためのトークン。 </param>
        /// <returns> 元Awaitableの結果を通知するTask。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="awaitable"/>がnullの場合に送出する。
        /// </exception>
        public static Task<T> AsTask<T>(
            Awaitable<T> awaitable,
            CancellationToken token = default)
        {
            if (awaitable == null)
            {
                throw new ArgumentNullException(nameof(awaitable));
            }

            var completionSource = new TaskCompletionSource<T>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var registrationOwner = new CancellationRegistrationOwner();
            RegisterCancellation(
                token,
                () => completionSource.TrySetCanceled(),
                registrationOwner);
            _ = ObserveAwaitableAsync(awaitable, completionSource);
            _ = DisposeRegistrationAfterCompletionAsync(
                completionSource.Task,
                registrationOwner);
            return completionSource.Task;
        }

        /// <summary> Awaitable配列と各要素がnullでないことを検証する。 </summary>
        /// <param name="awaitables"> 検証する配列。 </param>
        /// <param name="parameterName"> 公開API上の引数名。 </param>
        private static void ValidateAwaitables(
            Awaitable[] awaitables,
            string parameterName)
        {
            if (awaitables == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            for (int i = 0; i < awaitables.Length; i++)
            {
                if (awaitables[i] == null)
                {
                    throw new ArgumentNullException(
                        $"{parameterName}[{i}]",
                        "Awaitableの要素にnullは指定できません。");
                }
            }
        }

        /// <summary> 結果付きAwaitable配列と各要素がnullでないことを検証する。 </summary>
        /// <typeparam name="T"> Awaitableの結果型。 </typeparam>
        /// <param name="awaitables"> 検証する配列。 </param>
        /// <param name="parameterName"> 公開API上の引数名。 </param>
        private static void ValidateAwaitables<T>(
            Awaitable<T>[] awaitables,
            string parameterName)
        {
            if (awaitables == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            for (int i = 0; i < awaitables.Length; i++)
            {
                if (awaitables[i] == null)
                {
                    throw new ArgumentNullException(
                        $"{parameterName}[{i}]",
                        "Awaitableの要素にnullは指定できません。");
                }
            }
        }

        /// <summary> operation factoryがnullでないことを検証する。 </summary>
        /// <typeparam name="TFactory"> factoryのdelegate型。 </typeparam>
        /// <param name="operationFactory"> 検証するfactory。 </param>
        /// <param name="parameterName"> 公開API上の引数名。 </param>
        private static void ValidateOperationFactory<TFactory>(
            TFactory operationFactory,
            string parameterName)
            where TFactory : class
        {
            if (operationFactory == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary> timeoutがCancellationTokenSourceで扱える下限以上か検証する。 </summary>
        /// <param name="timeout"> 検証するtimeout。 </param>
        private static void ValidateTimeout(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout),
                    timeout,
                    "timeoutには0以上またはTimeout.InfiniteTimeSpanを指定してください。");
            }
        }

        /// <summary> 結果なしoperationへ協調的timeoutを適用する。 </summary>
        /// <param name="operationFactory"> linked tokenを受け取るoperation factory。 </param>
        /// <param name="timeout"> timeoutまでの時間。 </param>
        /// <param name="token"> 呼び出し側のキャンセルトークン。 </param>
        /// <returns> operationの完了を表すAwaitable。 </returns>
        private static async Awaitable WithTimeoutInternalAsync(
            Func<CancellationToken, Awaitable> operationFactory,
            TimeSpan timeout,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            using var timeoutSource = new CancellationTokenSource();
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                token,
                timeoutSource.Token);

            StartTimeout(timeoutSource, timeout);

            try
            {
                await operationFactory.Invoke(linkedSource.Token);
            }
            catch (OperationCanceledException exception)
            {
                ThrowCancellationOrTimeout(token, timeoutSource.Token, exception);
                throw;
            }

            ThrowCancellationOrTimeout(token, timeoutSource.Token, null);
        }

        /// <summary> 結果付きoperationへ協調的timeoutを適用する。 </summary>
        /// <typeparam name="T"> operationの結果型。 </typeparam>
        /// <param name="operationFactory"> linked tokenを受け取るoperation factory。 </param>
        /// <param name="timeout"> timeoutまでの時間。 </param>
        /// <param name="token"> 呼び出し側のキャンセルトークン。 </param>
        /// <returns> operationの結果。 </returns>
        private static async Awaitable<T> WithTimeoutInternalAsync<T>(
            Func<CancellationToken, Awaitable<T>> operationFactory,
            TimeSpan timeout,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            using var timeoutSource = new CancellationTokenSource();
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                token,
                timeoutSource.Token);

            StartTimeout(timeoutSource, timeout);

            T result;

            try
            {
                result = await operationFactory.Invoke(linkedSource.Token);
            }
            catch (OperationCanceledException exception)
            {
                ThrowCancellationOrTimeout(token, timeoutSource.Token, exception);
                throw;
            }

            ThrowCancellationOrTimeout(token, timeoutSource.Token, null);
            return result;
        }

        /// <summary> CancellationTokenSourceへtimeoutを設定する。 </summary>
        /// <param name="timeoutSource"> timeoutを所有するsource。 </param>
        /// <param name="timeout"> timeoutまでの時間。 </param>
        private static void StartTimeout(
            CancellationTokenSource timeoutSource,
            TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                return;
            }

            if (timeout == TimeSpan.Zero)
            {
                timeoutSource.Cancel();
                return;
            }

            timeoutSource.CancelAfter(timeout);
        }

        /// <summary> 呼び出し側キャンセルまたはtimeoutを対応する例外として通知する。 </summary>
        /// <param name="callerToken"> 呼び出し側のトークン。 </param>
        /// <param name="timeoutToken"> timeoutを通知するトークン。 </param>
        /// <param name="innerException"> operationが通知したキャンセル例外。 </param>
        private static void ThrowCancellationOrTimeout(
            CancellationToken callerToken,
            CancellationToken timeoutToken,
            OperationCanceledException innerException)
        {
            if (callerToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(callerToken);
            }

            if (timeoutToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    "非同期処理が指定時間内に完了しませんでした。",
                    innerException);
            }
        }

        /// <summary> tokenのキャンセルを登録し、登録の所有権をownerへ移す。 </summary>
        /// <param name="token"> 登録元のトークン。 </param>
        /// <param name="cancelAction"> キャンセル時に実行する処理。 </param>
        /// <param name="registrationOwner"> 登録を解放する所有者。 </param>
        private static void RegisterCancellation(
            CancellationToken token,
            Action cancelAction,
            CancellationRegistrationOwner registrationOwner)
        {
            if (!token.CanBeCanceled)
            {
                return;
            }

            CancellationTokenRegistration registration = token.Register(cancelAction);
            registrationOwner.SetRegistration(registration);
        }

        /// <summary> 結果なしTaskを最後まで観測し、待機中ならブリッジを完了する。 </summary>
        /// <param name="task"> 観測するTask。 </param>
        /// <param name="bridgeState"> 完了結果を通知するブリッジ状態。 </param>
        private static void ObserveTask(Task task, TaskBridgeState bridgeState)
        {
            _ = task.ContinueWith(
                completedTask =>
                {
                    Exception exception = completedTask.IsFaulted
                        ? GetObservedException(completedTask)
                        : null;
                    bool isCanceled = completedTask.IsCanceled;
                    RunOnMainThread(
                        () => bridgeState.Complete(exception, isCanceled));
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        /// <summary> 結果付きTaskを最後まで観測し、待機中ならブリッジを完了する。 </summary>
        /// <typeparam name="T"> Taskの結果型。 </typeparam>
        /// <param name="task"> 観測するTask。 </param>
        /// <param name="bridgeState"> 完了結果を通知するブリッジ状態。 </param>
        private static void ObserveTask<T>(
            Task<T> task,
            TaskBridgeState<T> bridgeState)
        {
            _ = task.ContinueWith(
                completedTask =>
                {
                    Exception exception = completedTask.IsFaulted
                        ? GetObservedException(completedTask)
                        : null;
                    bool isCanceled = completedTask.IsCanceled;
                    T result = default;

                    if (exception == null && !isCanceled)
                    {
                        result = completedTask.GetAwaiter().GetResult();
                    }

                    RunOnMainThread(
                        () => bridgeState.Complete(exception, isCanceled, result));
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        /// <summary>
        ///     指定処理をメインスレッドで実行し、既にメインスレッドなら同期的に実行する。
        /// </summary>
        /// <param name="action"> メインスレッドで実行する処理。 </param>
        private static void RunOnMainThread(Action action)
        {
            var awaiter = Awaitable.MainThreadAsync().GetAwaiter();

            if (awaiter.IsCompleted)
            {
                awaiter.GetResult();
                action.Invoke();
                return;
            }

            awaiter.OnCompleted(
                () =>
                {
                    awaiter.GetResult();
                    action.Invoke();
                });
        }

        /// <summary> AwaitableCompletionSourceの完了を待ち、token登録を必ず解放する。 </summary>
        /// <param name="awaitable"> ブリッジが所有するAwaitable。 </param>
        /// <param name="registrationOwner"> 解放する登録の所有者。 </param>
        /// <returns> ブリッジの完了を表すAwaitable。 </returns>
        private static async Awaitable AwaitBridgeAsync(
            Awaitable awaitable,
            CancellationRegistrationOwner registrationOwner)
        {
            try
            {
                await awaitable;
            }
            finally
            {
                registrationOwner.Dispose();
            }
        }

        /// <summary> 結果付きAwaitableCompletionSourceの完了を待ち、token登録を必ず解放する。 </summary>
        /// <typeparam name="T"> 完了結果の型。 </typeparam>
        /// <param name="awaitable"> ブリッジが所有するAwaitable。 </param>
        /// <param name="registrationOwner"> 解放する登録の所有者。 </param>
        /// <returns> ブリッジの完了結果。 </returns>
        private static async Awaitable<T> AwaitBridgeAsync<T>(
            Awaitable<T> awaitable,
            CancellationRegistrationOwner registrationOwner)
        {
            try
            {
                return await awaitable;
            }
            finally
            {
                registrationOwner.Dispose();
            }
        }

        /// <summary> 元Awaitableを最後まで消費し、結果なしTaskへ完了状態を通知する。 </summary>
        /// <param name="awaitable"> 唯一のconsumerとして消費するAwaitable。 </param>
        /// <param name="completionSource"> 完了状態を通知するTaskCompletionSource。 </param>
        /// <returns> observer自体の完了を表すTask。 </returns>
        private static async Task ObserveAwaitableAsync(
            Awaitable awaitable,
            TaskCompletionSource<bool> completionSource)
        {
            try
            {
                await awaitable;
                completionSource.TrySetResult(true);
            }
            catch (OperationCanceledException)
            {
                completionSource.TrySetCanceled();
            }
            catch (Exception exception)
            {
                completionSource.TrySetException(exception);
            }
        }

        /// <summary> 元Awaitableを最後まで消費し、結果付きTaskへ完了状態を通知する。 </summary>
        /// <typeparam name="T"> 完了結果の型。 </typeparam>
        /// <param name="awaitable"> 唯一のconsumerとして消費するAwaitable。 </param>
        /// <param name="completionSource"> 完了状態を通知するTaskCompletionSource。 </param>
        /// <returns> observer自体の完了を表すTask。 </returns>
        private static async Task ObserveAwaitableAsync<T>(
            Awaitable<T> awaitable,
            TaskCompletionSource<T> completionSource)
        {
            try
            {
                T result = await awaitable;
                completionSource.TrySetResult(result);
            }
            catch (OperationCanceledException)
            {
                completionSource.TrySetCanceled();
            }
            catch (Exception exception)
            {
                completionSource.TrySetException(exception);
            }
        }

        /// <summary> Taskの完了状態を観測してからtoken登録を解放する。 </summary>
        /// <param name="task"> 完了を観測するTask。 </param>
        /// <param name="registrationOwner"> 解放する登録の所有者。 </param>
        /// <returns> 登録解放処理の完了を表すTask。 </returns>
        private static async Task DisposeRegistrationAfterCompletionAsync(
            Task task,
            CancellationRegistrationOwner registrationOwner)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 返却Taskのキャンセルは公開契約であり、登録解放側では観測だけを行う。
            }
            catch (Exception)
            {
                // 返却Taskの例外は公開契約であり、登録解放側では観測だけを行う。
            }
            finally
            {
                registrationOwner.Dispose();
            }
        }

        /// <summary> Taskの例外を観測し、ブリッジへ通知する例外を選ぶ。 </summary>
        /// <param name="task"> faulted状態のTask。 </param>
        /// <returns> 単一例外の場合は元例外、複数例外の場合はAggregateException。 </returns>
        private static Exception GetObservedException(Task task)
        {
            AggregateException aggregateException = task.Exception;

            if (aggregateException.InnerExceptions.Count == 1)
            {
                return aggregateException.InnerException;
            }

            return aggregateException;
        }

        /// <summary> CancellationTokenRegistrationを1つだけ所有して解放する。 </summary>
        private sealed class CancellationRegistrationOwner : IDisposable
        {
            private readonly object _gate = new();
            private CancellationTokenRegistration _registration;
            private bool _hasRegistration;
            private bool _isDisposed;

            /// <summary> 登録の所有権を設定し、既に解放済みなら即座に解放する。 </summary>
            /// <param name="registration"> 所有する登録。 </param>
            public void SetRegistration(CancellationTokenRegistration registration)
            {
                bool disposeImmediately;

                lock (_gate)
                {
                    disposeImmediately = _isDisposed;

                    if (!disposeImmediately)
                    {
                        _registration = registration;
                        _hasRegistration = true;
                    }
                }

                if (disposeImmediately)
                {
                    registration.Dispose();
                }
            }

            /// <summary> 所有する登録を一度だけ解放する。 </summary>
            public void Dispose()
            {
                CancellationTokenRegistration registration = default;
                bool hasRegistration;

                lock (_gate)
                {
                    if (_isDisposed)
                    {
                        return;
                    }

                    _isDisposed = true;
                    hasRegistration = _hasRegistration;

                    if (hasRegistration)
                    {
                        registration = _registration;
                        _registration = default;
                        _hasRegistration = false;
                    }
                }

                if (hasRegistration)
                {
                    registration.Dispose();
                }
            }
        }

        /// <summary> 結果なしTaskとAwaitableCompletionSourceの競合を一度だけ解決する。 </summary>
        private sealed class TaskBridgeState
        {
            private readonly AwaitableCompletionSource _completionSource = new();
            private int _isCompleted;

            /// <summary> ブリッジが所有する、一度だけ待機可能なAwaitable。 </summary>
            public Awaitable Awaitable => _completionSource.Awaitable;

            /// <summary> ブリッジの待機だけをキャンセルする。 </summary>
            public void CancelWait()
            {
                if (Interlocked.CompareExchange(ref _isCompleted, 1, 0) == 0)
                {
                    _completionSource.SetCanceled();
                }
            }

            /// <summary> 待機中の場合だけ元Taskの完了状態を通知する。 </summary>
            /// <param name="exception"> 元Taskから観測した例外。 </param>
            /// <param name="isCanceled"> 元Taskがキャンセルされた場合はtrue。 </param>
            public void Complete(Exception exception, bool isCanceled)
            {
                if (Interlocked.CompareExchange(ref _isCompleted, 1, 0) != 0)
                {
                    return;
                }

                if (exception != null)
                {
                    _completionSource.SetException(exception);
                }
                else if (isCanceled)
                {
                    _completionSource.SetCanceled();
                }
                else
                {
                    _completionSource.SetResult();
                }
            }
        }

        /// <summary> 結果付きTaskとAwaitableCompletionSourceの競合を一度だけ解決する。 </summary>
        /// <typeparam name="T"> Taskの結果型。 </typeparam>
        private sealed class TaskBridgeState<T>
        {
            private readonly AwaitableCompletionSource<T> _completionSource = new();
            private int _isCompleted;

            /// <summary> ブリッジが所有する、一度だけ待機可能なAwaitable。 </summary>
            public Awaitable<T> Awaitable => _completionSource.Awaitable;

            /// <summary> ブリッジの待機だけをキャンセルする。 </summary>
            public void CancelWait()
            {
                if (Interlocked.CompareExchange(ref _isCompleted, 1, 0) == 0)
                {
                    _completionSource.SetCanceled();
                }
            }

            /// <summary> 待機中の場合だけ元Taskの完了状態と結果を通知する。 </summary>
            /// <param name="exception"> 元Taskから観測した例外。 </param>
            /// <param name="isCanceled"> 元Taskがキャンセルされた場合はtrue。 </param>
            /// <param name="result"> 元Taskの完了結果。 </param>
            public void Complete(
                Exception exception,
                bool isCanceled,
                T result)
            {
                if (Interlocked.CompareExchange(ref _isCompleted, 1, 0) != 0)
                {
                    return;
                }

                if (exception != null)
                {
                    _completionSource.SetException(exception);
                }
                else if (isCanceled)
                {
                    _completionSource.SetCanceled();
                }
                else
                {
                    _completionSource.SetResult(result);
                }
            }
        }
    }
}
