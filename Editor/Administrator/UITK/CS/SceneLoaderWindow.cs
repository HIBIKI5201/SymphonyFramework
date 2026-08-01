using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SymphonyFrameWork.System.SceneLoad;
using SymphonyFrameWork.Utility;

using UnityEditor;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary> SceneLoaderが追跡するシーン状態を一覧表示する管理パネル。 </summary>
    [UxmlElement]
    public sealed partial class SceneLoaderWindow : SymphonyVisualElement, IDisposable
    {
        /// <summary> 管理パネル用UXMLの非同期初期化を開始する。 </summary>
        public SceneLoaderWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "SceneLoaderWindow.uxml",
            InitializeType.None,
            LoadType.AssetDataBase)
        { }

        private IDisposable _sceneSubscription;
        private List<SceneLoadDto> _sceneItems = new();
        private ListView _sceneList;
        private bool _isDisposed;

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

        /// <summary> Scene Load ViewModelを購読するシーン一覧を構成する。 </summary>
        /// <param name="container"> UXMLから生成されたルート要素。 </param>
        /// <returns> 同期的に完了する初期化処理。 </returns>
        protected override ValueTask Initialize_S(VisualElement container)
        {
            if (_isDisposed)
            {
                return default;
            }

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
                    ApplyScenes(Array.Empty<SceneLoadDto>());
                    break;
            }
        }

        /// <summary> Scene Loader初期化後のEditor callbackでViewModelへ接続する。 </summary>
        private void DelayedBindViewModel()
        {
            EditorApplication.delayCall -= DelayedBindViewModel;
            BindViewModel();
        }

        /// <summary> 現在のScene Load ViewModelへ接続する。 </summary>
        private void BindViewModel()
        {
            UnbindViewModel();

            if (_isDisposed
                || !EditorApplication.isPlaying
                || !SceneLoader.IsInitialized)
            {
                ApplyScenes(Array.Empty<SceneLoadDto>());
                return;
            }

            SceneLoadViewModel viewModel = SceneLoader.CurrentViewModel;
            if (viewModel == null)
            {
                ApplyScenes(Array.Empty<SceneLoadDto>());
                return;
            }

            _sceneSubscription = viewModel.Scenes.Subscribe(ApplyScenes);
        }

        /// <summary> 現在のViewModel購読を解除する。 </summary>
        private void UnbindViewModel()
        {
            _sceneSubscription?.Dispose();
            _sceneSubscription = null;
        }

        /// <summary> 最新Dto一覧をListViewへ反映する。 </summary>
        /// <param name="sceneDtos"> ViewModelが公開した変更不能なDto一覧。 </param>
        private void ApplyScenes(IReadOnlyList<SceneLoadDto> sceneDtos)
        {
            _sceneItems = sceneDtos == null
                ? new List<SceneLoadDto>()
                : new List<SceneLoadDto>(sceneDtos);

            if (_sceneList == null)
            {
                return;
            }

            _sceneList.itemsSource = _sceneItems;
            _sceneList.Rebuild();
        }

    }
}
