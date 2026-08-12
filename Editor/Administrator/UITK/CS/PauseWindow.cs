using System;
using System.Threading.Tasks;

using SymphonyFrameWork.System;
using SymphonyFrameWork.Utility;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     PauseManagerの状態表示と操作を提供する。
    /// </summary>
    [UxmlElement]
    public sealed partial class PauseWindow : SymphonyVisualElement, IDisposable
    {
        #region 外部向けAPI

        /// <summary>
        ///     管理パネル用UXMLの非同期初期化を開始する。
        /// </summary>
        /// <remarks>
        ///     UXMLの基準パスはパッケージ導入とAssets直置きの双方を解決する。
        /// </remarks>
        public PauseWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "PauseWindow.uxml",
            InitializeTypeEnum.None,
            LoadTypeEnum.AssetDataBase)
        { }

        /// <summary>
        ///     ViewModelとEditor callbackの購読を解除する。
        /// </summary>
        public void Dispose()
        {
            // 破棄済みの場合は、同じ購読を重複して解除しない。
            if (_isDisposed) { return; }

            // Windowより長生きするEditor callbackとReactivePropertyの購読をすべて解除する。
            _isDisposed = true;
            EditorApplication.playModeStateChanged -= PlayModeStateChangedHandler;
            EditorApplication.delayCall -= DelayedBindViewModel;
            UnbindViewModel();
        }

        #endregion

        #region 内部処理

        private IDisposable _stateSubscription;
        private Label _pauseText;
        private Label _subscriberText;
        private VisualElement _pauseVisual;
        private Button _pauseButton;
        private Button _resumeButton;
        private bool _isDisposed;

        /// <summary>
        ///     Play Mode遷移に合わせて現在のViewModelへ接続し直す。
        /// </summary>
        /// <param name="state"> 遷移後のPlay Mode状態。 </param>
        private void PlayModeStateChangedHandler(PlayModeStateChange state)
        {
            // Edit ModeにはViewModelが存在しないため未接続表示とし、Play Mode開始後に取得し直す。
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

        /// <summary>
        ///     PauseManagerへの操作ボタンと状態表示要素を構成する。
        /// </summary>
        /// <param name="container"> UXMLから生成されたルート要素。 </param>
        /// <returns> 同期的に完了する初期化処理。 </returns>
        protected override Awaitable Initialize_S(VisualElement container)
        {
            // 非同期UXML初期化より先にWindowが閉じられた場合は、購読を開始しない。
            if (_isDisposed) { return SymphonyAwaitable.Completed(); }

            // 状態表示と操作に使うVisualElementを、生成済みのUXMLから取得する。
            SymphonyDocumentationGUI.BindOpenButton(container, SymphonyDocumentPageEnum.PauseManager);

            _pauseVisual = container.Q<VisualElement>("pause");
            _pauseText = container.Q<Label>("pause-text");
            _subscriberText = container.Q<Label>("pausable-subscribers");

            _pauseButton = container.Q<Button>("button-pause");
            _resumeButton = container.Q<Button>("button-resume");
            _pauseButton.clicked += () => SetPause(true);
            _resumeButton.clicked += () => SetPause(false);

            // VisualElementの匿名ラムダは要素と同時に破棄されるが、static eventは明示解除できる形で購読する。
            EditorApplication.playModeStateChanged += PlayModeStateChangedHandler;
            BindViewModel();

            return SymphonyAwaitable.Completed();
        }

        /// <summary>
        ///     Pause Manager初期化後のEditor callbackでViewModelへ接続する。
        /// </summary>
        private void DelayedBindViewModel()
        {
            // 一度だけ遅延実行し、Compositionによる初期化が完了した時点のViewModelを取得する。
            EditorApplication.delayCall -= DelayedBindViewModel;
            BindViewModel();
        }

        /// <summary>
        ///     現在のPause ViewModelへ接続する。
        /// </summary>
        private void BindViewModel()
        {
            // ViewModelはCompositionが所有するため、Windowは既存の購読ハンドルだけを置き換える。
            UnbindViewModel();

            // Window破棄後またはEdit ModeではViewModelへ接続せず、未接続表示を維持する。
            if (_isDisposed
                || !EditorApplication.isPlaying
                || !PauseManager.IsInitialized)
            {
                ApplyState(default, false);
                return;
            }

            PauseViewModel viewModel = PauseManager.CurrentViewModel;
            // CompositionがまだViewModelを公開していない場合は、操作不能な未接続表示にする。
            if (viewModel == null)
            {
                ApplyState(default, false);
                return;
            }

            // RuntimeのReactivePropertyはWindowより長生きするため、解除可能な購読ハンドルを保持する。
            _stateSubscription = viewModel.State.Subscribe(pauseDto => ApplyState(pauseDto, true));
        }

        /// <summary>
        ///     現在のViewModel購読を解除する。
        /// </summary>
        private void UnbindViewModel()
        {
            // OnEnable相当の初期化とDisposeを対にし、古いCompositionへの参照を残さない。
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
            // Window破棄後またはUXML構築前に遅延通知が届いた場合は表示へ触れない。
            if (_isDisposed || _pauseVisual == null) { return; }

            // 接続中はRuntime状態を表示し、未接続時は値を誤認させない中立表示へ戻す。
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

        /// <summary>
        ///     ポーズ状態を切り替える。
        /// </summary>
        /// <remarks> 未初期化の場合は操作しない。 </remarks>
        /// <param name="isPaused"> 設定するポーズ状態。 </param>
        private static void SetPause(bool isPaused)
        {
            // Edit ModeまたはComposition初期化前は、PauseManagerへ触れると例外になるため操作しない。
            if (!EditorApplication.isPlaying || !PauseManager.IsInitialized) { return; }

            PauseManager.Pause = isPaused;
        }

        #endregion
    }
}
