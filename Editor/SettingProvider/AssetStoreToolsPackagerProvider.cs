using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor.SettingProvider
{
    /// <summary> Asset Store Tools PackagerのProject Settings画面を提供する。 </summary>
    public sealed class AssetStoreToolsPackagerProvider
    {
        /// <summary> Project Settingsに表示する設定項目名。 </summary>
        public const string LABEL = "Asset Store Tools Packager";

        /// <summary> SettingsProviderの完全な設定パス。 </summary>
        public const string SELF_PATH = SymphonySettingProvider.PROVIDER_PATH + LABEL;

        /// <summary> Packager設定用のSettingsProviderを生成する。 </summary>
        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
        {
            // SettingsScope.Projectを指定することでProject Settingsに項目を追加できる
            var provider = new SettingsProvider(SELF_PATH, SettingsScope.Project)
            {
                // 項目のタイトル
                label = LABEL,

                // どのように描画するか(IMGUI)
                guiHandler = IMGUI,

                // 画面を開いた時点の設定ファイルを読み込む
                activateHandler = (_, _) => Reload(),

                // 検索するときのキーワード
                keywords = new HashSet<string>(new[]
                {
                    "asset", "store", "tools", "packager", "ignore", "extension"
                }),
            };

            return provider;
        }

        /// <summary> 編集中の設定。設定ファイルを読み込めない場合はnull。 </summary>
        private static AssetStoreToolsPackagerConfig _config;

        /// <summary> 設定ファイルへ保存していない変更があるかを示す。 </summary>
        private static bool _isDirty;

        /// <summary> 現在の編集内容を読み込んだ設定ファイルのパス。 </summary>
        private static string _loadedConfigPath;

        /// <summary> Packagerが使用する入出力パスと、パッケージ化設定を描画する。 </summary>
        private static void IMGUI(string searchContext)
        {
            string assetStoreToolsPath = AssetStoreToolsPackagerData.AssetStoreToolsPath;
            assetStoreToolsPath = EditorGUILayout.TextField("Asset Store Tools Path", assetStoreToolsPath);
            if (assetStoreToolsPath != AssetStoreToolsPackagerData.AssetStoreToolsPath)
            {
                // 設定ファイルはここで読み直さない。入力途中のパスごとに
                // 設定ファイルを生成してしまうため、Reloadボタンでの明示的な操作に任せる。
                AssetStoreToolsPackagerData.SetAssetStoreToolsPath(assetStoreToolsPath);
            }

            string exportedPackagesPath = AssetStoreToolsPackagerData.ExportedPackagesPath;
            exportedPackagesPath = EditorGUILayout.TextField("Exported Packages Path", exportedPackagesPath);
            if (exportedPackagesPath != AssetStoreToolsPackagerData.ExportedPackagesPath)
            {
                AssetStoreToolsPackagerData.SetExportedPackagesPath(exportedPackagesPath);
            }

            EditorGUILayout.Space();
            DrawConfig();
        }

        /// <summary> パッケージ化設定ファイルの内容を描画する。 </summary>
        private static void DrawConfig()
        {
            EditorGUILayout.LabelField("Packager Config", EditorStyles.boldLabel);

            string configPath = AssetStoreToolsPackagerConfigStore.GetConfigFilePath();
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.TextField("Config File", configPath);
            }

            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "設定ファイルを読み込めませんでした。Consoleのエラーを確認してください。",
                    MessageType.Error);

                if (GUILayout.Button("Reload", GUILayout.Width(100)))
                {
                    Reload();
                }

                return;
            }

            bool isPathChanged = configPath != _loadedConfigPath;
            if (isPathChanged)
            {
                EditorGUILayout.HelpBox(
                    $"対象フォルダのパスが変更されています。Reloadを押すとこのパスの設定を読み込みます。\n読み込み中: {_loadedConfigPath}",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();
            bool isChanged = DrawStringList("Ignored Directories", _config.IgnoredDirectories);

            EditorGUILayout.Space();
            isChanged |= DrawStringList("Force Include Extensions", _config.ForceIncludeExtensions);

            if (isChanged)
            {
                _isDirty = true;
            }

            EditorGUILayout.Space();

            if (_isDirty)
            {
                EditorGUILayout.HelpBox("保存していない変更があります。", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledGroupScope(!_isDirty || isPathChanged))
            {
                if (GUILayout.Button("Save", GUILayout.Width(100)))
                {
                    AssetStoreToolsPackagerConfigStore.Save(_config);
                    Reload();
                }
            }

            if (GUILayout.Button("Reload", GUILayout.Width(100)))
            {
                Reload();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        ///     文字列の一覧を要素ごとのテキストフィールドと追加・削除ボタンで描画する。
        /// </summary>
        /// <param name="label"> 一覧の見出し。 </param>
        /// <param name="values"> 描画と編集の対象となる一覧。 </param>
        /// <returns> 一覧の内容が変更された場合はtrue。 </returns>
        private static bool DrawStringList(string label, List<string> values)
        {
            EditorGUILayout.LabelField(label);

            bool isChanged = false;
            int removeIndex = -1;

            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < values.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    string edited = EditorGUILayout.TextField(values[i]);
                    if (edited != values[i])
                    {
                        values[i] = edited;
                        isChanged = true;
                    }

                    if (GUILayout.Button("-", GUILayout.Width(24)))
                    {
                        removeIndex = i;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (removeIndex >= 0)
                {
                    values.RemoveAt(removeIndex);
                    isChanged = true;
                }

                if (GUILayout.Button("Add", GUILayout.Width(100)))
                {
                    values.Add(string.Empty);
                    isChanged = true;
                }
            }

            return isChanged;
        }

        /// <summary> 設定ファイルを読み直し、未保存の変更を破棄する。 </summary>
        private static void Reload()
        {
            _loadedConfigPath = AssetStoreToolsPackagerConfigStore.GetConfigFilePath();
            _config = AssetStoreToolsPackagerConfigStore.Load();
            _isDirty = false;
        }
    }
}
