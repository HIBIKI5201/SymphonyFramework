using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SymphonyFrameWork.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ状態を管理する型
    /// </summary>
    public static class PauseManager
    {
        private static bool _pause;

        /// <summary> 現在のポーズ状態を取得または変更する。 </summary>
        public static bool Pause
        {
            get => _pause;
            set
            {
                _pause = value;
                OnPauseChanged?.Invoke(value);
            }
        }

        /// <summary> ポーズ状態とイベント購読を初期状態へ戻す。 </summary>
        internal static void Initialize()
        {
            _pause = false;
            OnPauseChanged = null;
        }

        /// <summary> ポーズ状態が変更されたときに新しい状態を通知する。 </summary>
        [Tooltip("ポーズ時にtrue、リズーム時にfalseで実行するイベント")]
        public static event Action<bool> OnPauseChanged;

        /// <summary>
        ///     ポーズ時に停止するNextFrameAsync
        /// </summary>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        public static async Task PausableNextFrameAsync(CancellationToken token = default)
        {
            //ポーズ中は終わるまで待機し続ける
            if (_pause) await Awaitable.NextFrameAsync(token);
            
            await Awaitable.NextFrameAsync(token);
        }

        /// <summary>
        ///     ポーズ時に停止するWaitForSecond
        /// </summary>
        /// <param name="time"> ポーズ時間を除いて待機する秒数。 </param>
        /// <returns> Unity Coroutineで実行するEnumerator。 </returns>
        public static IEnumerator PausableWaitForSecond(float time)
        {
            while (time > 0)
            {
                if (!_pause) time -= Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        ///     ポーズ時に停止するWaitForSecond
        /// </summary>
        /// <param name="time"> ポーズ時間を除いて待機する秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 待機処理を表すTask。 </returns>
        public static async Task PausableWaitForSecondAsync(float time, CancellationToken token = default)
        {
            while (time > 0)
            {
                if (!_pause) time -= Time.deltaTime;
                await Awaitable.NextFrameAsync(token);
            }
        }

        /// <summary>
        ///     ポーズ中は待機するWaitUntil
        /// </summary>
        /// <param name="action"> 待機終了条件を返す処理。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 条件成立までの待機処理を表すTask。 </returns>
        public static async Task PausableWaitUntil(Func<bool> action, CancellationToken token = default)
        {
            await SymphonyTask.WaitUntil(action, token);

            if (_pause) await Awaitable.NextFrameAsync(token);
        }

        /// <summary>
        ///     ポーズ中に停止するGameObjectのDestroy
        /// </summary>
        /// <param name="obj"> 待機後に破棄するGameObject。 </param>
        /// <param name="t"> ポーズ時間を除いて待機する秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        public static async void PausableDestroy(GameObject obj, float t, CancellationToken token = default)
        {
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
            await PausableWaitForSecondAsync(t, token);

            action?.Invoke();
        }

        /// <summary>
        ///     ポーズできるクラスに実装するインターフェース
        /// </summary>
        public interface IPausable
        {
            /// <summary>
            ///     ポーズのイベントを購買しているオブジェクトの一覧
            /// </summary>
            private static readonly Dictionary<IPausable, Action<bool>> PauseEventDictionary = new();

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
                if (PauseEventDictionary.ContainsKey(pausable)) return;

                Action<bool> pauseEvent = OnPauseEvent;

                PauseEventDictionary.Add(pausable, pauseEvent);

                OnPauseChanged += pauseEvent;

                void OnPauseEvent(bool paused)
                {
                    if (paused)
                        pausable.Pause();
                    else
                        pausable.Resume();
                }
            }

            /// <summary>
            ///     ポーズ時のイベントを購買解除する
            /// </summary>
            /// <param name="pausable"> ポーズ通知を解除する対象。 </param>
            static void UnregisterPauseManager(IPausable pausable)
            {
                if (PauseEventDictionary.TryGetValue(pausable, out var pauseEvent))
                {
                    OnPauseChanged -= pauseEvent;
                    PauseEventDictionary.Remove(pausable);
                }
            }
        }
    }
}
