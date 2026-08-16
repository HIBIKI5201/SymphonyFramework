using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor.SettingProvider
{
    /// <summary>
    ///     Asset Store Tools PackagerのProject Settings画面を提供する。
    /// </summary>
    public sealed class AssetStoreToolsPackagerProvider
    {
        #region 外部向けAPI

        /// <summary> Project Settingsに表示する設定項目名。 </summary>
        public const string LABEL = "Asset Store Tools Packager";

        /// <summary> SettingsProviderの完全な設定パス。 </summary>
        public const string SELF_PATH = SymphonySettingProvider.PROVIDER_PATH + LABEL;

        /// <summary>
        ///     Packager設定用のSettingsProviderを生成する。
        /// </summary>
        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
        {
            // Project単位の入口へ、設定画面の描画処理と検索語を登録する。
            SettingsProvider provider = new(SELF_PATH, SettingsScope.Project)
            {
                label = LABEL,
                guiHandler = IMGUI,

                // 画面を開くたびにディスク上の設定を読み、別の操作で行われた変更を反映する。
                activateHandler = (_, _) => Reload(),
                keywords = new HashSet<string>(new[]
                {
                    "asset", "store", "tools", "packager", "ignore", "extension", "pipeline"
                }),
            };

            return provider;
        }

        #endregion

        #region 内部処理

        /// <summary> 編集中の設定。 </summary>
        /// <remarks> 設定ファイルを読み込めない場合はnullを保持する。 </remarks>
        private static AssetStoreToolsPackagerConfig _config;

        /// <summary> 設定ファイルへ保存していない変更があるかを示す。 </summary>
        private static bool _isDirty;

        /// <summary> 現在の編集内容を読み込んだ設定ファイルのパス。 </summary>
        private static string _loadedConfigPath;

        /// <summary>
        ///     Packagerが使用する入出力パスとパッケージ化設定を描画する。
        /// </summary>
        private static void IMGUI(string searchContext)
        {
            SymphonyDocumentationGUI.DrawOpenButton(SymphonyDocumentPageEnum.AssetStoreToolsPackager);

            // 入力途中のパスごとに設定ファイルを生成しないよう、パスだけを保存してReloadは明示操作に任せる。
            string assetStoreToolsPath = AssetStoreToolsPackagerData.AssetStoreToolsPath;
            assetStoreToolsPath = EditorGUILayout.TextField("Asset Store Tools Path", assetStoreToolsPath);
            if (assetStoreToolsPath != AssetStoreToolsPackagerData.AssetStoreToolsPath)
            {
                AssetStoreToolsPackagerData.SetAssetStoreToolsPath(assetStoreToolsPath);
            }

            // 出力先は設定値が変わった場合だけ永続化する。
            string exportedPackagesPath = AssetStoreToolsPackagerData.ExportedPackagesPath;
            exportedPackagesPath = EditorGUILayout.TextField("Exported Packages Path", exportedPackagesPath);
            if (exportedPackagesPath != AssetStoreToolsPackagerData.ExportedPackagesPath)
            {
                AssetStoreToolsPackagerData.SetExportedPackagesPath(exportedPackagesPath);
            }

            // 実行順に合わせ、出力パイプラインの次に対象ファイルの設定を描画する。
            EditorGUILayout.Space();
            DrawPipelines();

            EditorGUILayout.Space();
            DrawConfig();
        }

        /// <summary>
        ///     Packagerウィンドウで選べる出力パイプラインの一覧を描画する。
        /// </summary>
        /// <remarks>
        ///     ProjectSettingsへ保存する設定のため、パスの項目と同じく変更した時点で保存する。
        ///     PackagerConfig.jsonのようなSave／Reloadは設けない。
        /// </remarks>
        private static void DrawPipelines()
        {
            EditorGUILayout.LabelField("Export Pipelines", EditorStyles.boldLabel);

            List<AssetStoreToolsPackagePipeline> pipelines =
                new(AssetStoreToolsPackagerData.Pipelines);

            bool isChanged = false;
            int removeIndex = -1;

            using (new EditorGUI.IndentLevelScope())
            {
                // 登録済みパイプラインを順に描画し、各要素の差し替えまたは削除指定を受け付ける。
                for (int i = 0; i < pipelines.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    // 参照が変わった要素だけを差し替え、描画ループの後で一覧を一括保存する。
                    AssetStoreToolsPackagePipeline edited = (AssetStoreToolsPackagePipeline)EditorGUILayout.ObjectField(
                        pipelines[i], typeof(AssetStoreToolsPackagePipeline), false);
                    if (edited != pipelines[i])
                    {
                        pipelines[i] = edited;
                        isChanged = true;
                    }

                    // 反復中に一覧を変更すると添字がずれるため、削除対象だけを記録する。
                    if (GUILayout.Button("-", GUILayout.Width(24))) { removeIndex = i; }

                    EditorGUILayout.EndHorizontal();
                }

                // 描画が終わってから記録済みの1件を削除し、反復中のコレクション変更を避ける。
                if (removeIndex >= 0)
                {
                    pipelines.RemoveAt(removeIndex);
                    isChanged = true;
                }

                EditorGUILayout.BeginHorizontal();
                // 未設定のスロットを追加し、次の描画からObjectFieldで選べるようにする。
                if (GUILayout.Button("Add", GUILayout.Width(100)))
                {
                    pipelines.Add(null);
                    isChanged = true;
                }

                // 明示操作された場合だけテンプレートアセットを生成し、利用側へ意図しないアセットを増やさない。
                if (GUILayout.Button("Create Default Pipeline", GUILayout.Width(180)))
                {
                    AssetStoreToolsPackagePipeline created = CreateDefaultPipelineAsset();
                    // 生成に成功した場合だけ一覧へ追加し、失敗時のnull参照を保存しない。
                    if (created != null)
                    {
                        pipelines.Add(created);
                        isChanged = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            // 変更された一覧だけをProjectSettingsへ保存する。
            if (isChanged) { AssetStoreToolsPackagerData.SetPipelines(pipelines); }

            // 実行可能なパイプラインが無い場合は、既定テンプレートの生成方法を案内する。
            if (pipelines.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "出力パイプラインがアサインされていません。"
                    + "Create Default Pipeline を押すと、Singles → Used Dependencies → Create ZIP の"
                    + "テンプレートを生成してアサインします。",
                    MessageType.Info);
            }
        }

        /// <summary>
        ///     既定テンプレートのパイプラインアセットを生成する。
        /// </summary>
        /// <remarks>
        ///     Frameworkの起動時に自動生成しない。利用側のリポジトリへ意図しないアセットを増やさないため、
        ///     このボタンの明示的な操作でだけ生成する。
        /// </remarks>
        /// <returns> 生成したアセット。生成に失敗した場合はnull。 </returns>
        private static AssetStoreToolsPackagePipeline CreateDefaultPipelineAsset()
        {
            // 利用側が明示的に生成を選んだ時点で、Editor用Resourcesの保存先を用意する。
            string directory = EditorSymphonyConstant.RESOURCES_EDITOR_PATH;

            try
            {
                Directory.CreateDirectory(directory);
            }
            // IMGUIの描画中に例外を投げるとProject Settings画面が壊れるため、
            // CreateDirectoryが投げうるものをまとめて捕まえる。
            catch (Exception exception) when (
                exception is IOException
                    or UnauthorizedAccessException
                    or ArgumentException
                    or NotSupportedException)
            {
                SymphonyDebugLogger.LogDirect(
                    $"[{nameof(AssetStoreToolsPackagerProvider)}]\n"
                    + $"パイプラインの配置先を生成できませんでした: {directory}\n{exception.Message}", LogKindEnum.Error);
                return null;
            }

            // ファイルシステムで作成したフォルダをAssetDatabaseへ認識させてからアセットを作る。
            AssetDatabase.Refresh();

            // 同名のアセットがある場合は上書きせず別名で作る。
            // 利用側が手を入れたパイプラインを、ボタンの誤操作で失わせないため。
            string path = AssetDatabase.GenerateUniqueAssetPath(
                directory + "/" + nameof(AssetStoreToolsPackagePipeline) + ".asset");

            // 一意な保存先へテンプレートを永続化し、生成結果をProjectへ即時反映する。
            AssetStoreToolsPackagePipeline pipeline = AssetStoreToolsPackagePipeline.CreateTemplate();
            AssetDatabase.CreateAsset(pipeline, path);
            AssetDatabase.SaveAssets();

            SymphonyDebugLogger.LogDirect(
                $"[{nameof(AssetStoreToolsPackagerProvider)}]\n"
                + $"既定のパイプラインを生成しました: {path}");

            return pipeline;
        }

        /// <summary>
        ///     パッケージ化設定ファイルの内容を描画する。
        /// </summary>
        private static void DrawConfig()
        {
            EditorGUILayout.LabelField("Packager Config", EditorStyles.boldLabel);

            string configPath = AssetStoreToolsPackagerConfigStore.GetConfigFilePath();
            using (new EditorGUI.DisabledGroupScope(true)) { EditorGUILayout.TextField("Config File", configPath); }

            // 読み込みに失敗した場合は編集を止め、再読み込みだけを許可する。
            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "設定ファイルを読み込めませんでした。Consoleのエラーを確認してください。",
                    MessageType.Error);

                if (GUILayout.Button("Reload", GUILayout.Width(100))) { Reload(); }

                return;
            }

            // 編集中に対象パスが変わった場合は、別ファイルへの誤保存を防ぐためSaveを無効化する。
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

            // いずれかの一覧が変わった場合だけ未保存状態へ移行する。
            if (isChanged) { _isDirty = true; }

            EditorGUILayout.Space();

            if (_isDirty) { EditorGUILayout.HelpBox("保存していない変更があります。", MessageType.Info); }

            // 読み込み元と現在のパスが一致し、未保存変更がある場合だけSaveを許可する。
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledGroupScope(!_isDirty || isPathChanged))
            {
                if (GUILayout.Button("Save", GUILayout.Width(100)))
                {
                    AssetStoreToolsPackagerConfigStore.Save(_config);
                    Reload();
                }
            }

            if (GUILayout.Button("Reload", GUILayout.Width(100))) { Reload(); }
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
                // 既存値を順に描画し、各要素の編集または削除指定を受け付ける。
                for (int i = 0; i < values.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    // 編集された要素だけを差し替え、描画後に呼び出し側へ変更有無を返す。
                    string edited = EditorGUILayout.TextField(values[i]);
                    if (edited != values[i])
                    {
                        values[i] = edited;
                        isChanged = true;
                    }

                    // 反復中に一覧を変更すると添字がずれるため、削除対象だけを記録する。
                    if (GUILayout.Button("-", GUILayout.Width(24))) { removeIndex = i; }

                    EditorGUILayout.EndHorizontal();
                }

                // 描画が終わってから記録済みの1件を削除する。
                if (removeIndex >= 0)
                {
                    values.RemoveAt(removeIndex);
                    isChanged = true;
                }

                // 空文字列の要素を末尾へ追加し、次の描画から編集可能にする。
                if (GUILayout.Button("Add", GUILayout.Width(100)))
                {
                    values.Add(string.Empty);
                    isChanged = true;
                }
            }

            return isChanged;
        }

        /// <summary>
        ///     設定ファイルを読み直し、未保存の変更を破棄する。
        /// </summary>
        private static void Reload()
        {
            // 読み込んだパスと設定を同時に更新し、以後のSave可否判定を同じスナップショットへ揃える。
            _loadedConfigPath = AssetStoreToolsPackagerConfigStore.GetConfigFilePath();
            _config = AssetStoreToolsPackagerConfigStore.Load();
            _isDirty = false;
        }

        #endregion
    }
}
