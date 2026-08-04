using System.Collections.Generic;
using System.Linq;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     アセットパスの一覧を階層構造として表すノード。
    /// </summary>
    /// <remarks>
    ///     Unity APIへ触れない純粋なロジックとして保つ。描画側はこの構造を読むだけにする。
    /// </remarks>
    internal sealed class AssetPathTreeNode
    {
        /// <summary> このノードの表示名。 </summary>
        public string Name { get; private set; }

        /// <summary> 名前の昇順に並んだ子ノード。 </summary>
        public IReadOnlyList<AssetPathTreeNode> Children => _children;

        /// <summary> このノードの配下に含まれるアセットの数。 </summary>
        public int AssetCount { get; private set; }

        /// <summary> 子を持たない末端かを示す。 </summary>
        public bool IsLeaf => _children.Count == 0;

        /// <summary> UI上で展開されているか。描画側が書き換える。 </summary>
        public bool IsExpanded = true;

        /// <summary>
        ///     パスの一覧から階層ツリーを組み立てる。
        /// </summary>
        /// <param name="paths"> 区切り文字で階層を表したパスの一覧。 </param>
        /// <param name="rootName"> ルートノードの表示名。 </param>
        /// <returns> 子が名前の昇順に並んだルートノード。 </returns>
        internal static AssetPathTreeNode Build(IEnumerable<string> paths, string rootName)
        {
            var root = new AssetPathTreeNode { Name = rootName };

            if (paths == null)
            {
                return root;
            }

            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                root.Add(path.Replace("\\", "/").Split('/'));
            }

            root.Sort();
            return root;
        }

        private readonly List<AssetPathTreeNode> _children = new();

        /// <summary> 分割済みのパス要素を子孫として登録し、経路上の件数を加算する。 </summary>
        private void Add(string[] segments)
        {
            AssetPathTreeNode current = this;
            current.AssetCount++;

            foreach (string segment in segments)
            {
                if (string.IsNullOrEmpty(segment))
                {
                    continue;
                }

                AssetPathTreeNode child = current._children
                    .FirstOrDefault(node => node.Name == segment);

                if (child == null)
                {
                    child = new AssetPathTreeNode { Name = segment };
                    current._children.Add(child);
                }

                child.AssetCount++;
                current = child;
            }
        }

        /// <summary> 子孫を名前の昇順へ並べ替える。 </summary>
        private void Sort()
        {
            _children.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));

            foreach (AssetPathTreeNode child in _children)
            {
                child.Sort();
            }
        }
    }
}
