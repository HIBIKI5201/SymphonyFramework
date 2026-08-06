using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     パッケージへ含まれるアセットを階層表示し、出力の実行を確認するEditorWindow。
    /// </summary>
    internal sealed class AssetStoreToolsPackageConfirmWindow : EditorWindow
    {
        /// <summary>
        ///     出力計画の内容を表示する確認ウィンドウを開く。
        /// </summary>
        /// <param name="plan"> 提示する出力計画。 </param>
        /// <param name="onConfirmed"> 出力が承認されたときに呼ぶ処理。 </param>
        internal static void Open(AssetStoreToolsPackagePlan plan, Action onConfirmed)
        {
            if (plan == null)
            {
                return;
            }

            var window = CreateInstance<AssetStoreToolsPackageConfirmWindow>();
            window.titleContent = new GUIContent("Export Confirmation");
            window.minSize = new Vector2(420f, 320f);
            window._plan = plan;
            window._onConfirmed = onConfirmed;
            window._roots = BuildRoots(plan);
            window.ShowUtility();
        }

        private AssetStoreToolsPackagePlan _plan;
        private Action _onConfirmed;
        private List<AssetPathTreeNode> _roots;
        private Vector2 _scrollPosition;
        private bool _isConfirmed;
        private bool _shouldClose;

        /// <summary> 出力内容の要約、階層ツリー、確定操作を描画する。 </summary>
        private void OnGUI()
        {
            // ドメインリロードで計画を失った場合は提示できないため閉じる。
            if (_plan == null || _roots == null)
            {
                Close();
                return;
            }

            GUILayout.Label("以下のアセットを出力します", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Pipeline", _plan.PipelineName);
            EditorGUILayout.LabelField("Steps", BuildStepsLabel(_plan));
            EditorGUILayout.LabelField("Total Assets", _plan.TotalAssetCount.ToString());

            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox);
            foreach (AssetPathTreeNode root in _roots)
            {
                DrawNode(root, 0);
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export", GUILayout.Height(28)))
            {
                _isConfirmed = true;
                _shouldClose = true;
            }

            if (GUILayout.Button("Cancel", GUILayout.Height(28)))
            {
                _shouldClose = true;
            }
            EditorGUILayout.EndHorizontal();

            // レイアウトグループを閉じてから破棄する。OnGUIの途中で閉じると描画が崩れる。
            if (_shouldClose)
            {
                Close();
            }
        }

        /// <summary> 承認された場合にだけ出力処理を呼ぶ。 </summary>
        private void OnDestroy()
        {
            if (_isConfirmed)
            {
                _onConfirmed?.Invoke();
            }
        }

        /// <summary>
        ///     実行する手順を並び順のまま1行へ組み立てる。
        /// </summary>
        /// <remarks>
        ///     並べ替えは行わない。誤った順序をそのまま見せることが、順序の誤りに気づく手段になる。
        /// </remarks>
        /// <param name="plan"> 提示する出力計画。 </param>
        /// <returns> 矢印区切りの手順名。手順が無い場合はその旨を示す文字列。 </returns>
        private static string BuildStepsLabel(AssetStoreToolsPackagePlan plan)
        {
            if (plan.Steps.Count == 0)
            {
                return "（手順が設定されていません）";
            }

            return string.Join(" → ", plan.Steps.Select(step => step.DisplayName));
        }

        /// <summary> 計画のディレクトリごとにツリーのルートを組み立てる。 </summary>
        private static List<AssetPathTreeNode> BuildRoots(AssetStoreToolsPackagePlan plan)
        {
            return plan.Entries
                .Select(entry => AssetPathTreeNode.Build(
                    entry.AssetPaths.Select(path => ToRelativePath(path, entry.DirectoryPath)),
                    entry.Name))
                .ToList();
        }

        /// <summary> ディレクトリからの相対パスへ変換する。配下でなければそのまま返す。 </summary>
        private static string ToRelativePath(string path, string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath)
                || !path.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return path.Substring(directoryPath.Length).TrimStart('/');
        }

        /// <summary> ノードとその子孫を折りたたみ付きで描画する。 </summary>
        private static void DrawNode(AssetPathTreeNode node, int indentLevel)
        {
            EditorGUI.indentLevel = indentLevel;

            if (node.IsLeaf)
            {
                // アセットが1件も無いディレクトリは、出力時に警告になることを示す。
                string label = node.AssetCount == 0
                    ? $"{node.Name}（対象アセットなし）"
                    : node.Name;

                EditorGUILayout.LabelField(label);
                return;
            }

            node.IsExpanded = EditorGUILayout.Foldout(
                node.IsExpanded,
                $"{node.Name} ({node.AssetCount})",
                true);

            if (!node.IsExpanded)
            {
                return;
            }

            foreach (AssetPathTreeNode child in node.Children)
            {
                DrawNode(child, indentLevel + 1);
            }
        }
    }
}
