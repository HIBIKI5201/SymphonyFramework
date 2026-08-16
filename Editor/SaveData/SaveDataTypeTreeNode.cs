using System;
using System.Collections.Generic;
using System.Linq;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     セーブデータ型の名前空間階層を表すノード。
    /// </summary>
    internal sealed class SaveDataTypeTreeNode
    {
        #region 外部向けAPI

        /// <summary> このノードの表示名。 </summary>
        internal string Name { get; private set; }

        /// <summary> 名前の序数昇順に並んだ子ノード。 </summary>
        internal IReadOnlyList<SaveDataTypeTreeNode> Children => _children;

        /// <summary> 葉が保持する型の完全名。名前空間ノードではnull。 </summary>
        internal string TypeFullName { get; private set; }

        /// <summary> 型を表す葉かを示す。 </summary>
        internal bool IsLeaf => TypeFullName != null;

        /// <summary> UI上で展開されているか。 </summary>
        internal bool IsExpanded = true;

        /// <summary>
        ///     型の完全名一覧から名前空間ツリーを構築する。
        /// </summary>
        /// <param name="typeFullNames"> 型の完全名一覧。 </param>
        /// <returns> 分岐の無い名前空間をまとめたルートノード。 </returns>
        internal static SaveDataTypeTreeNode Build(IEnumerable<string> typeFullNames)
        {
            // 入力が無い場合も、描画側が扱える空のルートを返す。
            SaveDataTypeTreeNode root = new() { Name = string.Empty };
            if (typeFullNames == null) { return root; }

            // 完全名を名前空間と型名へ分け、共通する名前空間を同じノードへ集約する。
            foreach (string typeFullName in typeFullNames)
            {
                if (string.IsNullOrEmpty(typeFullName)) { continue; }

                root.Add(typeFullName);
            }

            // 入力順に依存しない表示順へ揃え、単一分岐の名前空間をまとめる。
            root.CompactNamespaces(false);
            root.Sort();
            return root;
        }

        /// <summary>
        ///     このノード以下にある型の完全名を列挙する。
        /// </summary>
        /// <returns> 子の表示順に並んだ型の完全名。 </returns>
        internal IEnumerable<string> EnumerateTypeFullNames()
        {
            // 葉は一括切り替えの対象として自分自身だけを返す。
            if (IsLeaf)
            {
                yield return TypeFullName;
                yield break;
            }

            // 名前空間ノードは全子孫の葉を表示順に返す。
            foreach (SaveDataTypeTreeNode child in _children)
            {
                foreach (string typeFullName in child.EnumerateTypeFullNames())
                {
                    yield return typeFullName;
                }
            }
        }

        #endregion

        #region 内部処理

        private readonly List<SaveDataTypeTreeNode> _children = new();

        /// <summary>
        ///     1つの型の完全名をツリーへ登録する。
        /// </summary>
        /// <param name="typeFullName"> 登録する型の完全名。 </param>
        private void Add(string typeFullName)
        {
            string[] segments = typeFullName.Split('.');
            SaveDataTypeTreeNode current = this;

            // 最後の要素を型の葉、それ以前を名前空間ノードとして作る。
            for (int index = 0; index < segments.Length; index++)
            {
                string segment = segments[index];
                if (string.IsNullOrEmpty(segment)) { continue; }

                bool isLeaf = index == segments.Length - 1;
                SaveDataTypeTreeNode child = current._children.FirstOrDefault(node =>
                    node.Name == segment && node.IsLeaf == isLeaf);
                if (child == null)
                {
                    child = new SaveDataTypeTreeNode
                    {
                        Name = segment,
                        TypeFullName = isLeaf ? typeFullName : null,
                    };
                    current._children.Add(child);
                }

                current = child;
            }
        }

        /// <summary>
        ///     分岐の無い名前空間ノードを1つの表示ノードへまとめる。
        /// </summary>
        /// <param name="canMergeCurrent"> 現在のノードを子と結合できる場合はtrue。 </param>
        private void CompactNamespaces(bool canMergeCurrent)
        {
            // 子孫を先に圧縮し、現在ノードから連なる全単一分岐を一度に判定できるようにする。
            foreach (SaveDataTypeTreeNode child in _children)
            {
                child.CompactNamespaces(true);
            }

            // ルート、葉、分岐、直下が型の葉である場合は名前をまとめない。
            while (canMergeCurrent
                && !IsLeaf
                && _children.Count == 1
                && !_children[0].IsLeaf)
            {
                SaveDataTypeTreeNode child = _children[0];
                Name += "." + child.Name;
                _children.Clear();
                _children.AddRange(child._children);
            }
        }

        /// <summary>
        ///     子孫を名前の序数昇順へ並べ替える。
        /// </summary>
        private void Sort()
        {
            // 全階層で同じ表示順を保証してから子孫へ再帰する。
            _children.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            foreach (SaveDataTypeTreeNode child in _children) { child.Sort(); }
        }

        #endregion
    }
}
