using NUnit.Framework;

using SymphonyFrameWork.Editor;
using SymphonyFrameWork.System.SceneBlock;

using UnityEditor;

using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SceneBlockAssetの専用Inspectorが対象型へ結び付いていることを検証する。
    /// </summary>
    /// <remarks>
    ///     描画そのものはEditorのGUI操作を伴うため自動で検証できない。
    ///     ここでは結び付けと生成だけを固定し、表示内容は人が確認する。
    /// </remarks>
    public sealed class SceneBlockAssetDrawerTests
    {
        /// <summary>
        ///     SceneBlockAssetの専用Inspectorとして登録されている。
        /// </summary>
        [Test]
        public void CustomEditor_TargetsSceneBlockAsset()
        {
            CustomEditor[] attributes = (CustomEditor[])typeof(SceneBlockAssetDrawer)
                .GetCustomAttributes(typeof(CustomEditor), false);

            Assert.That(attributes, Has.Length.EqualTo(1));
        }

        /// <summary>
        ///     SceneBlockAssetのInspectorとして生成できる。
        /// </summary>
        [Test]
        public void CreateEditor_ForSceneBlockAsset_ReturnsDrawer()
        {
            SceneBlockAsset asset = ScriptableObject.CreateInstance<SceneBlockAsset>();
            asset.name = "TownBlock";

            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(asset);

            try
            {
                Assert.That(editor, Is.InstanceOf<SceneBlockAssetDrawer>());
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
