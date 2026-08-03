using System;
using System.Collections;
using System.Threading;

using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.Utility;

using UnityEngine;
using Object = UnityEngine.Object;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ状態を管理する型。
    ///     引数の検証と<see cref="PauseService"/>への転送、およびポーズ中に進行しない
    ///     待機ユーティリティを提供します。状態は保持しません。
    /// </summary>
    public static class PauseManager
    {
        /// <summary>
        ///     現在のポーズ状態を取得または変更する。
        ///     現在と同じ値を設定した場合、<see cref="OnPauseChanged"/>は発行されません。
        /// </summary>
        /// <exception cref="SymphonyNotInitializedException"> 初期化前に呼び出した場合。 </exception>
        public static bool Pause
        {
            get => EnsureInitialized().IsPaused;
            set => EnsureInitialized().SetPaused(value);
        }

        /// <summary>
        ///     ポーズ状態が変更されたときに新しい状態を通知する。
        ///     同じ値の再設定では発行されません。
        /// </summary>
        /// <exception cref="SymphonyNotInitializedException"> 初期化前に呼び出した場合。 </exception>
        public static event Action<bool> OnPauseChanged
        {
            add => EnsureInitialized().AddPauseChangedHandler(value);
            remove => EnsureInitialized().RemovePauseChangedHandler(value);
        }

        /// <summary> ポーズ機構の管理状態を取得時点の不変値として取得する。 </summary>
        /// <returns> 現在のポーズ状態と購読件数。 </returns>
        /// <exception cref="SymphonyNotInitializedException"> 初期化前に呼び出した場合。 </exception>
        public static PauseInfo GetPauseInfo()
        {
            return EnsureQuery().GetInfo();
        }

        /// <summary> Pause Managerが初期化済みかどうか。 </summary>
        internal static bool IsInitialized => _service != null;

        /// <summary> 状態表示に接続するためのViewModel。未初期化の場合はnull。 </summary>
        internal static PauseViewModel CurrentViewModel => _viewModel;

        /// <summary> ポーズ状態とイベント購読を初期状態へ戻し、内部レイヤーを結合する。 </summary>
        internal static void Initialize()
        {
            ResetRuntimeState();

            var state = new PauseStateEntity();
            var registry = new PausableRegistry();
            _service = new PauseService(state, registry);
            _query = new PauseQuery(state, registry);
            _viewModel = new PauseViewModel(_query, _service);
        }

        /// <summary> ポーズ状態とイベント購読を消去して未初期化状態へ戻す。 </summary>
        internal static void ResetRuntimeState()
        {
            _viewModel?.Dispose();
            _service?.Reset();

            _viewModel = null;
            _query = null;
            _service = null;
        }

        /// <summary>
        ///     ポーズ時に停止するNextFrameAsync
        /// </summary>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        public static async Awaitable PausableNextFrameAsync(CancellationToken token = default)
        {
            PauseQuery query = EnsureQuery();

            //ポーズ中は終わるまで待機し続ける
            if (query.IsPaused) await Awaitable.NextFrameAsync(token);

            await Awaitable.NextFrameAsync(token);
        }

        /// <summary>
        ///     ポーズ時に停止するWaitForSecond
        /// </summary>
        /// <param name="time"> ポーズ時間を除いて待機する秒数。 </param>
        /// <returns> Unity Coroutineで実行するEnumerator。 </returns>
        public static IEnumerator PausableWaitForSecond(float time)
        {
            PauseQuery query = EnsureQuery();
            ValidateDuration(time, nameof(time));

            while (time > 0)
            {
                if (!query.IsPaused) time -= Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        ///     ポーズ時に停止するWaitForSecond
        /// </summary>
        /// <param name="time"> ポーズ時間を除いて待機する秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 待機処理を表すAwaitable。 </returns>
        public static async Awaitable PausableWaitForSecondAsync(float time, CancellationToken token = default)
        {
            PauseQuery query = EnsureQuery();
            ValidateDuration(time, nameof(time));

            while (time > 0)
            {
                if (!query.IsPaused) time -= Time.deltaTime;
                await Awaitable.NextFrameAsync(token);
            }
        }

        /// <summary>
        ///     ポーズ中は待機するWaitUntil
        /// </summary>
        /// <param name="action"> 待機終了条件を返す処理。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 条件成立までの待機処理を表すAwaitable。 </returns>
        public static async Awaitable PausableWaitUntil(Func<bool> action, CancellationToken token = default)
        {
            PauseQuery query = EnsureQuery();

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            await SymphonyAwaitable.WaitWhile(() => !action.Invoke(), token);

            if (query.IsPaused) await Awaitable.NextFrameAsync(token);
        }

        /// <summary>
        ///     ポーズ中に停止するGameObjectのDestroy
        /// </summary>
        /// <param name="obj"> 待機後に破棄するGameObject。 </param>
        /// <param name="t"> ポーズ時間を除いて待機する秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        public static async void PausableDestroy(GameObject obj, float t, CancellationToken token = default)
        {
            EnsureInitialized();

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            ValidateDuration(t, nameof(t));
            await PausableWaitForSecondAsync(t, token);

            Object.Destroy(obj);
        }

        /// <summary>
        ///     ポーズ中に停止するInvoke
        /// </summary>
        /// <param name="action"> 待機後に実行する処理。 </param>
        /// <param name="t"> ポーズ時間を除いて待機する秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        public static async void PausableInvoke(Action action, float t, CancellationToken token = default)
        {
            EnsureInitialized();

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            ValidateDuration(t, nameof(t));
            await PausableWaitForSecondAsync(t, token);

            action?.Invoke();
        }

        /// <summary>
        ///     ポーズできるクラスに実装するインターフェース
        /// </summary>
        public interface IPausable
        {
            /// <summary>
            ///     ポーズ時に呼び出されるイベント
            /// </summary>
            void Pause();

            /// <summary>
            ///     リズーム時に呼び出されるイベント
            /// </summary>
            void Resume();

            /// <summary>
            ///     PauseManagerにポーズ時のイベントを購買登録する
            /// </summary>
            /// <param name="pausable"> ポーズ通知を受け取る対象。 </param>
            static void RegisterPauseManager(IPausable pausable)
            {
                EnsureInitialized().Register(pausable);
            }

            /// <summary>
            ///     ポーズ時のイベントを購買解除する
            /// </summary>
            /// <param name="pausable"> ポーズ通知を解除する対象。 </param>
            static void UnregisterPauseManager(IPausable pausable)
            {
                EnsureInitialized().Unregister(pausable);
            }
        }

        /// <summary> Compositionから内部レイヤーが結合済みであることを確認する。 </summary>
        /// <returns> 処理を委譲するService。 </returns>
        private static PauseService EnsureInitialized()
        {
            return _service ?? throw new SymphonyNotInitializedException(typeof(PauseManager));
        }

        /// <summary> Compositionから内部レイヤーが結合済みであることを確認する。 </summary>
        /// <returns> 状態を読み取るQuery。 </returns>
        private static PauseQuery EnsureQuery()
        {
            return _query ?? throw new SymphonyNotInitializedException(typeof(PauseManager));
        }

        /// <summary> 待機時間が0以上か検証する。 </summary>
        /// <param name="durationSeconds"> 検証する待機時間。 </param>
        /// <param name="parameterName"> 公開APIで使用されている引数名。 </param>
        private static void ValidateDuration(float durationSeconds, string parameterName)
        {
            if (durationSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    durationSeconds,
                    "待機時間は0秒以上で指定してください。");
            }
        }

        private static PauseService _service;
        private static PauseQuery _query;
        private static PauseViewModel _viewModel;
    }
}
