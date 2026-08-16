using System.Linq;

using NUnit.Framework;

using SymphonyFrameWork.Editor;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     セーブデータ型の名前空間ツリー構築規則を検証する。
    /// </summary>
    public sealed class SaveDataTypeTreeNodeTests
    {
        /// <summary>
        ///     分岐の無い名前空間は1つの表示ノードへまとまる。
        /// </summary>
        [Test]
        public void Build_SingleBranchNamespaces_AreMergedIntoOneNode()
        {
            SaveDataTypeTreeNode root = SaveDataTypeTreeNode.Build(new[]
            {
                "SpaceA.ScopeB.ClassC",
                "SpaceA.ScopeB.ClassD",
            });

            Assert.That(root.Children.Select(node => node.Name), Is.EqualTo(new[] { "SpaceA.ScopeB" }));
            Assert.That(root.Children[0].Children.Select(node => node.Name),
                Is.EqualTo(new[] { "ClassC", "ClassD" }));
        }

        /// <summary>
        ///     名前空間が分岐する位置ではノードを分ける。
        /// </summary>
        [Test]
        public void Build_BranchingNamespaces_AreSplitAtBranchPoint()
        {
            SaveDataTypeTreeNode root = SaveDataTypeTreeNode.Build(new[]
            {
                "SpaceA.ScopeB.ClassC",
                "SpaceA.ScopeB.ClassD",
                "SpaceA.ScopeE.ClassF",
            });

            Assert.That(root.Children.Select(node => node.Name), Is.EqualTo(new[] { "SpaceA" }));
            Assert.That(root.Children[0].Children.Select(node => node.Name),
                Is.EqualTo(new[] { "ScopeB", "ScopeE" }));
        }

        /// <summary>
        ///     名前空間直下の唯一の子が型でも別の葉として維持する。
        /// </summary>
        [Test]
        public void Build_SingleClassUnderNamespace_KeepsClassAsOwnLeaf()
        {
            SaveDataTypeTreeNode root = SaveDataTypeTreeNode.Build(new[] { "SpaceA.ClassB" });

            Assert.That(root.Children[0].Name, Is.EqualTo("SpaceA"));
            Assert.That(root.Children[0].Children[0].Name, Is.EqualTo("ClassB"));
            Assert.That(root.Children[0].Children[0].IsLeaf, Is.True);
        }

        /// <summary>
        ///     名前空間の無い型はルート直下の葉になる。
        /// </summary>
        [Test]
        public void Build_TypesWithoutNamespace_BecomeRootLeaves()
        {
            SaveDataTypeTreeNode root = SaveDataTypeTreeNode.Build(new[] { "ClassB" });

            Assert.That(root.Children[0].Name, Is.EqualTo("ClassB"));
            Assert.That(root.Children[0].IsLeaf, Is.True);
        }

        /// <summary>
        ///     入力順に関係なく兄弟を名前の序数昇順へ並べる。
        /// </summary>
        [Test]
        public void Build_UnorderedInput_SortsChildrenByName()
        {
            SaveDataTypeTreeNode root = SaveDataTypeTreeNode.Build(new[]
            {
                "Zulu.TypeZ",
                "Alpha.TypeA",
                "Middle.TypeM",
            });

            Assert.That(root.Children.Select(node => node.Name),
                Is.EqualTo(new[] { "Alpha", "Middle", "Zulu" }));
        }

        /// <summary>
        ///     型の葉は保存キーになる完全名を保持する。
        /// </summary>
        [Test]
        public void Build_Leaf_ExposesFullTypeName()
        {
            SaveDataTypeTreeNode root = SaveDataTypeTreeNode.Build(new[] { "SpaceA.ClassB" });

            Assert.That(root.Children[0].Children[0].TypeFullName, Is.EqualTo("SpaceA.ClassB"));
        }

        /// <summary>
        ///     名前空間ノードは全子孫の型完全名を列挙する。
        /// </summary>
        [Test]
        public void EnumerateTypeFullNames_Node_ReturnsAllDescendantLeaves()
        {
            SaveDataTypeTreeNode root = SaveDataTypeTreeNode.Build(new[]
            {
                "SpaceA.ScopeB.ClassC",
                "SpaceA.ScopeE.ClassF",
            });

            Assert.That(root.Children[0].EnumerateTypeFullNames(),
                Is.EqualTo(new[] { "SpaceA.ScopeB.ClassC", "SpaceA.ScopeE.ClassF" }));
        }

        /// <summary>
        ///     型の葉は自分自身の完全名だけを列挙する。
        /// </summary>
        [Test]
        public void EnumerateTypeFullNames_Leaf_ReturnsItself()
        {
            SaveDataTypeTreeNode root = SaveDataTypeTreeNode.Build(new[] { "ClassB" });

            Assert.That(root.Children[0].EnumerateTypeFullNames(), Is.EqualTo(new[] { "ClassB" }));
        }
    }
}
