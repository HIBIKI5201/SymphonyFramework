using NUnit.Framework;

using SymphonyFramework.Core;

using UnityEditor;

using UnityEngine.UIElements;

namespace SymphonyFramework.Tests
{
    /// <summary> Suite管理パネルのUXML契約を検証する。 </summary>
    public sealed class SuiteWindowTests
    {
        /// <summary> ブロック一覧とドキュメントボタンを生成する。 </summary>
        [Test]
        public void Instantiate_SuitePanel_ContainsListAndDocumentButton()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                EditorSymphonyConstant.UITK_PATH + "UXML/SuiteWindow.uxml");

            Assert.That(tree, Is.Not.Null);

            TemplateContainer container = tree.Instantiate();
            Assert.That(container.Q<ListView>("block-list"), Is.Not.Null);
            Assert.That(container.Q<Button>("button-document"), Is.Not.Null);
        }

        /// <summary> 管理パネルのルートUXMLへSuiteパネルが登録されている。 </summary>
        [Test]
        public void SymphonyWindow_ContainsSuitePanel()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                EditorSymphonyConstant.UITK_PATH + "SymphonyWindow.uxml");

            Assert.That(tree, Is.Not.Null);

            // ルートへ足し忘れると、パネルを作ってもAdministratorに現れない。
            TemplateContainer container = tree.Instantiate();
            Assert.That(container.Q<SymphonyFramework.Editor.SuiteWindow>(), Is.Not.Null);
        }
    }
}
