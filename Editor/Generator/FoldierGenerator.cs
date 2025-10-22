using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger;
using System.IO;
using System.Linq;
using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     フォルダを生成する
    /// </summary>
    public static class FolderGenerator
    {
        /// <summary>
        ///     規定のディレクトリ構成を生成する
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(FolderGenerator), priority = 100)]
        public static void GenerateFolder()
        {
            SymphonyDebugLogger.NewText($"[{nameof(GenerateFolder)}]");

            string[] assetsFolders = GetFolderPaths();

            //全てのフォルダを生成する
            foreach (string folder in assetsFolders)
            {
                string path = $"{ASSETS_PATH}/{folder}";

                if (!AssetDatabase.IsValidFolder(path))
                {
                    FolderCreate(path);
                    SymphonyDebugLogger.AddText($"フォルダ作成: {path}");
                }
            }
            AssetDatabase.Refresh();

            SymphonyDebugLogger.LogText();
            EditorUtility.DisplayDialog("フォルダを生成", "フォルダを生成しました", "OK");
        }

        private const string ASSETS_PATH = "Assets";

        /// <summary>
        ///     パスのフォルダを生成する。
        /// </summary>
        /// <param name="path"></param>
        private static void FolderCreate(string path)
        {
            // フォルダがあれば終了。
            if (AssetDatabase.IsValidFolder(path)) return;

            // パスをディレクトリとフォルダ名に分ける。
            string parent = Path.GetDirectoryName(path);
            string folderName = Path.GetFileName(path);

            // ディレクトリが無ければ再帰的にフォルダを生成する。
            if (!AssetDatabase.IsValidFolder(parent))
            {
                FolderCreate(parent);
            }

            // パスのフォルダを作成。
            AssetDatabase.CreateFolder(parent, folderName);
        }

        /// <summary>
        ///     全てのフォルダのパスを生成して返す。
        /// </summary>
        /// <returns></returns>
        private static string[] GetFolderPaths()
        {
            string artPath = "Arts";
            string animationPath = "Animation";
            string scriptsPath = "Scripts";

            string[] assetsFolders =
                // アセット直下のフォルダ
                new string[] {
                    artPath, "AssetStoreTools", "Editor", "Resources",
                    "ResourcesForAddressable", "Prefabs", "Scenes", scriptsPath,
                    "Settings", "StreamingAssets" }
                
                //Artsフォルダ内のフォルダ
                .Concat(new string[] {
                    animationPath, "Audio", "Models",
                    "Shaders", "Sprites", "Textures" }
                    .Select(s => $"{artPath}/{s}"))
                
                //Animationのフォルダ
                .Concat(new string[] { "Clips", "Controllers", "Timelines" }
                    .Select(s => $"{artPath}/{animationPath}/{s}"))

                //Scriptsのフォルダ
                .Concat(new string[] {"Runtime", "Develop"}
                    .Select(s => $"{scriptsPath}/{s}"))
                .ToArray();


            return assetsFolders;
        }
    }
}
