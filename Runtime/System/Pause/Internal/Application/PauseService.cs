using System;

using UnityEngine;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ状態の変更と、<see cref="PauseManager.IPausable"/>への通知を担当する。
    ///     状態の保持は<see cref="PauseStateEntity"/>、購読の管理は
    ///     <see cref="PausableRegistry"/>へ委譲する。
    /// </summary>
    internal sealed class PauseService
    {
        /// <summary>
        ///     状態と購読の保持先を指定して生成する。
        /// </summary>
        /// <param name="state"> ポーズ状態を保持するEntity。 </param>
        /// <param name="registry"> ポーズ通知の購読を所有するレジストリ。 </param>
        public PauseService(PauseStateEntity state, PausableRegistry registry)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        ///     ポーズ状態が変化したときに新しい状態を通知する。
        ///     これは利用側のゲームロジックが購読する公開eventの実体であり、
        ///     **購読者の例外は握らずそのまま伝播させる。**
        /// </summary>
        public event Action<bool> OnPauseChanged;

        /// <summary>
        ///     表示内容が変わりうる変更を通知する。ポーズ状態と購読件数の変化で発行する。
        /// </summary>
        public event Action OnStateChanged;

        /// <summary> 現在ポーズ中かどうか。 </summary>
        public bool IsPaused => _state.IsPaused;

        /// <summary> ポーズ通知を購読している対象の件数。 </summary>
        public int PausableSubscriberCount => _registry.Count;

        /// <summary>
        ///     ポーズ状態を設定する。状態が変化したときだけ通知する。
        /// </summary>
        /// <param name="isPaused"> 設定するポーズ状態。 </param>
        public void SetPaused(bool isPaused)
        {
            if (!_state.SetPaused(isPaused))
            {
                return;
            }

            RaiseStateChanged();
            OnPauseChanged?.Invoke(isPaused);
        }

        /// <summary> ポーズ通知の購読者を追加する。 </summary>
        /// <param name="handler"> 追加する処理。 </param>
        public void AddPauseChangedHandler(Action<bool> handler)
        {
            OnPauseChanged += handler;
        }

        /// <summary> ポーズ通知の購読者を除去する。 </summary>
        /// <param name="handler"> 除去する処理。 </param>
        public void RemovePauseChangedHandler(Action<bool> handler)
        {
            OnPauseChanged -= handler;
        }

        /// <summary>
        ///     ポーズ通知を受け取る対象を登録する。登録済みの場合は何もしない。
        /// </summary>
        /// <param name="pausable"> ポーズ通知を受け取る対象。 </param>
        public void Register(PauseManager.IPausable pausable)
        {
            if (pausable == null)
            {
                throw new ArgumentNullException(nameof(pausable));
            }

            void PauseEventHandler(bool paused)
            {
                if (paused)
                {
                    pausable.Pause();
                }
                else
                {
                    pausable.Resume();
                }
            }

            if (!_registry.TryRegister(pausable, PauseEventHandler))
            {
                return;
            }

            OnPauseChanged += PauseEventHandler;
            RaiseStateChanged();
        }

        /// <summary>
        ///     ポーズ通知を受け取る対象の登録を解除する。未登録の場合は何もしない。
        /// </summary>
        /// <param name="pausable"> ポーズ通知を解除する対象。 </param>
        public void Unregister(PauseManager.IPausable pausable)
        {
            if (pausable == null)
            {
                throw new ArgumentNullException(nameof(pausable));
            }

            if (!_registry.TryUnregister(pausable, out Action<bool> pauseEvent))
            {
                return;
            }

            OnPauseChanged -= pauseEvent;
            RaiseStateChanged();
        }

        /// <summary> ポーズ状態と購読を消去する。 </summary>
        public void Reset()
        {
            _state.Reset();
            _registry.Clear();
            OnPauseChanged = null;
            RaiseStateChanged();
        }

        /// <summary>
        ///     表示向けの状態変更を通知する。購読側の例外はここで止める。
        ///     購読しているのは表示専用のViewModelであり、その失敗をゲーム側の
        ///     ポーズ処理の失敗にしない。
        /// </summary>
        private void RaiseStateChanged()
        {
            try
            {
                OnStateChanged?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private readonly PauseStateEntity _state;
        private readonly PausableRegistry _registry;
    }
}
