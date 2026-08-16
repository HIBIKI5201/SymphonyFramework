using System;

using SymphonyFrameWork.Debugger.Logger;

using UnityEngine;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ状態を変更し、登録された対象へ通知する。
    /// </summary>
    /// <remarks> 状態は<see cref="PauseStateEntity"/>、購読は<see cref="PausableRegistry"/>へ委譲する。 </remarks>
    internal sealed class PauseService
    {
        #region 外部向けAPI

        /// <summary>
        ///     状態と購読の保持先を指定して生成する。
        /// </summary>
        /// <param name="state"> ポーズ状態を保持するEntity。 </param>
        /// <param name="registry"> ポーズ通知の購読を所有するレジストリ。 </param>
        public PauseService(PauseStateEntity state, PausableRegistry registry)
        {
            // 状態と購読のどちらが欠けても通知の整合を保てないため、生成時に拒否する。
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary> ポーズ状態が変化したときに新しい状態を通知する。 </summary>
        /// <remarks> ゲームロジックの購読者例外はそのまま伝播する。 </remarks>
        public event Action<bool> OnPauseChanged;

        /// <summary> ポーズ状態または購読件数が変化したときに通知する。 </summary>
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
            // 同じ状態の再設定ではゲームロジックと表示へ重複通知しない。
            if (!_state.SetPaused(isPaused)) { return; }

            // 表示状態を先に同期し、確定したポーズ状態をゲームロジックへ通知する。
            RaiseStateChanged();
            OnPauseChanged?.Invoke(isPaused);
        }

        /// <summary>
        ///     ポーズ通知の購読者を追加する。
        /// </summary>
        /// <param name="handler"> 追加する処理。 </param>
        public void AddPauseChangedHandler(Action<bool> handler)
        {
            OnPauseChanged += handler;
        }

        /// <summary>
        ///     ポーズ通知の購読者を除去する。
        /// </summary>
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
            // 通知先を持たない購読は登録できないため、呼び出し元の誤りとして拒否する。
            if (pausable == null) { throw new ArgumentNullException(nameof(pausable)); }

            void PauseEventHandler(bool paused)
            {
                // 新しい状態に応じて、対象の停止と再開のどちらか一方だけを通知する。
                if (paused) { pausable.Pause(); }
                else { pausable.Resume(); }
            }

            // 同じ対象の二重購読はPauseとResumeを重複実行するため追加しない。
            if (!_registry.TryRegister(pausable, PauseEventHandler)) { return; }

            // Registryへ保持した同一Delegateをeventへ登録し、購読件数の変化を表示へ通知する。
            OnPauseChanged += PauseEventHandler;
            RaiseStateChanged();
        }

        /// <summary>
        ///     ポーズ通知を受け取る対象の登録を解除する。未登録の場合は何もしない。
        /// </summary>
        /// <param name="pausable"> ポーズ通知を解除する対象。 </param>
        public void Unregister(PauseManager.IPausable pausable)
        {
            // 通知先を特定できない解除要求は、呼び出し元の誤りとして拒否する。
            if (pausable == null) { throw new ArgumentNullException(nameof(pausable)); }

            // 未登録ならeventから除去すべきDelegateも存在しないため何もしない。
            if (!_registry.TryUnregister(pausable, out Action<bool> pauseEvent)) { return; }

            // 登録時と同一のDelegateをeventから外し、購読件数の変化を表示へ通知する。
            OnPauseChanged -= pauseEvent;
            RaiseStateChanged();
        }

        /// <summary>
        ///     ポーズ状態と購読を消去する。
        /// </summary>
        public void Reset()
        {
            // Domain Reloadなしの再初期化へ前回の状態やゲームロジックの購読を残さない。
            _state.Reset();
            _registry.Clear();
            OnPauseChanged = null;
            RaiseStateChanged();
        }

        #endregion

        #region 内部処理

        private readonly PauseStateEntity _state;
        private readonly PausableRegistry _registry;

        /// <summary>
        ///     表示向けの状態変更を通知する。
        /// </summary>
        /// <remarks> 表示専用ViewModelの失敗をゲーム側のポーズ処理へ伝播させない。 </remarks>
        private void RaiseStateChanged()
        {
            try
            {
                // 表示をポーズ状態と購読件数の確定後に同期する。
                OnStateChanged?.Invoke();
            }
            catch (Exception exception)
            {
                // 表示側の失敗はゲームロジックのポーズ処理へ逆流させず、診断ログだけを残す。
                SymphonyDebugLogger.LogException(exception);
            }
        }

        #endregion
    }
}
