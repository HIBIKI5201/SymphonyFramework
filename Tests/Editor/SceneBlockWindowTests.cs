using NUnit.Framework;

using SymphonyFrameWork.Core;

using UnityEditor;

using UnityEngine.UIElements;

namespace SymphonyFrameWork.Tests
{
    /// <summary> Scene Block管理パネルのUXML契約を検証する。 </summary>
    public sealed class SceneBlockWindowTests
    {
        /// <summary> ブロック一覧とドキュメントボタンを生成する。 </summary>
        [Test]
        public void Instantiate_SceneBlockPanel_ContainsListAndDocumentButton()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                EditorSymphonyConstant.UITK_PATH + "UXML/SceneBlockWindow.uxml");

            Assert.That(tree, Is.Not.Null);

            TemplateContainer container = tree.Instantiate();
            Assert.That(container.Q<ListView>("block-list"), Is.Not.Null);
            Assert.That(container.Q<Button>("button-document"), Is.Not.Null);
        }

        /// <summary> 管理パネルのルートUXMLへScene Blockパネルが登録されている。 </summary>
        [Test]
        public void SymphonyWindow_ContainsSceneBlockPanel()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                EditorSymphonyConstant.UITK_PATH + "SymphonyWindow.uxml");

            Assert.That(tree, Is.Not.Null);

            // ルートへ足し忘れると、パネルを作ってもAdministratorに現れない。
            TemplateContainer container = tree.Instantiate();
            Assert.That(container.Q<SymphonyFrameWork.Editor.SceneBlockWindow>(), Is.Not.Null);
        }
    }
}
