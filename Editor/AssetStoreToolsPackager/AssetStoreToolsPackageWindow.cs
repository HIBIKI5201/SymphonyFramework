using SymphonyFrameWork.Core;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    public class AssetStoreToolsPackageWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            // パッケージ対象ディレクトリをバリデーションチェック。
            if (!AssetDatabase.IsValidFolder(EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH))
            {
                Debug.LogError($"AssetStoreToolsフォルダが存在しません: {EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH}");
                return;
            }


            GetWindow<AssetStoreToolsPackageWindow>(false, "Asset Store Tools Packager", true);
        }

        private string[] _directories;

        private void OnEnable()
        {
            // アセットごとのパスを生成。
            _directories = Directory.GetDirectories(EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH);
        }

        private void OnGUI()
        {
            GUILayout.Label("Asset Store Tools Packager", EditorStyles.boldLabel);
            if (GUILayout.Button("Export Asset Store Tools Folder"))
            {
                AssetStoreToolsPackager.Export(_directories);
            }
        }
    }
}
