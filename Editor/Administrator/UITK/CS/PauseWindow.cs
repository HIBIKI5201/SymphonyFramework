using System;
using System.Threading.Tasks;

using SymphonyFrameWork.System;
using SymphonyFrameWork.Utility;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary> PauseManagerの状態表示と操作を提供する管理パネル。 </summary>
    [UxmlElement]
    public sealed partial class PauseWindow : SymphonyVisualElement, IDisposable
    {
        /// <summary> 管理パネル用UXMLの非同期初期化を開始する。 </summary>
        public PauseWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "PauseWindow.uxml",
            InitializeType.None,
            LoadType.AssetDataBase)
        { }

        /// <summary> ViewModelとEditor callbackの購読を解除する。 </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            EditorApplication.playModeStateChanged -= PlayModeStateChangedHandler;
            EditorApplication.delayCall -= DelayedBindViewModel;
            UnbindViewModel();
        }

        /// <summary> PauseManagerへの操作ボタンと状態表示要素を構成する。 </summary>
        /// <param name="container"> UXMLから生成されたルート要素。 </param>
        /// <returns> 同期的に完了する初期化処理。 </returns>
        protected override ValueTask Initialize_S(VisualElement container)
        {
            if (_isDisposed)
            {
                return default;
            }

            _pauseVisual = container.Q<VisualElement>("pause");
            _pauseText = container.Q<Label>("pause-text");
            _subscriberText = container.Q<Label>("pausable-subscribers");

            _pauseButton = container.Q<Button>("button-pause");
            _resumeButton = container.Q<Button>("button-resume");
            _pauseButton.clicked += () => SetPause(true);
            _resumeButton.clicked += () => SetPause(false);

            EditorApplication.playModeStateChanged += PlayModeStateChangedHandler;
            BindViewModel();

            return default;
        }

        /// <summary> Play Mode遷移に合わせて現在のViewModelへ接続し直す。 </summary>
        /// <param name="state"> 遷移後のPlay Mode状態。 </param>
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

        /// <summary> Pause Manager初期化後のEditor callbackでViewModelへ接続する。 </summary>
        private void DelayedBindViewModel()
        {
            EditorApplication.delayCall -= DelayedBindViewModel;
            BindViewModel();
        }

        /// <summary> 現在のPause ViewModelへ接続する。 </summary>
        private void BindViewModel()
        {
            UnbindViewModel();

            if (_isDisposed
                || !EditorApplication.isPlaying
                || !PauseManager.IsInitialized)
            {
                ApplyState(default, false);
                return;
            }

            PauseViewModel viewModel = PauseManager.CurrentViewModel;
            if (viewModel == null)
            {
                ApplyState(default, false);
                return;
            }

            _stateSubscription = viewModel.State.Subscribe(pauseDto => ApplyState(pauseDto, true));
        }

        /// <summary> 現在のViewModel購読を解除する。 </summary>
        private void UnbindViewModel()
        {
            _stateSubscription?.Dispose();
            _stateSubscription = null;
        }

        /// <summary>
        ///     最新の表示値を管理パネルへ反映する。
        /// </summary>
        /// <param name="pauseDto"> ViewModelが公開した表示値。 </param>
        /// <param name="isConnected"> Pause Managerへ接続できているかどうか。 </param>
        private void ApplyState(PauseDto pauseDto, bool isConnected)
        {
            if (_isDisposed || _pauseVisual == null)
            {
                return;
            }

            _pauseVisual.style.backgroundColor = isConnected
                ? new StyleColor(pauseDto.IsPaused ? Color.green : Color.red)
                : new StyleColor(new Color32(56, 56, 56, 255));

            _pauseText.text = isConnected ? pauseDto.IsPaused.ToString() : "-";
            _subscriberText.text = isConnected
                ? $"IPausable: {pauseDto.PausableSubscriberCount}"
                : "IPausable: -";

            // Play Mode外ではPauseManagerが未初期化であり、操作すると例外になる。
            _pauseButton.SetEnabled(isConnected);
            _resumeButton.SetEnabled(isConnected);
        }

        /// <summary> ポーズ状態を切り替える。未初期化での操作は無視する。 </summary>
        /// <param name="isPaused"> 設定するポーズ状態。 </param>
        private static void SetPause(bool isPaused)
        {
            if (!EditorApplication.isPlaying || !PauseManager.IsInitialized)
            {
                return;
            }

            PauseManager.Pause = isPaused;
        }

        private IDisposable _stateSubscription;
        private Label _pauseText;
        private Label _subscriberText;
        private VisualElement _pauseVisual;
        private Button _pauseButton;
        private Button _resumeButton;
        private bool _isDisposed;
    }
}
