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
        #region 外部向けAPI

        /// <summary> このノードの表示名。 </summary>
        public string Name { get; private set; }

        /// <summary> 名前の昇順に並んだ子ノード。 </summary>
        public IReadOnlyList<AssetPathTreeNode> Children => _children;

        /// <summary> このノードの配下に含まれるアセットの数。 </summary>
        public int AssetCount { get; private set; }

        /// <summary> 子を持たない末端かを示す。 </summary>
        public bool IsLeaf => _children.Count == 0;

        /// <summary> UI上で展開されているか。 </summary>
        /// <remarks> 描画側が書き換える。 </remarks>
        public bool IsExpanded = true;

        /// <summary>
        ///     パスの一覧から階層ツリーを組み立てる。
        /// </summary>
        /// <param name="paths"> 区切り文字で階層を表したパスの一覧。 </param>
        /// <param name="rootName"> ルートノードの表示名。 </param>
        /// <returns> 子が名前の昇順に並んだルートノード。 </returns>
        internal static AssetPathTreeNode Build(IEnumerable<string> paths, string rootName)
        {
            // 入力が無い場合も、描画側が扱える空のルートを返す。
            AssetPathTreeNode root = new() { Name = rootName };

            // 列挙元が未設定なら、空のツリーとして扱う。
            if (paths == null) { return root; }

            // 入力順に各パスを処理し、共通する階層は同じノードへ集約する。
            foreach (string path in paths)
            {
                // 空のパスは階層を構成できないため除外する。
                if (string.IsNullOrEmpty(path)) { continue; }

                root.Add(path.Replace("\\", "/").Split('/'));
            }

            // 入力順に依存しない表示順へ揃えてから公開する。
            root.Sort();
            return root;
        }

        #endregion

        #region 内部処理

        private readonly List<AssetPathTreeNode> _children = new();

        /// <summary>
        ///     分割済みのパス要素を子孫として登録する。
        /// </summary>
        private void Add(string[] segments)
        {
            // ルートを含む経路上の各ノードへ、配下に追加されるアセット数を反映する。
            AssetPathTreeNode current = this;
            current.AssetCount++;

            // 区切り文字で分割した順序を、親から子への階層として保持する。
            foreach (string segment in segments)
            {
                // 連続する区切り文字から生じた空要素は、名前の無い階層を作るため除外する。
                if (string.IsNullOrEmpty(segment)) { continue; }

                AssetPathTreeNode child = current._children
                    .FirstOrDefault(node => node.Name == segment);

                // 同名の子が無い場合だけ生成し、共通するパス要素は同じノードへ集約する。
                if (child == null)
                {
                    child = new AssetPathTreeNode { Name = segment };
                    current._children.Add(child);
                }

                child.AssetCount++;
                current = child;
            }
        }

        /// <summary>
        ///     子孫を名前の昇順へ並べ替える。
        /// </summary>
        private void Sort()
        {
            // 全階層の表示順を安定させるため、現在の子を並べてから子孫へ再帰する。
            _children.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));

            // 子孫にも同じ並べ替え規則を適用する。
            foreach (AssetPathTreeNode child in _children) { child.Sort(); }
        }

        #endregion
    }
}
