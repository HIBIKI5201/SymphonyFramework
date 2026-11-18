using NUnit.Framework;

using SymphonyFramework.Editor;
using SymphonyFramework.Scenes;

using UnityEditor;

using UnityEngine;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     SuiteAssetの専用Inspectorが対象型へ結び付いていることを検証する。
    /// </summary>
    /// <remarks>
    ///     描画そのものはEditorのGUI操作を伴うため自動で検証できない。
    ///     ここでは結び付けと生成だけを固定し、表示内容は人が確認する。
    /// </remarks>
    public sealed class SuiteAssetDrawerTests
    {
        /// <summary>
        ///     SuiteAssetの専用Inspectorとして登録されている。
        /// </summary>
        [Test]
        public void CustomEditor_TargetsSuiteAsset()
        {
            CustomEditor[] attributes = (CustomEditor[])typeof(SuiteAssetDrawer)
                .GetCustomAttributes(typeof(CustomEditor), false);

            Assert.That(attributes, Has.Length.EqualTo(1));
        }

        /// <summary>
        ///     SuiteAssetのInspectorとして生成できる。
        /// </summary>
        [Test]
        public void CreateEditor_ForSuiteAsset_ReturnsDrawer()
        {
            SuiteAsset asset = ScriptableObject.CreateInstance<SuiteAsset>();
            asset.name = "TownBlock";

            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(asset);

            try
            {
                Assert.That(editor, Is.InstanceOf<SuiteAssetDrawer>());
            }
            finally
            {
                // EditorとScriptableObjectはGCで消えないため、明示的に破棄する。
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(asset);
            }
        }
    }
}
