using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     フォルダを生成する。
    /// </summary>
    public static class FolderGenerator
    {
        #region 外部向けAPI

        /// <summary>
        ///     規定のディレクトリ構成を生成する。
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(FolderGenerator), priority = 100)]
        public static void GenerateFolder()
        {
            SymphonyDebugLogger.NewText($"[{nameof(GenerateFolder)}]");

            string[] assetsFolders = LoadFolderPaths(STRUCTURE_PATH);

            // FolderStructure.mdの記述順に、存在しないフォルダだけをAssets配下へ生成する。
            foreach (string folder in assetsFolders)
            {
                string path = $"{ASSETS_PATH}/{folder}";

                if (!AssetDatabase.IsValidFolder(path))
                {
                    FolderCreate(path);
                    SymphonyDebugLogger.AddText($"フォルダ作成: {path}");
                }
            }

            // すべてのフォルダを作成した後に一度だけAssetDatabaseへ反映する。
            AssetDatabase.Refresh();

            SymphonyDebugLogger.LogText();
            EditorUtility.DisplayDialog("フォルダを生成", "フォルダを生成しました", "OK");
        }

        /// <summary>
        ///     全てのフォルダのパスを生成して返す。
        /// </summary>
        /// <param name="markdownPath"> フォルダ構成を記述したMarkdownファイルのパス。 </param>
        /// <returns> Assetsからの相対フォルダパス一覧。 </returns>
        public static string[] LoadFolderPaths(string markdownPath)
        {
            // フォルダ構成の正本を全行読み込み、インデントと記述順を保持して解釈する。
            string[] lines = File.ReadAllLines(markdownPath);

            List<string> result = new();
            Stack<string> stack = new();

            foreach (string rawLine in lines)
            {
                int dashIndex = rawLine.IndexOf('-');

                // 箇条書きではない行はフォルダ定義として扱わない。
                if (dashIndex < 0) { continue; }

                int depth = dashIndex / 2;
                string folder = rawLine[(dashIndex + 1)..].Trim();

                // 現在行の深さまで親階層を戻してから、新しいフォルダを末端へ加える。
                while (stack.Count > depth) { stack.Pop(); }

                stack.Push(folder);
                string path = string.Join("/", stack.Reverse());
                result.Add(path);
            }

            return result.ToArray();
        }

        #endregion

        #region 内部処理

        private const string ASSETS_PATH = "Assets";
        private static readonly string STRUCTURE_PATH =
            EditorSymphonyConstant.FRAMEWORK_PATH + "/Editor/Generator/FolderGenerate/FolderStructure.md";

        /// <summary>
        ///     パスのフォルダを生成する。
        /// </summary>
        /// <param name="path"> 再帰的に生成するAssets配下のフォルダパス。 </param>
        private static void FolderCreate(string path)
        {
            // 既存フォルダとその内容を上書きしないため、作成済みなら終了する。
            if (AssetDatabase.IsValidFolder(path)) { return; }

            // Unity APIへ渡す前に、親パスと作成するフォルダ名へ分割する。
            string parent = Path.GetDirectoryName(path);
            string folderName = Path.GetFileName(path);

            // 親が無い場合は先に再帰生成し、AssetDatabase.CreateFolderへ有効な親を渡す。
            if (!AssetDatabase.IsValidFolder(parent)) { FolderCreate(parent); }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        #endregion
    }
}
