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
    ///     ポーズ状態の操作とポーズ対応の待機APIを提供する。
    /// </summary>
    /// <remarks> 引数を検証し、状態操作を<see cref="PauseService"/>へ委譲する。 </remarks>
    public static class PauseManager
    {
        #region 外部向けAPI

        /// <summary> ポーズ状態が変更されたときに新しい状態を通知する。 </summary>
        /// <remarks> 同じ値の再設定では発行しない。 </remarks>
        /// <exception cref="SymphonyNotInitializedException"> 初期化前に呼び出した場合。 </exception>
        public static event Action<bool> OnPauseChanged
        {
            add => EnsureInitialized().AddPauseChangedHandler(value);
            remove => EnsureInitialized().RemovePauseChangedHandler(value);
        }

        /// <summary> 現在のポーズ状態。 </summary>
        /// <remarks> 現在と同じ値を設定した場合は<see cref="OnPauseChanged"/>を発行しない。 </remarks>
        /// <exception cref="SymphonyNotInitializedException"> 初期化前に呼び出した場合。 </exception>
        public static bool Pause
        {
            get => EnsureInitialized().IsPaused;
            set => EnsureInitialized().SetPaused(value);
        }

        /// <summary>
        ///     ポーズ機構の管理状態を取得する。
        /// </summary>
        /// <returns> 現在のポーズ状態と購読件数。 </returns>
        /// <exception cref="SymphonyNotInitializedException"> 初期化前に呼び出した場合。 </exception>
        public static PauseInfo GetPauseInfo()
        {
            return EnsureQuery().GetInfo();
        }

        /// <summary>
        ///     ポーズ状態を考慮して次のフレームまで待機する。
        /// </summary>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        public static async Awaitable PausableNextFrameAsync(CancellationToken token = default)
        {
            PauseQuery query = EnsureQuery();

            // 呼び出し時点でポーズ中なら待機を1フレーム延長し、フレーム処理の進行を遅らせる。
            if (query.IsPaused) { await Awaitable.NextFrameAsync(token); }

            // 非ポーズ時にも必ず次のPlayerLoopまで制御を戻す。
            await Awaitable.NextFrameAsync(token);
        }

        /// <summary>
        ///     ポーズ時間を除外して指定秒数を待機する。
        /// </summary>
        /// <param name="time"> ポーズ時間を除いて待機する秒数。 </param>
        /// <returns> Unity Coroutineで実行するEnumerator。 </returns>
        public static IEnumerator PausableWaitForSecond(float time)
        {
            PauseQuery query = EnsureQuery();
            ValidateDuration(time, nameof(time));

            // ポーズ中のdeltaTimeを残り時間へ反映せず、待機時間とTween相当の進行を停止状態へ追従させる。
            while (time > 0)
            {
                if (!query.IsPaused) { time -= Time.deltaTime; }
                yield return null;
            }
        }

        /// <summary>
        ///     ポーズ時間を除外して指定秒数を非同期に待機する。
        /// </summary>
        /// <param name="time"> ポーズ時間を除いて待機する秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 待機処理を表すAwaitable。 </returns>
        public static async Awaitable PausableWaitForSecondAsync(float time, CancellationToken token = default)
        {
            PauseQuery query = EnsureQuery();
            ValidateDuration(time, nameof(time));

            // ポーズ中のdeltaTimeを残り時間へ反映せず、待機の進行をポーズ状態へ追従させる。
            while (time > 0)
            {
                if (!query.IsPaused) { time -= Time.deltaTime; }
                await Awaitable.NextFrameAsync(token);
            }
        }

        /// <summary>
        ///     条件が成立するまで非同期に待機する。
        /// </summary>
        /// <param name="action"> 待機終了条件を返す処理。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 条件成立までの待機処理を表すAwaitable。 </returns>
        public static async Awaitable PausableWaitUntil(Func<bool> action, CancellationToken token = default)
        {
            PauseQuery query = EnsureQuery();

            // 終了条件を評価できない待機は開始させない。
            if (action == null) { throw new ArgumentNullException(nameof(action)); }

            // 条件は毎フレーム観測し、Tweenなど外部で進む処理の完了へ追従する。
            await SymphonyAwaitable.WaitWhile(() => !action.Invoke(), token);

            // 条件成立時点がポーズ中なら、後続処理を同じフレームで再開させない。
            if (query.IsPaused) { await Awaitable.NextFrameAsync(token); }
        }

        /// <summary>
        ///     ポーズ時間を除外して待機した後にGameObjectを破棄する。
        /// </summary>
        /// <remarks> 戻り値を待機しない場合も破棄処理は進行する。 </remarks>
        /// <param name="obj"> 待機後に破棄するGameObject。 </param>
        /// <param name="t"> ポーズ時間を除いて待機する秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 破棄までの待機を表すAwaitable。 </returns>
        /// <exception cref="ArgumentNullException"> objがnullの場合。 </exception>
        /// <exception cref="ArgumentOutOfRangeException"> tが負の場合。 </exception>
        public static Awaitable PausableDestroy(GameObject obj, float t, CancellationToken token = default)
        {
            // 検証は同期部分で行い、呼び出し元のtry/catchへ届くようにする。
            EnsureInitialized();

            // 待機完了後に破棄対象を失わないよう、開始前にnullを拒否する。
            if (obj == null) { throw new ArgumentNullException(nameof(obj)); }

            ValidateDuration(t, nameof(t));

            // 遅延本体を分離し、引数検証だけはAwaitableを返す前に同期的に完了させる。
            return DestroyAfterDelayAsync(obj, t, token);
        }

        /// <summary>
        ///     ポーズ時間を除外して待機した後に処理を実行する。
        /// </summary>
        /// <remarks> 戻り値を待機しない場合も指定処理は進行する。 </remarks>
        /// <param name="action"> 待機後に実行する処理。 </param>
        /// <param name="t"> ポーズ時間を除いて待機する秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 実行までの待機を表すAwaitable。 </returns>
        /// <exception cref="ArgumentNullException"> actionがnullの場合。 </exception>
        /// <exception cref="ArgumentOutOfRangeException"> tが負の場合。 </exception>
        public static Awaitable PausableInvoke(Action action, float t, CancellationToken token = default)
        {
            // 検証は同期部分で行い、呼び出し元のtry/catchへ届くようにする。
            EnsureInitialized();

            // 待機完了後に実行対象を失わないよう、開始前にnullを拒否する。
            if (action == null) { throw new ArgumentNullException(nameof(action)); }

            ValidateDuration(t, nameof(t));

            // 遅延本体を分離し、引数検証だけはAwaitableを返す前に同期的に完了させる。
            return InvokeAfterDelayAsync(action, t, token);
        }

        /// <summary>
        ///     ポーズ状態の変更へ追従する対象を表す。
        /// </summary>
        public interface IPausable
        {
            #region 外部向けAPI

            /// <summary>
            ///     ポーズ開始時の処理を実行する。
            /// </summary>
            void Pause();

            /// <summary>
            ///     ポーズ解除時の処理を実行する。
            /// </summary>
            void Resume();

            /// <summary>
            ///     ポーズ状態の変更通知へ登録する。
            /// </summary>
            /// <param name="pausable"> ポーズ通知を受け取る対象。 </param>
            static void RegisterPauseManager(IPausable pausable)
            {
                // PauseとResumeを同じ対象へ通知できるよう、Serviceへ購読を委譲する。
                EnsureInitialized().Register(pausable);
            }

            /// <summary>
            ///     ポーズ状態の変更通知から解除する。
            /// </summary>
            /// <param name="pausable"> ポーズ通知を解除する対象。 </param>
            static void UnregisterPauseManager(IPausable pausable)
            {
                // 登録時の通知処理を特定して除去できるServiceへ解除を委譲する。
                EnsureInitialized().Unregister(pausable);
            }

            #endregion
        }

        #endregion

        #region 内部処理

        private static PauseService _service;
        private static PauseQuery _query;
        private static PauseViewModel _viewModel;

        /// <summary>
        ///     待機後にGameObjectを破棄する。
        /// </summary>
        /// <param name="obj"> 破棄するGameObject。 </param>
        /// <param name="durationSeconds"> ポーズ時間を除いて待機する秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 破棄までの待機を表すAwaitable。 </returns>
        private static async Awaitable DestroyAfterDelayAsync(
            GameObject obj,
            float durationSeconds,
            CancellationToken token)
        {
            // ポーズ中の経過時間を除外し、解除後に残り時間から破棄待機を再開する。
            await PausableWaitForSecondAsync(durationSeconds, token);

            Object.Destroy(obj);
        }

        /// <summary>
        ///     待機後に処理を実行する。
        /// </summary>
        /// <param name="action"> 実行する処理。 </param>
        /// <param name="durationSeconds"> ポーズ時間を除いて待機する秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 実行までの待機を表すAwaitable。 </returns>
        private static async Awaitable InvokeAfterDelayAsync(
            Action action,
            float durationSeconds,
            CancellationToken token)
        {
            // ポーズ中の経過時間を除外し、解除後に残り時間から実行待機を再開する。
            await PausableWaitForSecondAsync(durationSeconds, token);

            action.Invoke();
        }

        /// <summary>
        ///     Compositionから内部レイヤーが結合済みであることを確認する。
        /// </summary>
        /// <returns> 処理を委譲するService。 </returns>
        private static PauseService EnsureInitialized()
        {
            return _service ?? throw new SymphonyNotInitializedException(typeof(PauseManager));
        }

        /// <summary>
        ///     Compositionから読み取り層が結合済みであることを確認する。
        /// </summary>
        /// <returns> 状態を読み取るQuery。 </returns>
        private static PauseQuery EnsureQuery()
        {
            return _query ?? throw new SymphonyNotInitializedException(typeof(PauseManager));
        }

        /// <summary>
        ///     待機時間が0以上か検証する。
        /// </summary>
        /// <param name="durationSeconds"> 検証する待機時間。 </param>
        /// <param name="parameterName"> 公開APIで使用されている引数名。 </param>
        private static void ValidateDuration(float durationSeconds, string parameterName)
        {
            // 負の待機時間はループを通らず即時完了するため、呼び出し誤りとして明示する。
            if (durationSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    durationSeconds,
                    "待機時間は0秒以上で指定してください。");
            }
        }

        /// <summary> Pause Managerが初期化済みかどうか。 </summary>
        internal static bool IsInitialized => _service != null;

        /// <summary> 状態表示に接続するためのViewModel。未初期化の場合はnull。 </summary>
        internal static PauseViewModel CurrentViewModel => _viewModel;

        /// <summary>
        ///     ポーズ状態とイベント購読を初期状態へ戻し、内部レイヤーを結合する。
        /// </summary>
        internal static void Initialize()
        {
            // Domain Reloadなしで再初期化されても、前回の状態と購読を残さない。
            ResetRuntimeState();

            // 状態と購読を共有するService、Query、ViewModelを同じCompositionで結合する。
            PauseStateEntity state = new();
            PausableRegistry registry = new();
            _service = new PauseService(state, registry);
            _query = new PauseQuery(state, registry);
            _viewModel = new PauseViewModel(_query, _service);
        }

        /// <summary>
        ///     ポーズ状態とイベント購読を消去して未初期化状態へ戻す。
        /// </summary>
        internal static void ResetRuntimeState()
        {
            // 表示側を通知元から切り離してから、ゲームロジックの購読と状態を消去する。
            _viewModel?.Dispose();
            _service?.Reset();

            // Domain Reloadなしの次回初期化で古い内部層を再利用しない。
            _viewModel = null;
            _query = null;
            _service = null;
        }

        #endregion
    }
}
