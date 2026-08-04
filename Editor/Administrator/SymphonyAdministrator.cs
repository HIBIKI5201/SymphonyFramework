using SymphonyFrameWork.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     SymphonyFrameWorkの管理パネルを表示するクラス
    /// </summary>
    public sealed class SymphonyAdministrator : EditorWindow
    {
        private const string WINDOW_NAME = "Symphony Administrator";

        /// <summary> 管理パネルを構成するUXMLファイルの基準パス。 </summary>
        public static string UITK_UXML_PATH = EditorSymphonyConstant.UITK_PATH + "UXML/";

        private PauseWindow _pauseWindow;
        private ServiceLocateWindow _serviceLocatorWindow;
        private SceneLoadWindow _sceneLoaderWindow;
        private AutoEnumGeneratorWindow _generatorWindow;
        private SaveDataWindow _saveDataRegistryWindow;

        /// <summary> UXMLから管理パネルを構築する。 </summary>
        private void OnEnable()
        {
            var container = LoadWindow();

            if (container != null)
            {
                _pauseWindow = container.Q<PauseWindow>();
                _serviceLocatorWindow = container.Q<ServiceLocateWindow>();
                _sceneLoaderWindow = container.Q<SceneLoadWindow>();
                _generatorWindow = container.Q<AutoEnumGeneratorWindow>();
                _saveDataRegistryWindow = container.Q<SaveDataWindow>();
            }
            else
            {
                Debug.LogWarning("ウィンドウがロードできませんでした");
            }
        }

        /// <summary>
        ///     保持中の管理パネルリソースを解放する。
        ///     各パネルは状態変更eventを購読しており、Editor更新ごとのpollingは行わない。
        /// </summary>
        private void OnDisable()
        {
            _pauseWindow?.Dispose();
            _serviceLocatorWindow?.Dispose();
            _sceneLoaderWindow?.Dispose();
            _saveDataRegistryWindow?.Dispose();
            _pauseWindow = null;
            _serviceLocatorWindow = null;
            _sceneLoaderWindow = null;
            _saveDataRegistryWindow = null;
        }


        /// <summary>
        ///     ウィンドウ表示
        /// </summary>
        [MenuItem(SymphonyConstant.WINDOW_MENU_PATH + WINDOW_NAME, priority = 0)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<SymphonyAdministrator>();
            wnd.titleContent = new GUIContent(WINDOW_NAME);
        }

        /// <summary>
        ///     UXMLを追加
        /// </summary>
        /// <returns> インスタンス化した管理ウィンドウのルート要素。 </returns>
        private TemplateContainer LoadWindow()
        {
            rootVisualElement.Clear();

            var windowTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorSymphonyConstant.UITK_PATH + "SymphonyWindow.uxml");
            ;
            if (windowTree != null)
            {
                var windowElement = windowTree.Instantiate();
                rootVisualElement.Add(windowElement);
                return windowElement;
            }

            Debug.LogError("ウィンドウが見つかりません");
            return null;
        }
    }
}
