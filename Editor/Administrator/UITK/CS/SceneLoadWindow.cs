using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SymphonyFrameWork.System.SceneLoad;
using SymphonyFrameWork.Utility;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     SceneLoaderが追跡するシーン状態を一覧表示する。
    /// </summary>
    [UxmlElement]
    public sealed partial class SceneLoadWindow : SymphonyVisualElement, IDisposable
    {
        #region 外部向けAPI

        /// <summary>
        ///     管理パネル用UXMLの非同期初期化を開始する。
        /// </summary>
        /// <remarks>
        ///     UXMLの基準パスはパッケージ導入とAssets直置きの双方を解決する。
        /// </remarks>
        public SceneLoadWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "SceneLoadWindow.uxml",
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

        private IDisposable _sceneSubscription;
        private List<SceneLoadDto> _sceneItems = new();
        private ListView _sceneList;
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
                    ApplyScenes(Array.Empty<SceneLoadDto>());
                    break;
            }
        }

        /// <summary>
        ///     Scene Load ViewModelを購読するシーン一覧を構成する。
        /// </summary>
        /// <param name="container"> UXMLから生成されたルート要素。 </param>
        /// <returns> 同期的に完了する初期化処理。 </returns>
        protected override Awaitable Initialize_S(VisualElement container)
        {
            // 非同期UXML初期化より先にWindowが閉じられた場合は、購読を開始しない。
            if (_isDisposed) { return SymphonyAwaitable.Completed(); }

            // RuntimeのDtoを表示するListViewを生成済みUXMLへ接続する。
            SymphonyDocumentationGUI.BindOpenButton(container, SymphonyDocumentPageEnum.SceneLoader);

            _sceneList = container.Q<ListView>("scene-list");
            _sceneList.makeItem = () => new Label();
            _sceneList.bindItem = (element, index) =>
            {
                SceneLoadDto scene = _sceneItems[index];
                (element as Label).text =
                    $"Name : {scene.SceneName}\n"
                    + $"State : {scene.State}, Priority : {scene.Priority}\n"
                    + $"Progress : {scene.Progress:P0}, Active : {scene.IsActive}";
            };
            _sceneList.itemsSource = _sceneItems;
            _sceneList.selectionType = SelectionType.None;

            // ListViewの匿名ラムダは要素と同時に破棄されるが、static eventは明示解除できる形で購読する。
            EditorApplication.playModeStateChanged += PlayModeStateChangedHandler;
            BindViewModel();
            return SymphonyAwaitable.Completed();
        }

        /// <summary>
        ///     Scene Loader初期化後のEditor callbackでViewModelへ接続する。
        /// </summary>
        private void DelayedBindViewModel()
        {
            // 一度だけ遅延実行し、Compositionによる初期化が完了した時点のViewModelを取得する。
            EditorApplication.delayCall -= DelayedBindViewModel;
            BindViewModel();
        }

        /// <summary>
        ///     現在のScene Load ViewModelへ接続する。
        /// </summary>
        private void BindViewModel()
        {
            // ViewModelはCompositionが所有するため、Windowは既存の購読ハンドルだけを置き換える。
            UnbindViewModel();

            // Window破棄後またはEdit ModeではViewModelへ接続せず、空の一覧を維持する。
            if (_isDisposed
                || !EditorApplication.isPlaying
                || !SceneLoader.IsInitialized)
            {
                ApplyScenes(Array.Empty<SceneLoadDto>());
                return;
            }

            SceneLoadViewModel viewModel = SceneLoader.CurrentViewModel;
            // CompositionがまだViewModelを公開していない場合は、未接続の空一覧にする。
            if (viewModel == null)
            {
                ApplyScenes(Array.Empty<SceneLoadDto>());
                return;
            }

            // RuntimeのReactivePropertyはWindowより長生きするため、解除可能な購読ハンドルを保持する。
            _sceneSubscription = viewModel.Scenes.Subscribe(ApplyScenes);
        }

        /// <summary>
        ///     現在のViewModel購読を解除する。
        /// </summary>
        private void UnbindViewModel()
        {
            // 初期化時の購読とDisposeを対にし、古いCompositionへの参照を残さない。
            _sceneSubscription?.Dispose();
            _sceneSubscription = null;
        }

        /// <summary>
        ///     最新Dto一覧をListViewへ反映する。
        /// </summary>
        /// <param name="sceneDtos"> ViewModelが公開した変更不能なDto一覧。 </param>
        private void ApplyScenes(IReadOnlyList<SceneLoadDto> sceneDtos)
        {
            // 通知後に元の一覧が変化しても表示内容が揺れないよう、Window専用のスナップショットを作る。
            _sceneItems = sceneDtos == null
                ? new List<SceneLoadDto>()
                : new List<SceneLoadDto>(sceneDtos);

            // UXML構築前に初期値が通知された場合は、保持だけ行い表示更新を待つ。
            if (_sceneList == null) { return; }

            // ListViewの参照先を最新スナップショットへ差し替えて再構築する。
            _sceneList.itemsSource = _sceneItems;
            _sceneList.Rebuild();
        }

        #endregion
    }
}
