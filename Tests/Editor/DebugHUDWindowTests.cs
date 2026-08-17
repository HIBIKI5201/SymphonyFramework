using NUnit.Framework;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Editor;

using UnityEditor;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Tests
{
    /// <summary> Debug HUD管理パネルのUXML契約を検証する。 </summary>
    public sealed class DebugHUDWindowTests
    {
        /// <summary> 状態表示、登録数、操作ボタンを生成する。 </summary>
        [Test]
        public void Instantiate_DebugHUDPanel_ContainsStatusAndButtons()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                EditorSymphonyConstant.UITK_PATH + "UXML/DebugHUDWindow.uxml");

            Assert.That(tree, Is.Not.Null);
            TemplateContainer container = tree.Instantiate();
            Assert.That(container.Q<Label>("hud-status"), Is.Not.Null);
            Assert.That(container.Q<Label>("hud-registered-texts"), Is.Not.Null);
            Assert.That(container.Q<Button>("button-show"), Is.Not.Null);
            Assert.That(container.Q<Button>("button-hide"), Is.Not.Null);
            Assert.That(container.Q<Button>("button-document"), Is.Not.Null);
        }
    }
}
