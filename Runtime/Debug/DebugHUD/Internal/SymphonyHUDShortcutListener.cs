using System;

using SymphonyFrameWork.Config;

using UnityEngine;
using UnityEngine.InputSystem;

namespace SymphonyFrameWork.Debugger.HUD
{
    /// <summary>
    ///     Debug HUD用Input Actionの購読と解放をUnityライフサイクルへ同期する。
    /// </summary>
    internal sealed class SymphonyHUDShortcutListener : MonoBehaviour
    {
        #region 外部向けAPI

        /// <summary>
        ///     設定Actionを複製し、HUDの表示切り替え処理へ接続する。
        /// </summary>
        /// <param name="toggleAction"> Project Settingsで設定されたInput Action。 </param>
        /// <param name="toggle"> Action発火時に実行する表示切り替え処理。 </param>
        internal void Configure(InputAction toggleAction, Action toggle)
        {
            if (toggle == null) { throw new ArgumentNullException(nameof(toggle)); }

            ReleaseAction();

            // ConfigアセットのActionを直接有効化せず、Listenerが所有する複製へライフサイクルを限定する。
            _toggleAction = toggleAction != null && toggleAction.bindings.Count > 0
                ? toggleAction.Clone()
                : DebugHUDConfig.CreateDefaultToggleAction();
            _toggle = toggle;

            if (isActiveAndEnabled) { Subscribe(); }
        }

        /// <summary>
        ///     所有するActionを停止して解放する。
        /// </summary>
        internal void Shutdown() => ReleaseAction();

        #endregion

        #region 内部処理

        private Action _toggle;
        private InputAction _toggleAction;
        private bool _isSubscribed;

        /// <summary> Componentが有効になった時点で入力監視を開始する。 </summary>
        private void OnEnable() => Subscribe();

        /// <summary> Componentが無効になった時点で入力監視を解除する。 </summary>
        private void OnDisable() => Unsubscribe();

        /// <summary> GameObject破棄時に所有Actionを解放する。 </summary>
        private void OnDestroy() => ReleaseAction();

        /// <summary> Input Actionの購読と有効化を一度だけ行う。 </summary>
        private void Subscribe()
        {
            if (_toggleAction == null || _isSubscribed) { return; }

            _toggleAction.performed += OnTogglePerformed;
            _toggleAction.Enable();
            _isSubscribed = true;
        }

        /// <summary> Input Actionの購読を解除して無効化する。 </summary>
        private void Unsubscribe()
        {
            if (_toggleAction == null || !_isSubscribed) { return; }

            _toggleAction.Disable();
            _toggleAction.performed -= OnTogglePerformed;
            _isSubscribed = false;
        }

        /// <summary> Input Actionの発火をHUDの表示切り替えへ転送する。 </summary>
        /// <param name="context"> 発火したInput Actionの情報。 </param>
        private void OnTogglePerformed(InputAction.CallbackContext context) => _toggle?.Invoke();

        /// <summary> 購読を解除し、Listenerが所有するActionを破棄する。 </summary>
        private void ReleaseAction()
        {
            Unsubscribe();
            _toggleAction?.Dispose();
            _toggleAction = null;
            _toggle = null;
        }

        #endregion
    }
}
