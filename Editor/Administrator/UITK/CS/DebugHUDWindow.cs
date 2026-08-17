using System;

using SymphonyFrameWork.Debugger.HUD;
using SymphonyFrameWork.Utility;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary> Debug HUDの状態表示とShow / Hide操作を提供する。 </summary>
    [UxmlElement]
    public sealed partial class DebugHUDWindow : SymphonyVisualElement, IDisposable
    {
        #region 外部向けAPI

        /// <summary> 管理パネル用UXMLの非同期初期化を開始する。 </summary>
        public DebugHUDWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "DebugHUDWindow.uxml",
            InitializeTypeEnum.None,
            LoadTypeEnum.AssetDataBase)
        { }

        /// <summary> ViewModelとEditor callbackの購読を解除する。 </summary>
        public void Dispose()
        {
            if (_isDisposed) { return; }

            _isDisposed = true;
            EditorApplication.playModeStateChanged -= PlayModeStateChangedHandler;
            EditorApplication.delayCall -= DelayedBindViewModel;
            UnbindViewModel();
        }

        #endregion

        #region 内部処理

        private IDisposable _stateSubscription;
        private Label _statusText;
        private Label _registeredTextCount;
        private Button _showButton;
        private Button _hideButton;
        private bool _isDisposed;

        /// <inheritdoc />
        protected override Awaitable Initialize_S(VisualElement container)
        {
            if (_isDisposed) { return SymphonyAwaitable.Completed(); }

            SymphonyDocumentationGUI.BindOpenButton(container, SymphonyDocumentPageEnum.Debug);
            _statusText = container.Q<Label>("hud-status");
            _registeredTextCount = container.Q<Label>("hud-registered-texts");
            _showButton = container.Q<Button>("button-show");
            _hideButton = container.Q<Button>("button-hide");
            _showButton.clicked += Show;
            _hideButton.clicked += Hide;

            EditorApplication.playModeStateChanged += PlayModeStateChangedHandler;
            BindViewModel();
            return SymphonyAwaitable.Completed();
        }

        /// <summary> Play Mode遷移に合わせてViewModelへ接続し直す。 </summary>
        private void PlayModeStateChangedHandler(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.delayCall -= DelayedBindViewModel;
                    EditorApplication.delayCall += DelayedBindViewModel;
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.delayCall -= DelayedBindViewModel;
                    UnbindViewModel();
                    ApplyState(default, false);
                    break;
            }
        }

        /// <summary> Composition初期化後に現在のViewModelへ接続する。 </summary>
        private void DelayedBindViewModel()
        {
            EditorApplication.delayCall -= DelayedBindViewModel;
            BindViewModel();
        }

        /// <summary> 現在のDebug HUD ViewModelへ接続する。 </summary>
        private void BindViewModel()
        {
            UnbindViewModel();

            if (_isDisposed || !EditorApplication.isPlaying)
            {
                ApplyState(default, false);
                return;
            }

            DebugHUDViewModel viewModel = SymphonyDebugHUD.CurrentViewModel;
            if (viewModel == null)
            {
                ApplyState(default, false);
                return;
            }

            _stateSubscription = viewModel.State.Subscribe(state => ApplyState(state, true));
        }

        /// <summary> 現在のViewModel購読を解除する。 </summary>
        private void UnbindViewModel()
        {
            _stateSubscription?.Dispose();
            _stateSubscription = null;
        }

        /// <summary> 最新状態をパネルへ反映する。 </summary>
        private void ApplyState(DebugHUDDto state, bool isConnected)
        {
            if (_isDisposed || _statusText == null) { return; }

            _statusText.text = isConnected
                ? $"Initialized: {state.IsInitialized} / Available: {state.IsAvailable} / Visible: {state.IsVisible}"
                : "Initialized: - / Available: - / Visible: -";
            _registeredTextCount.text = isConnected
                ? $"Registered Texts: {state.RegisteredTextCount}"
                : "Registered Texts: -";

            bool canOperate = isConnected && state.IsInitialized && state.IsAvailable;
            _showButton.SetEnabled(canOperate && !state.IsVisible);
            _hideButton.SetEnabled(canOperate && state.IsVisible);
        }

        /// <summary> 利用可能な場合だけHUDを表示する。 </summary>
        private static void Show()
        {
            DebugHUDDto state = SymphonyDebugHUD.CurrentViewModel?.State.Value ?? default;
            if (EditorApplication.isPlaying && state.IsInitialized && state.IsAvailable)
            {
                SymphonyDebugHUD.Show();
            }
        }

        /// <summary> 利用可能な場合だけHUDを非表示にする。 </summary>
        private static void Hide()
        {
            DebugHUDDto state = SymphonyDebugHUD.CurrentViewModel?.State.Value ?? default;
            if (EditorApplication.isPlaying && state.IsInitialized && state.IsAvailable)
            {
                SymphonyDebugHUD.Hide();
            }
        }

        #endregion
    }
}
