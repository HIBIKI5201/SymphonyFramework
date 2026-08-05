using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

using SymphonyFrameWork.Core;

using UnityEditor;
using UnityEngine;
using static SymphonyFrameWork.Editor.AssetStoreToolsPackager;

namespace SymphonyFrameWork.Editor
{
    /// <summary> Asset Store Toolsの出力対象とパッケージ形式を選択するEditorWindow。 </summary>
    public sealed class AssetStoreToolsPackageWindow : EditorWindow
    {
        /// <summary> 設定済みパスを検証してPackagerウィンドウを表示する。 </summary>
        public static void ShowWindow()
        {
            string assetStoreToolsPath = AssetStoreToolsPackagerData.AssetStoreToolsPath;

            if (string.IsNullOrEmpty(assetStoreToolsPath))
            {
                Debug.LogError("AssetStoreToolsフォルダのパスが設定されていません。");
                return;
            }

            // パッケージ対象ディレクトリをバリデーションチェック。
            if (!AssetDatabase.IsValidFolder(assetStoreToolsPath))
            {
                Debug.LogError($"AssetStoreToolsフォルダが存在しません: {assetStoreToolsPath}");
                return;
            }


            GetWindow<AssetStoreToolsPackageWindow>(false, "Asset Store Tools Packager", true);
        }

        private sealed class DirectoryItem
        {
            /// <summary> パッケージ対象ディレクトリのパス。 </summary>
            public string Path;

            /// <summary> UIへ表示するディレクトリ名。 </summary>
            public string Name;

            /// <summary> ユーザーが出力対象として選択しているかを示す。 </summary>
            public bool IsSelected;

            /// <summary> 除外設定により選択できないかを示す。 </summary>
            public bool IsIgnored;
        }

        /// <summary> ウィンドウが持つタブ。 </summary>
        private enum PackagerTabEnum
        {
            /// <summary> パッケージを出力する。 </summary>
            Export,

            /// <summary> 出力済みパッケージのうち更新されたものを取り込む。 </summary>
            Import,
        }

        private static readonly string[] TAB_LABELS = { "Export", "Import" };

        private List<DirectoryItem> _directoryItems = new();
        private Vector2 _scrollPosition;
        private PackageModeEnum _packageMode = PackageModeEnum.Singles;
        private bool _createZip = false;
        private bool _usedDependencies = false;

        private PackagerTabEnum _tab = PackagerTabEnum.Export;
        private string[] _exportDirectories = Array.Empty<string>();
        private string[] _exportDirectoryLabels = Array.Empty<string>();
        private int _selectedExportIndex;
        private List<AssetStoreToolsImportCandidate> _importCandidates = new();
        private Vector2 _importScrollPosition;
        private bool _hasManifest;

        /// <summary> ウィンドウ有効化時に出力対象ディレクトリ一覧を読み込む。 </summary>
        private void OnEnable()
        {
            RefreshDirectories();
        }

        /// <summary> タブを描画し、選択中のタブの内容へ委譲する。 </summary>
        private void OnGUI()
        {
            GUILayout.Label("Asset Store Tools Packager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            PackagerTabEnum previousTab = _tab;
            _tab = (PackagerTabEnum)GUILayout.Toolbar((int)_tab, TAB_LABELS);

            // Importタブへ入った時点で最新の状態を読み込む。
            // 別プロジェクトで出力した直後に開くことがあるため、表示のたびに読み直す。
            if (_tab != previousTab && _tab == PackagerTabEnum.Import)
            {
                RefreshImportCandidates();
            }

            EditorGUILayout.Space();

            switch (_tab)
            {
                case PackagerTabEnum.Export:
                    DrawExportTab();
                    break;
                case PackagerTabEnum.Import:
                    DrawImportTab();
                    break;
            }
        }

        /// <summary> ディレクトリ選択、出力形式、エクスポート操作を描画する。 </summary>
        private void DrawExportTab()
        {
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
            _packageMode = (PackageModeEnum)EditorGUILayout.EnumFlagsField("Export Mode", _packageMode);
            DrawCombineDeprecationHelp();
            _createZip = EditorGUILayout.ToggleLeft("Create ZIP File", _createZip);
            _usedDependencies = EditorGUILayout.ToggleLeft("Used Dependencies", _usedDependencies);

            // エクスポートボタン。
            using (new EditorGUI.DisabledGroupScope(_directoryItems.All(d => !d.IsSelected)))
            {
                if (_packageMode == PackageModeEnum.Nothing)
                {
                    GUILayout.TextField("Noting mode is invalid");
                    return;
                }

                if (GUILayout.Button("Export Selected Directories", GUILayout.Height(30)))
                {
                    string[] selectedDirs = _directoryItems
                        .Where(d => d.IsSelected)
                        .Select(d => d.Path)
                        .ToArray();

                    // 出力内容を確定してから提示し、確認ウィンドウの承認を受けて実行する。
                    AssetStoreToolsPackagePlan plan = CreatePlan(
                        selectedDirs,
                        _packageMode,
                        _createZip,
                        _usedDependencies);

                    if (plan != null)
                    {
                        AssetStoreToolsPackageConfirmWindow.Open(plan, () => Export(plan));
                    }
                }
            }
        }

        /// <summary>
        ///     出力済みフォルダの選択と、差分インポートの操作を描画する。
        /// </summary>
        private void DrawImportTab()
        {
            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                RefreshImportCandidates();
            }

            EditorGUILayout.Space();

            if (_exportDirectories.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "出力済みのパッケージがありません。\n"
                    + "Export タブで Export Mode = Singles として出力してください。",
                    MessageType.Info);
                return;
            }

            int selectedIndex = EditorGUILayout.Popup(
                "Exported Packages", _selectedExportIndex, _exportDirectoryLabels);
            if (selectedIndex != _selectedExportIndex)
            {
                _selectedExportIndex = selectedIndex;
                RefreshImportCandidates(keepSelectedIndex: true);
            }

            EditorGUILayout.Space();

            if (!_hasManifest)
            {
                EditorGUILayout.HelpBox(
                    $"{EditorSymphonyConstant.ASSET_STORE_TOOLS_MANIFEST_FILE_NAME} がありません。\n"
                    + "統合パッケージだけで出力した場合、差分インポートの対象になりません。",
                    MessageType.Warning);
                return;
            }

            DrawImportCandidates();
        }

        /// <summary> インポート候補の一覧と、一括選択・実行の操作を描画する。 </summary>
        private void DrawImportCandidates()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Updated", GUILayout.Width(120)))
            {
                _importCandidates.ForEach(candidate =>
                    candidate.IsSelected =
                        AssetStoreToolsImportPlanner.IsSelectedByDefault(candidate.State));
            }
            if (GUILayout.Button("Deselect All", GUILayout.Width(100)))
            {
                _importCandidates.ForEach(candidate => candidate.IsSelected = false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            _importScrollPosition = EditorGUILayout.BeginScrollView(
                _importScrollPosition, EditorStyles.helpBox);
            foreach (AssetStoreToolsImportCandidate candidate in _importCandidates)
            {
                candidate.IsSelected = EditorGUILayout.ToggleLeft(
                    BuildCandidateLabel(candidate), candidate.IsSelected);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            int selectedCount = _importCandidates.Count(candidate => candidate.IsSelected);
            using (new EditorGUI.DisabledGroupScope(selectedCount == 0))
            {
                if (GUILayout.Button(
                        $"Import Selected Packages ({selectedCount})", GUILayout.Height(30)))
                {
                    AssetStoreToolsPackageImporter.Import(
                        _exportDirectories[_selectedExportIndex], _importCandidates);
                    RefreshImportCandidates(keepSelectedIndex: true);
                }
            }
        }

        /// <summary>
        ///     候補1件分の表示名を組み立てる。
        /// </summary>
        /// <param name="candidate"> 表示するインポート候補。 </param>
        /// <returns> 名前、状態、リビジョンの変化を含む表示名。 </returns>
        private static string BuildCandidateLabel(AssetStoreToolsImportCandidate candidate)
        {
            string versionText = candidate.LocalVersion == null
                ? $"→ v{candidate.ManifestVersion}"
                : $"v{candidate.LocalVersion.Value} → v{candidate.ManifestVersion}";

            return $"{candidate.Name}  [{candidate.State}]  {versionText}";
        }

        /// <summary>
        ///     出力済みフォルダの一覧とインポート候補を読み直す。
        /// </summary>
        /// <param name="keepSelectedIndex">
        ///     選択中の出力済みフォルダを維持するか。折りたたみ以外の操作で呼ぶ場合はtrue。
        /// </param>
        private void RefreshImportCandidates(bool keepSelectedIndex = false)
        {
            _exportDirectories = AssetStoreToolsPackageImporter.GetExportDirectories().ToArray();
            _exportDirectoryLabels = _exportDirectories
                .Select(Path.GetFileName)
                .ToArray();

            if (!keepSelectedIndex || _selectedExportIndex >= _exportDirectories.Length)
            {
                _selectedExportIndex = 0;
            }

            _importCandidates.Clear();
            _hasManifest = false;

            if (_exportDirectories.Length == 0)
            {
                return;
            }

            string exportDirectory = _exportDirectories[_selectedExportIndex];
            _hasManifest = AssetStoreToolsVersionLogStore.LoadManifest(exportDirectory) != null;
            _importCandidates.AddRange(
                AssetStoreToolsPackageImporter.BuildCandidates(exportDirectory));
        }

        /// <summary>
        ///     Combineが選択されている場合に、廃止予定であることを表示する。
        /// </summary>
        /// <remarks>
        ///     選択自体は禁止しない。既存の運用を突然壊さず、移行期間を設けるため。
        /// </remarks>
        private void DrawCombineDeprecationHelp()
        {
#pragma warning disable CS0618
            if ((_packageMode & PackageModeEnum.Combine) == 0)
            {
                return;
            }
#pragma warning restore CS0618

            EditorGUILayout.HelpBox(
                "Combine は廃止予定です。Singles を使用してください。\n"
                + "統合パッケージはディレクトリ単位で取り出せないため、差分インポートの対象になりません。"
                + "Combine だけを指定した場合、PackageManifest.json は作られません。",
                MessageType.Warning);
        }

        /// <summary>
        ///     ディレクトリ一覧をリフレッシュして、AssetStoreToolsPackagerから情報を取得する。
        /// </summary>
        private void RefreshDirectories()
        {
            _directoryItems.Clear();

            var infos = GetPackageDirectories();
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
