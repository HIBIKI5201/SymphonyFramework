using SymphonyFrameWork.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     SymphonyFrameWorkの管理パネルを表示するクラス
    /// </summary>
    public class SymphonyAdministrator : EditorWindow
    {
        private const string WINDOW_NAME = "Symphony Administrator";

        public static string UITK_UXML_PATH = EditorSymphonyConstant.UITK_PATH + "UXML/";

        private PauseWindow _pauseWindow;
        private ServiceLocatorWindow _serviceLocatorWindow;
        private AutoEnumGeneratorWindow _generatorWindow;

        private void Update()
        {
            _pauseWindow?.Update();
            _serviceLocatorWindow?.Update();
        }

        private void OnEnable()
        {
            var container = LoadWindow();

            if (container != null)
            {
                _pauseWindow = container.Q<PauseWindow>();
                _serviceLocatorWindow = container.Q<ServiceLocatorWindow>();
                _generatorWindow = container.Q<AutoEnumGeneratorWindow>();
            }
            else
            {
                Debug.LogWarning("ウィンドウがロードできませんでした");
            }

            EditorApplication.update += Update;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Update;
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
        /// <returns></returns>
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