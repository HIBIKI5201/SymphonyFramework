using NUnit.Framework;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Editor;

using UnityEditor;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Tests
{
    /// <summary> Symphony AdministratorのUXMLがカスタムパネル型を解決できることを検証する。 </summary>
    public sealed class SymphonyAdministratorUxmlTests
    {
        /// <summary> 管理ウィンドウを構成する6つのカスタムパネルを登録済み型として生成する。 </summary>
        [Test]
        public void Instantiate_AllAdministratorPanels_UsesRegisteredCustomElements()
        {
            VisualTreeAsset windowTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                EditorSymphonyConstant.UITK_PATH + "SymphonyWindow.uxml");

            Assert.That(windowTree, Is.Not.Null, "SymphonyWindow.uxmlを読み込めませんでした。");

            TemplateContainer container = windowTree.Instantiate();
            PauseWindow pauseWindow = container.Q<PauseWindow>();
            ServiceLocateWindow serviceLocateWindow = container.Q<ServiceLocateWindow>();
            SceneLoadWindow sceneLoadWindow = container.Q<SceneLoadWindow>();
            SaveDataWindow saveDataWindow = container.Q<SaveDataWindow>();
            AutoEnumGeneratorWindow autoEnumGeneratorWindow =
                container.Q<AutoEnumGeneratorWindow>();
            DebugHUDWindow debugHUDWindow = container.Q<DebugHUDWindow>();

            try
            {
                Assert.That(pauseWindow, Is.Not.Null);
                Assert.That(serviceLocateWindow, Is.Not.Null);
                Assert.That(sceneLoadWindow, Is.Not.Null);
                Assert.That(saveDataWindow, Is.Not.Null);
                Assert.That(autoEnumGeneratorWindow, Is.Not.Null);
                Assert.That(debugHUDWindow, Is.Not.Null);
            }
            finally
            {
                pauseWindow?.Dispose();
                serviceLocateWindow?.Dispose();
                sceneLoadWindow?.Dispose();
                saveDataWindow?.Dispose();
                debugHUDWindow?.Dispose();
            }
        }
    }
}
