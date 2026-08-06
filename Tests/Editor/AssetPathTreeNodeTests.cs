using NUnit.Framework;

using SymphonyFrameWork.Editor;

using System.Linq;

namespace SymphonyFrameWork.Tests
{
    /// <summary> アセットパスの一覧から階層ツリーを組み立てる契約を検証する。 </summary>
    public sealed class AssetPathTreeNodeTests
    {
        /// <summary> パスの区切りごとに親子を作る。 </summary>
        [Test]
        public void Build_NestedPaths_CreatesHierarchy()
        {
            AssetPathTreeNode root = AssetPathTreeNode.Build(
                new[] { "DOTween/Modules/DOTweenModuleUI.cs", "DOTween/DOTween.dll" },
                "Demigiant");

            Assert.That(root.Name, Is.EqualTo("Demigiant"));
            Assert.That(root.Children.Count, Is.EqualTo(1));

            AssetPathTreeNode dotween = root.Children[0];
            Assert.That(dotween.Name, Is.EqualTo("DOTween"));
            Assert.That(dotween.Children.Count, Is.EqualTo(2));

            AssetPathTreeNode modules = dotween.Children.First(node => node.Name == "Modules");
            Assert.That(modules.Children.Count, Is.EqualTo(1));
            Assert.That(modules.Children[0].Name, Is.EqualTo("DOTweenModuleUI.cs"));
            Assert.That(modules.Children[0].IsLeaf, Is.True);
        }

        /// <summary> 子は名前の昇順に並ぶ。 </summary>
        [Test]
        public void Build_UnorderedPaths_SortsChildrenByName()
        {
            AssetPathTreeNode root = AssetPathTreeNode.Build(
                new[] { "c.cs", "a.cs", "b.cs" },
                "Root");

            CollectionAssert.AreEqual(
                new[] { "a.cs", "b.cs", "c.cs" },
                root.Children.Select(node => node.Name));
        }

        /// <summary> 各ノードが配下のアセット数を持つ。 </summary>
        [Test]
        public void Build_CountsAssetsPerNode()
        {
            AssetPathTreeNode root = AssetPathTreeNode.Build(
                new[] { "Editor/a.cs", "Editor/b.cs", "c.cs" },
                "Root");

            Assert.That(root.AssetCount, Is.EqualTo(3));

            AssetPathTreeNode editor = root.Children.First(node => node.Name == "Editor");
            Assert.That(editor.AssetCount, Is.EqualTo(2));
            Assert.That(editor.IsLeaf, Is.False);

            AssetPathTreeNode leaf = root.Children.First(node => node.Name == "c.cs");
            Assert.That(leaf.AssetCount, Is.EqualTo(1));
            Assert.That(leaf.IsLeaf, Is.True);
        }

        /// <summary> 空の一覧では子を持たないルートを返す。 </summary>
        [Test]
        public void Build_EmptyPaths_ReturnsEmptyRoot()
        {
            AssetPathTreeNode root = AssetPathTreeNode.Build(new string[0], "Root");

            Assert.That(root.Children, Is.Empty);
            Assert.That(root.AssetCount, Is.Zero);
            Assert.That(root.IsLeaf, Is.True);
        }

        /// <summary> nullの一覧でも例外を出さず空のルートを返す。 </summary>
        [Test]
        public void Build_NullPaths_ReturnsEmptyRoot()
        {
            AssetPathTreeNode root = AssetPathTreeNode.Build(null, "Root");

            Assert.That(root.Children, Is.Empty);
            Assert.That(root.AssetCount, Is.Zero);
        }
    }
}
