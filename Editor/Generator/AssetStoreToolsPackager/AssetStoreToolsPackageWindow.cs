using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;
using SymphonyFrameWork.Editor.SettingProvider;

using UnityEditor;
using UnityEngine;
using static SymphonyFrameWork.Editor.AssetStoreToolsPackager;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Asset Store Toolsの出力対象とパッケージ形式を選択するEditorWindow。
    /// </summary>
    public sealed class AssetStoreToolsPackageWindow : EditorWindow
    {
        #region 外部向けAPI

        /// <summary>
        ///     設定済みパスを検証してPackagerウィンドウを表示する。
        /// </summary>
        public static void ShowWindow()
        {
            string assetStoreToolsPath = AssetStoreToolsPackagerData.AssetStoreToolsPath;

            // 対象パスが未設定なら、操作不能なウィンドウを開かず設定不足を通知する。
            if (string.IsNullOrEmpty(assetStoreToolsPath))
            {
                SymphonyDebugLogger.LogDirect("AssetStoreToolsフォルダのパスが設定されていません。", LogKindEnum.Error);
                return;
            }

            // 設定済みでもUnityが認識できないフォルダなら、出力処理へ進ませない。
            if (!AssetDatabase.IsValidFolder(assetStoreToolsPath))
            {
                SymphonyDebugLogger.LogDirect($"AssetStoreToolsフォルダが存在しません: {assetStoreToolsPath}", LogKindEnum.Error);
                return;
            }
            // 同じ種類のウィンドウを再利用し、複数画面から設定状態が分岐することを防ぐ。
            GetWindow<AssetStoreToolsPackageWindow>(false, "Asset Store Tools Packager", true);
        }

        #endregion

        #region 内部処理

        private static readonly string[] TAB_LABELS = { "Export", "Import" };

        private List<DirectoryItem> _directoryItems = new();
        private Vector2 _scrollPosition;

        private List<AssetStoreToolsPackagePipeline> _pipelines = new();
        private string[] _pipelineLabels = Array.Empty<string>();
        private int _selectedPipelineIndex;

        private PackagerTabEnum _tab = PackagerTabEnum.Export;
        private string _importDirectoryPath = string.Empty;
        private List<AssetStoreToolsImportCandidate> _importCandidates = new();
        private Vector2 _importScrollPosition;
        private bool _hasManifest;

        /// <summary>
        ///     ウィンドウ有効化時に出力対象ディレクトリとパイプラインを読み込む。
        /// </summary>
        private void OnEnable()
        {
            // 表示前にProject Settingsと対象フォルダの現在値を反映する。
            RefreshDirectories();
            RefreshPipelines();

            // Windowの有効期間だけ購読し、無効化後に破棄済みUIを更新しない。
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
        }

        /// <summary>
        ///     ウィンドウ無効化時に取り込み完了の購読を解除する。
        /// </summary>
        private void OnDisable()
        {
            AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;
        }

        /// <summary>
        ///     パッケージの取り込みが完了したら、候補を読み直す。
        /// </summary>
        /// <remarks>
        ///     <c>AssetDatabase.ImportPackage</c> は <c>interactive: false</c> でも
        ///     取り込みの反映が非同期で、呼び出し直後は同梱の<c>ExportedVersion.json</c>が
        ///     まだ更新されていない。そのまま読むと、取り込んだはずのパッケージが
        ///     <c>Updated</c> のまま残る。
        /// </remarks>
        /// <param name="packageName"> 取り込みが完了したパッケージ名。 </param>
        private void OnImportPackageCompleted(string packageName)
        {
            // Export表示中はインポート候補を描画しないため、不要な再読込を避ける。
            if (_tab != PackagerTabEnum.Import) { return; }

            RefreshImportCandidates();
            Repaint();
        }

        /// <summary>
        ///     タブを描画し、選択中のタブの内容へ委譲する。
        /// </summary>
        private void OnGUI()
        {
            GUILayout.Label("Asset Store Tools Packager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            PackagerTabEnum previousTab = _tab;
            _tab = (PackagerTabEnum)GUILayout.Toolbar((int)_tab, TAB_LABELS);

            // タブへ入った時点で最新の状態を読み込む。
            // Importは別プロジェクトで出力した直後に開くことがあり、
            // ExportはProject Settingsのパイプライン配列がウィンドウを開いたまま変わり得るため。
            if (_tab != previousTab)
            {
                switch (_tab)
                {
                    case PackagerTabEnum.Export:
                        RefreshPipelines();
                        break;
                    case PackagerTabEnum.Import:
                        RefreshImportCandidates();
                        break;
                }
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

        /// <summary>
        ///     ディレクトリ選択、出力形式、エクスポート操作を描画する。
        /// </summary>
        private void DrawExportTab()
        {
            // 明示的な更新操作では、対象ディレクトリとパイプラインを同時に読み直す。
            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                RefreshDirectories();
                RefreshPipelines();
            }

            EditorGUILayout.Space();

            // 除外対象を再選択しない範囲で、一括操作を提供する。
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

            // 除外対象は設定を示したまま、個別選択だけを無効化する。
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox);
            foreach (DirectoryItem item in _directoryItems)
            {
                using (new EditorGUI.DisabledGroupScope(item.IsIgnored))
                {
                    item.IsSelected = EditorGUILayout.ToggleLeft(
                        item.IsIgnored ? $"{item.Name} (Ignored)" : item.Name,
                        item.IsSelected);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 実行可能なパイプラインが無ければ、出力操作を描画しない。
            if (!DrawPipelineSelection()) { return; }

            // 出力対象が1件も選ばれていない間は、空の計画を作らせない。
            using (new EditorGUI.DisabledGroupScope(_directoryItems.All(d => !d.IsSelected)))
            {
                if (GUILayout.Button("Export Selected Directories", GUILayout.Height(30)))
                {
                    string[] selectedDirs = _directoryItems
                        .Where(d => d.IsSelected)
                        .Select(d => d.Path)
                        .ToArray();

                    // 出力内容を確定してから提示し、確認ウィンドウの承認を受けて実行する。
                    AssetStoreToolsPackagePlan plan = CreatePlan(
                        selectedDirs,
                        _pipelines[_selectedPipelineIndex]);

                    // 有効な計画だけを確認画面へ渡し、承認された同一インスタンスを出力する。
                    if (plan != null) { AssetStoreToolsPackageConfirmWindow.Open(plan, () => Export(plan)); }
                }
            }
        }

        /// <summary>
        ///     出力パイプラインの選択を描画する。
        /// </summary>
        /// <remarks>
        ///     選択肢の表示名はアセット名。手順の内容は確認ウィンドウで提示する。
        /// </remarks>
        /// <returns> 選択できるパイプラインがあり、出力へ進める場合はtrue。 </returns>
        private bool DrawPipelineSelection()
        {
            // 利用可能なパイプラインが無ければ、設定案内を表示して出力を停止する。
            if (_pipelines.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "出力パイプラインがアサインされていません。\n"
                    + "Project Settings > SymphonyFrameWork > Asset Store Tools Packager で"
                    + "アサインしてください。",
                    MessageType.Info);

                // 設定不足をその場で解消できるよう、該当するProject Settingsへ案内する。
                if (GUILayout.Button("Open Project Settings", GUILayout.Width(180)))
                {
                    SettingsService.OpenProjectSettings(AssetStoreToolsPackagerProvider.SELF_PATH);
                }

                return false;
            }

            _selectedPipelineIndex = EditorGUILayout.Popup(
                "Export Pipeline", _selectedPipelineIndex, _pipelineLabels);

            return true;
        }

        /// <summary>
        ///     出力済みフォルダの選択と、差分インポートの操作を描画する。
        /// </summary>
        private void DrawImportTab()
        {
            // 出力先設定の外にある受け取り済みフォルダも、OSの選択画面から直接指定できるようにする。
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Import Directory");
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.TextField(_importDirectoryPath);
            }
            if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
            {
                string initialDirectory = Directory.Exists(_importDirectoryPath)
                    ? _importDirectoryPath
                    : AssetStoreToolsPackageImporter.GetExportRootPath();
                string selectedDirectory = EditorUtility.OpenFolderPanel(
                    "Select Exported Package Directory", initialDirectory, string.Empty);

                // キャンセル時は現在の選択と候補を維持する。
                if (!string.IsNullOrEmpty(selectedDirectory))
                {
                    _importDirectoryPath = selectedDirectory.Replace('\\', '/');
                    RefreshImportCandidates();
                }
            }
            EditorGUILayout.EndHorizontal();

            // 選択中のディレクトリから、マニフェストと現在のリビジョンを読み直す。
            using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(_importDirectoryPath)))
            {
                if (GUILayout.Button("Refresh", GUILayout.Width(100))) { RefreshImportCandidates(); }
            }

            EditorGUILayout.Space();

            // 取り込み元が未指定なら、候補一覧を描画せず選択方法を案内する。
            if (string.IsNullOrEmpty(_importDirectoryPath))
            {
                EditorGUILayout.HelpBox(
                    "取り込み元のディレクトリが選択されていません。\n"
                    + "Select Folder から PackageManifest.json を含む出力済みディレクトリを選択してください。",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            // 統合パッケージだけの出力は差分単位を持たないため、インポート操作を表示しない。
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

        /// <summary>
        ///     インポート候補の一覧と、一括選択・実行の操作を描画する。
        /// </summary>
        private void DrawImportCandidates()
        {
            // 更新が必要な項目だけの再選択と、全解除を一括操作として提供する。
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
            // マニフェストの順序を維持して、候補ごとの選択状態を描画する。
            foreach (AssetStoreToolsImportCandidate candidate in _importCandidates)
            {
                candidate.IsSelected = EditorGUILayout.ToggleLeft(
                    BuildCandidateLabel(candidate), candidate.IsSelected);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 選択が空の間は、件数0のインポート要求を発生させない。
            int selectedCount = _importCandidates.Count(candidate => candidate.IsSelected);
            using (new EditorGUI.DisabledGroupScope(selectedCount == 0))
            {
                if (GUILayout.Button(
                        $"Import Selected Packages ({selectedCount})", GUILayout.Height(30)))
                {
                    AssetStoreToolsPackageImporter.Import(
                        _importDirectoryPath, _importCandidates);

                    // ここでは選択状態を戻すだけ。リビジョンの反映は非同期なので、
                    // 最終的な状態は importPackageCompleted の購読側で読み直す。
                    RefreshImportCandidates();
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
        ///     選択中の出力済みフォルダからインポート候補を読み直す。
        /// </summary>
        private void RefreshImportCandidates()
        {
            _importCandidates.Clear();
            _hasManifest = false;

            // 取り込み元が未指定なら、空の候補表示を維持する。
            if (string.IsNullOrEmpty(_importDirectoryPath)) { return; }

            _hasManifest = AssetStoreToolsVersionLogStore.LoadManifest(_importDirectoryPath) != null;
            _importCandidates.AddRange(
                AssetStoreToolsPackageImporter.BuildCandidates(_importDirectoryPath));
        }

        /// <summary>
        ///     Project Settingsからパイプラインの一覧を読み直す。
        /// </summary>
        /// <remarks>
        ///     参照が外れたままの要素は選択肢から除く。
        ///     Project Settingsの配列はnullを持てるため。
        /// </remarks>
        private void RefreshPipelines()
        {
            _pipelines.Clear();
            _pipelines.AddRange(
                AssetStoreToolsPackagerData.Pipelines.Where(pipeline => pipeline != null));

            _pipelineLabels = _pipelines
                .Select(pipeline => pipeline.name)
                .ToArray();

            // 削除されたパイプラインを指す選択位置は、利用可能な先頭へ戻す。
            if (_selectedPipelineIndex >= _pipelines.Count) { _selectedPipelineIndex = 0; }
        }

        /// <summary>
        ///     ディレクトリ一覧をリフレッシュして、AssetStoreToolsPackagerから情報を取得する。
        /// </summary>
        private void RefreshDirectories()
        {
            _directoryItems.Clear();

            // 設定の除外状態をUI項目へ写し、除外対象を既定選択から外す。
            IReadOnlyList<PackageDirectoryInfo> infos = GetPackageDirectories();
            foreach (PackageDirectoryInfo info in infos)
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

        /// <summary>
        ///     ウィンドウが持つタブを表す。
        /// </summary>
        private enum PackagerTabEnum
        {
            #region 外部向けAPI

            /// <summary> パッケージを出力する。 </summary>
            Export,

            /// <summary> 出力済みパッケージのうち更新されたものを取り込む。 </summary>
            Import,

            #endregion
        }

        /// <summary>
        ///     パッケージ候補ディレクトリ1件分の表示状態を保持する。
        /// </summary>
        private sealed class DirectoryItem
        {
            #region 外部向けAPI

            /// <summary> パッケージ対象ディレクトリのパス。 </summary>
            public string Path;

            /// <summary> UIへ表示するディレクトリ名。 </summary>
            public string Name;

            /// <summary> ユーザーが出力対象として選択しているかを示す。 </summary>
            public bool IsSelected;

            /// <summary> 除外設定により選択できないかを示す。 </summary>
            public bool IsIgnored;

            #endregion
        }

        #endregion
    }
}
