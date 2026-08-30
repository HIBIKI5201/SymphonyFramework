using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     SymphonyFrameWorkの管理パネルを表示する。
    /// </summary>
    public sealed class SymphonyAdministrator : EditorWindow
    {
        #region 外部向けAPI

        /// <summary> 管理パネルを構成するUXMLファイルの基準パス。 </summary>
        // パッケージ導入とAssets直置きのどちらでも同じUXMLを解決できる基準パスを使用する。
        public static string UITK_UXML_PATH = EditorSymphonyConstant.UITK_PATH + "UXML/";

        /// <summary>
        ///     管理パネルを表示する。
        /// </summary>
        [MenuItem(SymphonyConstant.WINDOW_MENU_PATH + WINDOW_NAME, priority = 0)]
        public static void ShowWindow()
        {
            // 同じ種類のEditorWindowを再利用し、メニュー名とタイトルを一致させる。
            SymphonyAdministrator window = GetWindow<SymphonyAdministrator>();
            window.titleContent = new GUIContent(WINDOW_NAME);
        }

        #endregion

        #region 内部処理

        private const string WINDOW_NAME = "Symphony Administrator";

        private PauseWindow _pauseWindow;
        private ServiceLocateWindow _serviceLocatorWindow;
        private SceneLoadWindow _sceneLoaderWindow;
        private SceneBlockWindow _sceneBlockWindow;
        private AutoEnumGeneratorWindow _generatorWindow;
        private SaveDataWindow _saveDataRegistryWindow;
        private DebugHUDWindow _debugHUDWindow;

        /// <summary>
        ///     UXMLから管理パネルを構築する。
        /// </summary>
        private void OnEnable()
        {
            // UXMLをロードできた場合だけ、各パネルを取得してWindow側の購読解除対象として保持する。
            TemplateContainer container = LoadWindow();

            // 読み込み成功時は子パネルを保持し、失敗時は構築不能を診断できるよう通知する。
            if (container != null)
            {
                _pauseWindow = container.Q<PauseWindow>();
                _serviceLocatorWindow = container.Q<ServiceLocateWindow>();
                _sceneLoaderWindow = container.Q<SceneLoadWindow>();
                _sceneBlockWindow = container.Q<SceneBlockWindow>();
                _generatorWindow = container.Q<AutoEnumGeneratorWindow>();
                _saveDataRegistryWindow = container.Q<SaveDataWindow>();
                _debugHUDWindow = container.Q<DebugHUDWindow>();
            }
            else
            {
                SymphonyDebugLogger.LogDirect("ウィンドウがロードできませんでした", LogKindEnum.Warning);
            }
        }

        /// <summary>
        ///     保持中の管理パネルリソースを解放する。
        /// </summary>
        /// <remarks>
        ///     各パネルは状態変更eventを購読しており、Editor更新ごとのpollingは行わない。
        /// </remarks>
        private void OnDisable()
        {
            // OnEnableで開始した長寿命な購読を対で解除し、Window破棄後のコールバックを防ぐ。
            _pauseWindow?.Dispose();
            _serviceLocatorWindow?.Dispose();
            _sceneLoaderWindow?.Dispose();
            _sceneBlockWindow?.Dispose();
            _saveDataRegistryWindow?.Dispose();
            _debugHUDWindow?.Dispose();
            _pauseWindow = null;
            _serviceLocatorWindow = null;
            _sceneLoaderWindow = null;
            _sceneBlockWindow = null;
            _saveDataRegistryWindow = null;
            _debugHUDWindow = null;
        }

        /// <summary>
        ///     管理パネルのUXMLを読み込む。
        /// </summary>
        /// <returns> インスタンス化した管理ウィンドウのルート要素。 </returns>
        private TemplateContainer LoadWindow()
        {
            // 再有効化時に古いVisualElementとその匿名ラムダ購読をルートから切り離す。
            rootVisualElement.Clear();

            // パッケージ導入とAssets直置きの双方を解決する共通基準パスからUXMLを取得する。
            VisualTreeAsset windowTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                EditorSymphonyConstant.UITK_PATH + "SymphonyWindow.uxml");
            ;
            // UXMLが見つかった場合だけツリーを構築し、見つからなければ診断を残す。
            if (windowTree != null)
            {
                // 読み込んだツリーをWindowへ追加し、子パネル検索用のルートを返す。
                TemplateContainer windowElement = windowTree.Instantiate();
                rootVisualElement.Add(windowElement);
                return windowElement;
            }

            // UXMLが見つからない場合は、不完全なルートを返さず呼び出し側へ失敗を伝える。
            SymphonyDebugLogger.LogDirect("ウィンドウが見つかりません", LogKindEnum.Error);
            return null;
        }

        #endregion
    }
}
