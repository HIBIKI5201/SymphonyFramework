using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Debugger.Logger;

using UnityEngine;

namespace SymphonyFrameWork.Utility
{
    /// <summary>
    ///     UnityのAwaitableを合成し、Taskとの相互変換を提供する。
    /// </summary>
    /// <remarks>
    ///     未完了側を安全に消費する一般契約が無いため、WhenAnyは提供しない。
    /// </remarks>
    public static class SymphonyAwaitable
    {
        #region 外部向けAPI

        /// <summary>
        ///     指定した処理をバックグラウンドスレッドで実行し、完了後にメインスレッドへ戻る。
        /// </summary>
        /// <param name="action"> バックグラウンドスレッドで実行する処理。 </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/>がnullの場合に送出する。
        /// </exception>
        public static async void BackGroundThreadAction(Action action)
        {
            // 実行先のスレッドを切り替える前に、呼び出し誤りを同期的に通知する。
            if (action == null) { throw new ArgumentNullException(nameof(action)); }

            // Unity APIへ触れない処理だけをバックグラウンドへ移し、終了経路にかかわらずメインへ戻す。
            await Awaitable.BackgroundThreadAsync();

            try
            {
                action.Invoke();
            }
            finally
            {
                await Awaitable.MainThreadAsync();
            }

            // fire-and-forget処理の正常完了を実行環境でも追跡できるよう、Unity標準ログへ出力する。
            SymphonyDebugLogger.LogDirect($"{action.Method} is done");
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
            // 実行先のスレッドを切り替える前に、呼び出し誤りを同期的に通知する。
            if (action == null) { throw new ArgumentNullException(nameof(action)); }

            // Unity APIへ触れない処理だけをバックグラウンドへ移し、終了経路にかかわらずメインへ戻す。
            await Awaitable.BackgroundThreadAsync();

            try
            {
                action.Invoke();
            }
            finally
            {
                await Awaitable.MainThreadAsync();
            }

            // 待機可能な処理でも既存契約の完了ログを実行環境へ残す。
            SymphonyDebugLogger.LogDirect($"{action.Method} is done");
        }

        /// <summary>
        ///     同期的に完了するAwaitableを作成する。
        /// </summary>
        /// <remarks> Awaitableは再awaitできないため、呼び出しごとに新しいインスタンスを返す。 </remarks>
        /// <returns> 新しく作成した完了済みAwaitable。 </returns>
        public static Awaitable Completed()
        {
            // プール済みAwaitableの共有を避け、呼び出しごとに一度だけawaitできる完了ソースを所有する。
            AwaitableCompletionSource completionSource = new();
            completionSource.SetResult();
            return completionSource.Awaitable;
        }

        /// <summary>
        ///     指定した結果で同期的に完了するAwaitableを作成する。
        /// </summary>
        /// <remarks> Awaitableは再awaitできないため、呼び出しごとに新しいインスタンスを返す。 </remarks>
        /// <typeparam name="T"> 結果の型。 </typeparam>
        /// <param name="result"> 完了結果。 </param>
        /// <returns> 新しく作成した完了済みAwaitable。 </returns>
        public static Awaitable<T> FromResult<T>(T result)
        {
            // 結果型ごとに専用ソースを作り、一度だけawaitできる結果付きAwaitableを返す。
            AwaitableCompletionSource<T> completionSource = new();
            completionSource.SetResult(result);
            return completionSource.Awaitable;
        }

        /// <summary>
        ///     指定したすべてのAwaitableを一度ずつ待機する。
        /// </summary>
        /// <remarks>
        ///     渡したAwaitableはこのメソッドだけでawaitする。空配列は同期的に完了する。
        ///     途中で失敗しても全要素を消費し、最初の例外またはキャンセルを最後に通知する。
        /// </remarks>
        /// <param name="awaitables"> 待機するAwaitableの一覧。 </param>
        /// <returns> すべてのAwaitableを消費したときに完了するAwaitable。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="awaitables"/>またはその要素がnullの場合に送出する。
        /// </exception>
        public static async Awaitable WhenAll(params Awaitable[] awaitables)
        {
            // 一部を消費した後でnullに失敗すると残りを安全に再利用できないため、開始前に全要素を検証する。
            ValidateAwaitables(awaitables, nameof(awaitables));

            Exception firstException = null;
            OperationCanceledException firstCancellation = null;

            // Awaitableは一度だけawaitできるため、このメソッドを唯一のconsumerとして全要素を最後まで消費する。
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

            // 全要素の消費後は、通常の失敗をキャンセルより優先して最初の例外を元スタック付きで通知する。
            if (firstException != null) { ExceptionDispatchInfo.Capture(firstException).Throw(); }

            // 通常例外が無くキャンセルだけが発生した場合は、最初のキャンセルを元スタック付きで通知する。
            if (firstCancellation != null) { ExceptionDispatchInfo.Capture(firstCancellation).Throw(); }
        }

        /// <summary>
        ///     指定したすべてのAwaitableを一度ずつ待機する。
        /// </summary>
        /// <remarks>
        ///     渡したAwaitableはこのメソッドだけでawaitする。空配列は同期的に完了する。
        ///     途中で失敗しても全要素を消費し、最初の例外またはキャンセルを最後に通知する。
        /// </remarks>
        /// <typeparam name="T"> 結果の型。 </typeparam>
        /// <param name="awaitables"> 待機するAwaitableの一覧。 </param>
        /// <returns> 入力と同じ順序で格納した結果配列。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="awaitables"/>またはその要素がnullの場合に送出する。
        /// </exception>
        public static async Awaitable<T[]> WhenAll<T>(params Awaitable<T>[] awaitables)
        {
            // 一部を消費した後でnullに失敗すると残りを安全に再利用できないため、開始前に全要素を検証する。
            ValidateAwaitables(awaitables, nameof(awaitables));

            T[] results = new T[awaitables.Length];
            Exception firstException = null;
            OperationCanceledException firstCancellation = null;

            // Awaitableは一度だけawaitできるため、このメソッドだけで全要素を消費し入力順の位置へ格納する。
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

            // 全要素の消費後は、通常の失敗をキャンセルより優先して最初の例外を元スタック付きで通知する。
            if (firstException != null) { ExceptionDispatchInfo.Capture(firstException).Throw(); }

            // 通常例外が無くキャンセルだけが発生した場合は、最初のキャンセルを元スタック付きで通知する。
            if (firstCancellation != null) { ExceptionDispatchInfo.Capture(firstCancellation).Throw(); }

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
            // 元Awaitableを先に唯一のconsumerとして消費し、その正常完了後だけ後続処理を検討する。
            await awaitable;

            // 元処理の完了後に呼び出し側が不要になった場合は、後続の副作用だけを抑止する。
            if (token.IsCancellationRequested) { return; }

            action?.Invoke();
        }

        /// <summary>
        ///     条件がtrueになるまで待機する。
        /// </summary>
        /// <remarks> 条件が最初からtrueの場合は同期的に完了する。 </remarks>
        /// <param name="predicate"> 待機を完了するときにtrueを返す条件。 </param>
        /// <param name="token"> 待機だけを中断するためのトークン。 </param>
        /// <returns> 条件がtrueになったときに完了するAwaitable。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="predicate"/>がnullの場合に送出する。
        /// </exception>
        /// <exception cref="OperationCanceledException"> 待機がキャンセルされた場合に送出する。 </exception>
        public static Awaitable WaitUntil(
            Func<bool> predicate,
            CancellationToken token = default)
        {
            // 条件を評価するループへnullを渡さず、呼び出し時点で誤りを通知する。
            if (predicate == null) { throw new ArgumentNullException(nameof(predicate)); }

            // 条件待機を一箇所へ集約し、trueになるまでの待機を否定条件で表す。
            return WaitWhile(() => !predicate.Invoke(), token);
        }

        /// <summary>
        ///     条件がtrueの間待機する。
        /// </summary>
        /// <remarks> 条件が最初からfalseの場合は同期的に完了する。 </remarks>
        /// <param name="predicate"> 待機を継続する間trueを返す条件。 </param>
        /// <param name="token"> 待機だけを中断するためのトークン。 </param>
        /// <returns> 条件がfalseになったときに完了するAwaitable。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="predicate"/>がnullの場合に送出する。
        /// </exception>
        /// <exception cref="OperationCanceledException"> 待機がキャンセルされた場合に送出する。 </exception>
        public static async Awaitable WaitWhile(
            Func<bool> predicate,
            CancellationToken token = default)
        {
            // 条件を評価するループへnullを渡さず、呼び出し時点で誤りを通知する。
            if (predicate == null) { throw new ArgumentNullException(nameof(predicate)); }

            // 条件が最初からfalseでも、開始前のキャンセルだけは見落とさず通知する。
            token.ThrowIfCancellationRequested();

            // 条件がtrueの間だけ次フレームへ進み、各再開後にもキャンセルを確認する。
            while (predicate.Invoke())
            {
                await Awaitable.NextFrameAsync(token);
                token.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        ///     operation factoryが生成した処理へtimeoutを適用する。
        /// </summary>
        /// <remarks>
        ///     factoryは受け取ったtokenを下位処理へ伝播する。timeoutは協調的であり、
        ///     tokenを無視する処理は強制停止しない。呼び出し側のキャンセルをtimeoutより優先する。
        /// </remarks>
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
            // operationを開始する前に引数を検証し、作成済みAwaitableの消費責任を曖昧にしない。
            ValidateOperationFactory(operationFactory, nameof(operationFactory));
            ValidateTimeout(timeout);
            return WithTimeoutInternalAsync(operationFactory, timeout, token);
        }

        /// <summary>
        ///     operation factoryが生成した結果付き処理へtimeoutを適用する。
        /// </summary>
        /// <remarks>
        ///     factoryは受け取ったtokenを下位処理へ伝播する。timeoutは協調的であり、
        ///     tokenを無視する処理は強制停止しない。呼び出し側のキャンセルをtimeoutより優先する。
        /// </remarks>
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
            // operationを開始する前に引数を検証し、作成済みAwaitableの消費責任を曖昧にしない。
            ValidateOperationFactory(operationFactory, nameof(operationFactory));
            ValidateTimeout(timeout);
            return WithTimeoutInternalAsync(operationFactory, timeout, token);
        }

        /// <summary>
        ///     結果付きTaskを一度だけ待機できるAwaitableへ変換する。
        /// </summary>
        /// <remarks>
        ///     tokenは待機だけをキャンセルする。元Taskは完了まで観測し、待機側をメインスレッドで再開する。
        /// </remarks>
        /// <typeparam name="T"> 結果の型。 </typeparam>
        /// <param name="task"> 変換する外部Task。 </param>
        /// <param name="token"> ブリッジの待機だけを中断するためのトークン。 </param>
        /// <returns> 元Taskの結果を通知するAwaitable。 </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="task"/>がnullの場合に送出する。 </exception>
        public static Awaitable<T> FromTask<T>(
            Task<T> task,
            CancellationToken token = default)
        {
            // 変換元が無い場合はobserverを構築せず、呼び出し時点で誤りを通知する。
            if (task == null) { throw new ArgumentNullException(nameof(task)); }

            // Taskの完了と待機キャンセルの競合を一度だけ解決する専用状態を、変換ごとに所有する。
            TaskBridgeState<T> bridgeState = new();
            CancellationRegistrationOwner registrationOwner = new();
            RegisterCancellation(
                token,
                () => RunOnMainThread(bridgeState.CancelWait),
                registrationOwner);
            // 待機が先にキャンセルされても元Taskの例外を未観測にしないよう、observerは最後まで残す。
            ObserveTask(task, bridgeState);
            return AwaitBridgeAsync(bridgeState.Awaitable, registrationOwner);
        }

        /// <summary>
        ///     Taskを一度だけ待機できるAwaitableへ変換する。
        /// </summary>
        /// <remarks>
        ///     tokenは待機だけをキャンセルする。元Taskは完了まで観測し、待機側をメインスレッドで再開する。
        /// </remarks>
        /// <param name="task"> 変換する外部Task。 </param>
        /// <param name="token"> ブリッジの待機だけを中断するためのトークン。 </param>
        /// <returns> 元Taskの完了を通知するAwaitable。 </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="task"/>がnullの場合に送出する。 </exception>
        public static Awaitable FromTask(
            Task task,
            CancellationToken token = default)
        {
            // 変換元が無い場合はobserverを構築せず、呼び出し時点で誤りを通知する。
            if (task == null) { throw new ArgumentNullException(nameof(task)); }

            // Taskの完了と待機キャンセルの競合を一度だけ解決する専用状態を、変換ごとに所有する。
            TaskBridgeState bridgeState = new();
            CancellationRegistrationOwner registrationOwner = new();
            RegisterCancellation(
                token,
                () => RunOnMainThread(bridgeState.CancelWait),
                registrationOwner);
            // 待機が先にキャンセルされても元Taskの例外を未観測にしないよう、observerは最後まで残す。
            ObserveTask(task, bridgeState);
            return AwaitBridgeAsync(bridgeState.Awaitable, registrationOwner);
        }

        /// <summary>
        ///     一度だけ待機できるAwaitableをTaskへ変換する。
        /// </summary>
        /// <remarks>
        ///     tokenは返すTaskだけをキャンセルする。元Awaitableは唯一のconsumerが最後まで消費する。
        /// </remarks>
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
            // 変換元が無い場合は完了ソースを構築せず、呼び出し時点で誤りを通知する。
            if (awaitable == null) { throw new ArgumentNullException(nameof(awaitable)); }

            // Awaitableを唯一のconsumerとして消費し、外部向けTaskの継続は非同期に実行して再入を避ける。
            TaskCompletionSource<bool> completionSource = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationRegistrationOwner registrationOwner = new();
            RegisterCancellation(
                token,
                () => completionSource.TrySetCanceled(),
                registrationOwner);
            // 返却Taskが先にキャンセルされても、プール返却と例外観測のため元Awaitableを最後まで消費する。
            _ = ObserveAwaitableAsync(awaitable, completionSource);
            _ = DisposeRegistrationAfterCompletionAsync(
                completionSource.Task,
                registrationOwner);
            return completionSource.Task;
        }

        /// <summary>
        ///     一度だけ待機できる結果付きAwaitableをTaskへ変換する。
        /// </summary>
        /// <remarks>
        ///     tokenは返すTaskだけをキャンセルする。元Awaitableは唯一のconsumerが最後まで消費する。
        /// </remarks>
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
            // 変換元が無い場合は完了ソースを構築せず、呼び出し時点で誤りを通知する。
            if (awaitable == null) { throw new ArgumentNullException(nameof(awaitable)); }

            // Awaitableを唯一のconsumerとして消費し、外部向けTaskの継続は非同期に実行して再入を避ける。
            TaskCompletionSource<T> completionSource = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationRegistrationOwner registrationOwner = new();
            RegisterCancellation(
                token,
                () => completionSource.TrySetCanceled(),
                registrationOwner);
            // 返却Taskが先にキャンセルされても、プール返却と例外観測のため元Awaitableを最後まで消費する。
            _ = ObserveAwaitableAsync(awaitable, completionSource);
            _ = DisposeRegistrationAfterCompletionAsync(
                completionSource.Task,
                registrationOwner);
            return completionSource.Task;
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     Awaitable配列と各要素がnullでないことを検証する。
        /// </summary>
        /// <param name="awaitables"> 検証する配列。 </param>
        /// <param name="parameterName"> 公開API上の引数名。 </param>
        private static void ValidateAwaitables(
            Awaitable[] awaitables,
            string parameterName)
        {
            // 配列自体が無い場合は要素検証を開始できないため、公開引数名で通知する。
            if (awaitables == null) { throw new ArgumentNullException(parameterName); }

            // Awaitableを一つも消費しない段階で、全要素の有効性を確認する。
            for (int i = 0; i < awaitables.Length; i++)
            {
                // null要素は一度だけawaitする契約を満たせないため、添字を含む引数名で通知する。
                if (awaitables[i] == null)
                {
                    throw new ArgumentNullException(
                        $"{parameterName}[{i}]",
                        "Awaitableの要素にnullは指定できません。");
                }
            }
        }

        /// <summary>
        ///     結果付きAwaitable配列と各要素がnullでないことを検証する。
        /// </summary>
        /// <typeparam name="T"> Awaitableの結果型。 </typeparam>
        /// <param name="awaitables"> 検証する配列。 </param>
        /// <param name="parameterName"> 公開API上の引数名。 </param>
        private static void ValidateAwaitables<T>(
            Awaitable<T>[] awaitables,
            string parameterName)
        {
            // 配列自体が無い場合は要素検証を開始できないため、公開引数名で通知する。
            if (awaitables == null) { throw new ArgumentNullException(parameterName); }

            // Awaitableを一つも消費しない段階で、全要素の有効性を確認する。
            for (int i = 0; i < awaitables.Length; i++)
            {
                // null要素は一度だけawaitする契約を満たせないため、添字を含む引数名で通知する。
                if (awaitables[i] == null)
                {
                    throw new ArgumentNullException(
                        $"{parameterName}[{i}]",
                        "Awaitableの要素にnullは指定できません。");
                }
            }
        }

        /// <summary>
        ///     operation factoryがnullでないことを検証する。
        /// </summary>
        /// <typeparam name="TFactory"> factoryのdelegate型。 </typeparam>
        /// <param name="operationFactory"> 検証するfactory。 </param>
        /// <param name="parameterName"> 公開API上の引数名。 </param>
        private static void ValidateOperationFactory<TFactory>(
            TFactory operationFactory,
            string parameterName)
            where TFactory : class
        {
            // factoryが無い状態ではキャンセルトークンを伝播できないため、operation作成前に拒否する。
            if (operationFactory == null) { throw new ArgumentNullException(parameterName); }
        }

        /// <summary>
        ///     timeoutがCancellationTokenSourceで扱える下限以上か検証する。
        /// </summary>
        /// <param name="timeout"> 検証するtimeout。 </param>
        private static void ValidateTimeout(TimeSpan timeout)
        {
            // CancellationTokenSourceが扱える負数は無期限だけのため、それ以外は開始前に拒否する。
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout),
                    timeout,
                    "timeoutには0以上またはTimeout.InfiniteTimeSpanを指定してください。");
            }
        }

        /// <summary>
        ///     結果なしoperationへ協調的timeoutを適用する。
        /// </summary>
        /// <param name="operationFactory"> linked tokenを受け取るoperation factory。 </param>
        /// <param name="timeout"> timeoutまでの時間。 </param>
        /// <param name="token"> 呼び出し側のキャンセルトークン。 </param>
        /// <returns> operationの完了を表すAwaitable。 </returns>
        private static async Awaitable WithTimeoutInternalAsync(
            Func<CancellationToken, Awaitable> operationFactory,
            TimeSpan timeout,
            CancellationToken token)
        {
            // factoryを呼ぶ前のキャンセルではoperation自体を開始しない。
            token.ThrowIfCancellationRequested();

            // 呼び出し側とtimeoutの所有権を分離し、どの終了経路でも両sourceを解放する。
            using CancellationTokenSource timeoutSource = new();
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                token,
                timeoutSource.Token);

            StartTimeout(timeoutSource, timeout);

            // operationにはlinked tokenを渡し、キャンセル原因は呼び出し側優先で変換する。
            try
            {
                await operationFactory.Invoke(linkedSource.Token);
            }
            catch (OperationCanceledException exception)
            {
                ThrowCancellationOrTimeout(token, timeoutSource.Token, exception);
                throw;
            }

            // tokenを無視したoperationがtimeout後に正常終了した場合も、timeout契約を優先する。
            ThrowCancellationOrTimeout(token, timeoutSource.Token, null);
        }

        /// <summary>
        ///     結果付きoperationへ協調的timeoutを適用する。
        /// </summary>
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
            // factoryを呼ぶ前のキャンセルではoperation自体を開始しない。
            token.ThrowIfCancellationRequested();

            // 呼び出し側とtimeoutの所有権を分離し、どの終了経路でも両sourceを解放する。
            using CancellationTokenSource timeoutSource = new();
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                token,
                timeoutSource.Token);

            StartTimeout(timeoutSource, timeout);

            T result;

            // operationにはlinked tokenを渡し、キャンセル原因は呼び出し側優先で変換する。
            try
            {
                result = await operationFactory.Invoke(linkedSource.Token);
            }
            catch (OperationCanceledException exception)
            {
                ThrowCancellationOrTimeout(token, timeoutSource.Token, exception);
                throw;
            }

            // tokenを無視したoperationがtimeout後に正常終了した場合も、結果よりtimeout契約を優先する。
            ThrowCancellationOrTimeout(token, timeoutSource.Token, null);
            return result;
        }

        /// <summary>
        ///     CancellationTokenSourceへtimeoutを設定する。
        /// </summary>
        /// <param name="timeoutSource"> timeoutを所有するsource。 </param>
        /// <param name="timeout"> timeoutまでの時間。 </param>
        private static void StartTimeout(
            CancellationTokenSource timeoutSource,
            TimeSpan timeout)
        {
            // 無期限指定ではタイマーを作らず、operation自身の完了か呼び出し側キャンセルだけを待つ。
            if (timeout == Timeout.InfiniteTimeSpan) { return; }

            // 0秒はCancelAfterの遅延を挟まず、operation開始時点からtimeout済みとして扱う。
            if (timeout == TimeSpan.Zero)
            {
                timeoutSource.Cancel();
                return;
            }

            timeoutSource.CancelAfter(timeout);
        }

        /// <summary>
        ///     呼び出し側キャンセルまたはtimeoutを対応する例外として通知する。
        /// </summary>
        /// <param name="callerToken"> 呼び出し側のトークン。 </param>
        /// <param name="timeoutToken"> timeoutを通知するトークン。 </param>
        /// <param name="innerException"> operationが通知したキャンセル例外。 </param>
        private static void ThrowCancellationOrTimeout(
            CancellationToken callerToken,
            CancellationToken timeoutToken,
            OperationCanceledException innerException)
        {
            // 両方が同時に成立した場合は、利用側が明示したキャンセルをtimeoutより優先する。
            if (callerToken.IsCancellationRequested) { throw new OperationCanceledException(callerToken); }

            // 呼び出し側のキャンセルが無くtimeoutだけ成立した場合は、専用例外へ変換する。
            if (timeoutToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    "非同期処理が指定時間内に完了しませんでした。",
                    innerException);
            }
        }

        /// <summary>
        ///     tokenのキャンセルを登録し、登録の所有権をownerへ移す。
        /// </summary>
        /// <param name="token"> 登録元のトークン。 </param>
        /// <param name="cancelAction"> キャンセル時に実行する処理。 </param>
        /// <param name="registrationOwner"> 登録を解放する所有者。 </param>
        private static void RegisterCancellation(
            CancellationToken token,
            Action cancelAction,
            CancellationRegistrationOwner registrationOwner)
        {
            // キャンセル不能なtokenでは登録と解放管理が不要なため、何も所有しない。
            if (!token.CanBeCanceled) { return; }

            // Register直後にキャンセル処理が完了しても、owner側が登録を即時解放できるよう所有権を渡す。
            CancellationTokenRegistration registration = token.Register(cancelAction);
            registrationOwner.SetRegistration(registration);
        }

        /// <summary>
        ///     結果なしTaskを最後まで観測し、待機中ならブリッジを完了する。
        /// </summary>
        /// <param name="task"> 観測するTask。 </param>
        /// <param name="bridgeState"> 完了結果を通知するブリッジ状態。 </param>
        private static void ObserveTask(Task task, TaskBridgeState bridgeState)
        {
            // 待機キャンセル後も元Taskの例外を観測し、完了通知だけをメインスレッドへ戻す。
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

        /// <summary>
        ///     結果付きTaskを最後まで観測し、待機中ならブリッジを完了する。
        /// </summary>
        /// <typeparam name="T"> Taskの結果型。 </typeparam>
        /// <param name="task"> 観測するTask。 </param>
        /// <param name="bridgeState"> 完了結果を通知するブリッジ状態。 </param>
        private static void ObserveTask<T>(
            Task<T> task,
            TaskBridgeState<T> bridgeState)
        {
            // 待機キャンセル後も元Taskの例外を観測し、完了通知だけをメインスレッドへ戻す。
            _ = task.ContinueWith(
                completedTask =>
                {
                    Exception exception = completedTask.IsFaulted
                        ? GetObservedException(completedTask)
                        : null;
                    bool isCanceled = completedTask.IsCanceled;
                    T result = default;

                    // 正常完了時だけ結果を取り出し、faultedまたはcanceled状態でGetResultを重ねて呼ばない。
                    if (exception == null && !isCanceled) { result = completedTask.GetAwaiter().GetResult(); }

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
            // Awaitableを生成したまま放置せず、必ずawaiterを消費してプールへ返却する。
            MainThreadAwaitable awaiter = Awaitable.MainThreadAsync().GetAwaiter();

            // 既にメインスレッドの場合はフレームを遅らせず、awaiterを同期的に完了させる。
            if (awaiter.IsCompleted)
            {
                awaiter.GetResult();
                action.Invoke();
                return;
            }

            // 別スレッドの場合はメインスレッド復帰後にawaiterを完了させてから処理を実行する。
            awaiter.OnCompleted(
                () =>
                {
                    awaiter.GetResult();
                    action.Invoke();
                });
        }

        /// <summary>
        ///     AwaitableCompletionSourceの完了を待ち、token登録を解放する。
        /// </summary>
        /// <param name="awaitable"> ブリッジが所有するAwaitable。 </param>
        /// <param name="registrationOwner"> 解放する登録の所有者。 </param>
        /// <returns> ブリッジの完了を表すAwaitable。 </returns>
        private static async Awaitable AwaitBridgeAsync(
            Awaitable awaitable,
            CancellationRegistrationOwner registrationOwner)
        {
            // 完了ソースのAwaitableはここだけでawaitし、終了後は登録の所有権を必ず解放する。
            try
            {
                await awaitable;
            }
            finally
            {
                registrationOwner.Dispose();
            }
        }

        /// <summary>
        ///     結果付きAwaitableCompletionSourceの完了を待ち、token登録を解放する。
        /// </summary>
        /// <typeparam name="T"> 完了結果の型。 </typeparam>
        /// <param name="awaitable"> ブリッジが所有するAwaitable。 </param>
        /// <param name="registrationOwner"> 解放する登録の所有者。 </param>
        /// <returns> ブリッジの完了結果。 </returns>
        private static async Awaitable<T> AwaitBridgeAsync<T>(
            Awaitable<T> awaitable,
            CancellationRegistrationOwner registrationOwner)
        {
            // 結果付きAwaitableはここだけでawaitし、終了後は登録の所有権を必ず解放する。
            try
            {
                return await awaitable;
            }
            finally
            {
                registrationOwner.Dispose();
            }
        }

        /// <summary>
        ///     元Awaitableを最後まで消費し、結果なしTaskへ完了状態を通知する。
        /// </summary>
        /// <param name="awaitable"> 唯一のconsumerとして消費するAwaitable。 </param>
        /// <param name="completionSource"> 完了状態を通知するTaskCompletionSource。 </param>
        /// <returns> observer自体の完了を表すTask。 </returns>
        private static async Task ObserveAwaitableAsync(
            Awaitable awaitable,
            TaskCompletionSource<bool> completionSource)
        {
            // 元Awaitableの唯一のconsumerとして、プール返却のため全ての終了状態を最後まで観測する。
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

        /// <summary>
        ///     元Awaitableを最後まで消費し、結果付きTaskへ完了状態を通知する。
        /// </summary>
        /// <typeparam name="T"> 完了結果の型。 </typeparam>
        /// <param name="awaitable"> 唯一のconsumerとして消費するAwaitable。 </param>
        /// <param name="completionSource"> 完了状態を通知するTaskCompletionSource。 </param>
        /// <returns> observer自体の完了を表すTask。 </returns>
        private static async Task ObserveAwaitableAsync<T>(
            Awaitable<T> awaitable,
            TaskCompletionSource<T> completionSource)
        {
            // 元Awaitableの唯一のconsumerとして、プール返却のため全ての終了状態を最後まで観測する。
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

        /// <summary>
        ///     Taskの完了状態を観測してからtoken登録を解放する。
        /// </summary>
        /// <param name="task"> 完了を観測するTask。 </param>
        /// <param name="registrationOwner"> 解放する登録の所有者。 </param>
        /// <returns> 登録解放処理の完了を表すTask。 </returns>
        private static async Task DisposeRegistrationAfterCompletionAsync(
            Task task,
            CancellationRegistrationOwner registrationOwner)
        {
            // Taskの結果は公開側へ任せ、このobserverは終了状態を消費して登録解放だけを保証する。
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

        /// <summary>
        ///     Taskの例外を観測し、ブリッジへ通知する例外を選ぶ。
        /// </summary>
        /// <param name="task"> faulted状態のTask。 </param>
        /// <returns> 単一例外の場合は元例外、複数例外の場合はAggregateException。 </returns>
        private static Exception GetObservedException(Task task)
        {
            AggregateException aggregateException = task.Exception;

            // 単一例外ではAggregateExceptionの包装を外し、元Taskの失敗を直接通知する。
            if (aggregateException.InnerExceptions.Count == 1) { return aggregateException.InnerException; }

            return aggregateException;
        }

        /// <summary>
        ///     CancellationTokenRegistrationを1つだけ所有して解放する。
        /// </summary>
        private sealed class CancellationRegistrationOwner : IDisposable
        {
            #region 外部向けAPI

            /// <summary>
            ///     登録の所有権を設定し、既に解放済みなら即座に解放する。
            /// </summary>
            /// <param name="registration"> 所有する登録。 </param>
            public void SetRegistration(CancellationTokenRegistration registration)
            {
                bool disposeImmediately;

                // 登録設定と解放が別スレッドで競合しても、所有権を一方だけに確定する。
                lock (_gate)
                {
                    disposeImmediately = _isDisposed;

                    // ownerが有効な間だけ登録を保持し、後続のDisposeへ解放責任を移す。
                    if (!disposeImmediately)
                    {
                        _registration = registration;
                        _hasRegistration = true;
                    }
                }

                // Disposeが先に完了していた場合は、ownerへ保持せず呼び出し側で直ちに解放する。
                if (disposeImmediately) { registration.Dispose(); }
            }

            /// <summary>
            ///     所有する登録を一度だけ解放する。
            /// </summary>
            public void Dispose()
            {
                CancellationTokenRegistration registration = default;
                bool hasRegistration;

                // 多重Disposeと登録設定との競合をlock内で解決し、解放対象だけを外へ取り出す。
                lock (_gate)
                {
                    // 既に解放済みの場合は、同じ登録を二重にDisposeしない。
                    if (_isDisposed) { return; }

                    _isDisposed = true;
                    hasRegistration = _hasRegistration;

                    // 登録を所有している場合だけフィールドから切り離し、再解放できない状態にする。
                    if (hasRegistration)
                    {
                        registration = _registration;
                        _registration = default;
                        _hasRegistration = false;
                    }
                }

                // CancellationTokenRegistration.Disposeによるコールバック待機をlock外で行う。
                if (hasRegistration) { registration.Dispose(); }
            }

            #endregion

            #region 内部処理

            private readonly object _gate = new();
            private CancellationTokenRegistration _registration;
            private bool _hasRegistration;
            private bool _isDisposed;

            #endregion
        }

        /// <summary>
        ///     結果なしTaskとAwaitableCompletionSourceの競合を一度だけ解決する。
        /// </summary>
        private sealed class TaskBridgeState
        {
            #region 外部向けAPI

            /// <summary> ブリッジが所有する、一度だけ待機可能なAwaitable。 </summary>
            public Awaitable Awaitable => _completionSource.Awaitable;

            /// <summary>
            ///     ブリッジの待機だけをキャンセルする。
            /// </summary>
            public void CancelWait()
            {
                // Task完了との競合に勝った側だけが、プール対象の完了ソースを一度だけ完了させる。
                if (Interlocked.CompareExchange(ref _isCompleted, 1, 0) == 0) { _completionSource.SetCanceled(); }
            }

            /// <summary>
            ///     待機中の場合だけ元Taskの完了状態を通知する。
            /// </summary>
            /// <param name="exception"> 元Taskから観測した例外。 </param>
            /// <param name="isCanceled"> 元Taskがキャンセルされた場合はtrue。 </param>
            public void Complete(Exception exception, bool isCanceled)
            {
                // キャンセルまたは別の完了通知が先行した場合は、完了ソースへ二重通知しない。
                if (Interlocked.CompareExchange(ref _isCompleted, 1, 0) != 0) { return; }

                // faultをキャンセルより優先し、どちらでもなければ正常完了として通知する。
                if (exception != null) { _completionSource.SetException(exception); }
                else if (isCanceled) { _completionSource.SetCanceled(); }
                else { _completionSource.SetResult(); }
            }

            #endregion

            #region 内部処理

            private readonly AwaitableCompletionSource _completionSource = new();
            private int _isCompleted;

            #endregion
        }

        /// <summary>
        ///     結果付きTaskとAwaitableCompletionSourceの競合を一度だけ解決する。
        /// </summary>
        /// <typeparam name="T"> Taskの結果型。 </typeparam>
        private sealed class TaskBridgeState<T>
        {
            #region 外部向けAPI

            /// <summary> ブリッジが所有する、一度だけ待機可能なAwaitable。 </summary>
            public Awaitable<T> Awaitable => _completionSource.Awaitable;

            /// <summary>
            ///     ブリッジの待機だけをキャンセルする。
            /// </summary>
            public void CancelWait()
            {
                // Task完了との競合に勝った側だけが、プール対象の完了ソースを一度だけ完了させる。
                if (Interlocked.CompareExchange(ref _isCompleted, 1, 0) == 0) { _completionSource.SetCanceled(); }
            }

            /// <summary>
            ///     待機中の場合だけ元Taskの完了状態と結果を通知する。
            /// </summary>
            /// <param name="exception"> 元Taskから観測した例外。 </param>
            /// <param name="isCanceled"> 元Taskがキャンセルされた場合はtrue。 </param>
            /// <param name="result"> 元Taskの完了結果。 </param>
            public void Complete(
                Exception exception,
                bool isCanceled,
                T result)
            {
                // キャンセルまたは別の完了通知が先行した場合は、完了ソースへ二重通知しない。
                if (Interlocked.CompareExchange(ref _isCompleted, 1, 0) != 0) { return; }

                // faultをキャンセルより優先し、どちらでもなければ結果付き正常完了として通知する。
                if (exception != null) { _completionSource.SetException(exception); }
                else if (isCanceled) { _completionSource.SetCanceled(); }
                else { _completionSource.SetResult(result); }
            }

            #endregion

            #region 内部処理

            private readonly AwaitableCompletionSource<T> _completionSource = new();
            private int _isCompleted;

            #endregion
        }

        #endregion
    }
}
