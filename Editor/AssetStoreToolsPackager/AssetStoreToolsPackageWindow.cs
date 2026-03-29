using SymphonyFrameWork.Core;
using System.Collections.Generic;
using System.Linq;
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

        private class DirectoryItem
        {
            public string Path;
            public string Name;
            public bool IsSelected;
            public bool IsIgnored;
        }

        private List<DirectoryItem> _directoryItems = new List<DirectoryItem>();
        private Vector2 _scrollPosition;
        private bool _createCombinedPackage = false;
        private bool _createZip = false;

        private void OnEnable()
        {
            RefreshDirectories();
        }

        private void OnGUI()
        {
            GUILayout.Label("Asset Store Tools Packager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                RefreshDirectories();
            }

            EditorGUILayout.Space();

            // 一括選択・解除。
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(100)))
            {
                _directoryItems.Where(d => !d.IsIgnored).ToList().ForEach(d => d.IsSelected = true);
            }
            if (GUILayout.Button("Deselect All", GUILayout.Width(100)))
            {
                _directoryItems.ForEach(d => d.IsSelected = false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // ディレクトリ一覧。
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox);
            foreach (DirectoryItem item in _directoryItems)
            {
                using (new EditorGUI.DisabledGroupScope(item.IsIgnored))
                {
                    item.IsSelected = EditorGUILayout.ToggleLeft(item.IsIgnored ? $"{item.Name} (Ignored)" : item.Name, item.IsSelected);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            _createCombinedPackage = EditorGUILayout.ToggleLeft("Create Combined Package", _createCombinedPackage);
            _createZip = EditorGUILayout.ToggleLeft("Create ZIP File", _createZip);

            // エクスポートボタン。
            using (new EditorGUI.DisabledGroupScope(_directoryItems.All(d => !d.IsSelected)))
            {
                if (GUILayout.Button("Export Selected Directories", GUILayout.Height(30)))
                {
                    string[] selectedDirs = _directoryItems
                        .Where(d => d.IsSelected)
                        .Select(d => d.Path)
                        .ToArray();

                    AssetStoreToolsPackager.Export(selectedDirs,
                        _createCombinedPackage,
                        _createZip);
                }
            }
        }

        /// <summary>
        ///     ディレクトリ一覧をリフレッシュして、AssetStoreToolsPackagerから情報を取得する。
        /// </summary>
        private void RefreshDirectories()
        {
            _directoryItems.Clear();

            var infos = AssetStoreToolsPackager.GetPackageDirectories();
            foreach (var info in infos)
            {
                _directoryItems.Add(new DirectoryItem
                {
                    Path = info.Path,
                    Name = info.Name,
                    IsSelected = !info.IsIgnored,
                    IsIgnored = info.IsIgnored
                });
            }
        }
    }
}
