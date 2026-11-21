using SymphonyFramework.Core;
using SymphonyFramework.Debugger.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     AssetStoreToolsフォルダをパッケージ化する。
    /// </summary>
    // 6.x の名前（SymphonyFrameWork.Editor）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Editor",
        sourceAssembly: "SymphonyFrameWork.Editor")]
    public static class AssetStoreToolsPackager
    {
        #region 外部向けAPI

        /// <summary>
        ///     AssetStoreToolsのパッケージ出力ウィンドウを開く。
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(ExportAssetStoreToolsFolder), priority = 100)]
        public static void ExportAssetStoreToolsFolder()
        {
            // 設定検証とウィンドウの再利用は表示側へ集約する。
            AssetStoreToolsPackageWindow.ShowWindow();
        }

        /// <summary>
        ///     パッケージ対象のディレクトリ一覧を取得する。
        /// </summary>
        /// <returns> 除外状態を含む直下ディレクトリの一覧。 </returns>
        public static IReadOnlyList<PackageDirectoryInfo> GetPackageDirectories()
        {
            List<PackageDirectoryInfo> results = new();

            // 対象フォルダが未設定か無効なら、ファイルI/Oを行わず空で返す。
            if (!AssetDatabase.IsValidFolder(AssetStoreToolsPackagerData.AssetStoreToolsPath)) { return results; }

            // 設定ファイルの確認と作成。読み込めない場合は除外設定が失われるため何も返さない。
            AssetStoreToolsPackagerConfig config = AssetStoreToolsPackagerConfigStore.Load();
            // 除外設定を確定できない状態では、誤って対象を出力しない。
            if (config == null) { return results; }

            HashSet<string> ignoredNames = new(config.IgnoredDirectories, StringComparer.OrdinalIgnoreCase);

            // 設定ファイル自身を含むルート直下のファイルは対象にせず、ディレクトリだけを列挙する。
            string[] dirs = Directory.GetDirectories(AssetStoreToolsPackagerData.AssetStoreToolsPath);
            foreach (string dir in dirs)
            {
                string name = Path.GetFileName(dir);
                bool isIgnored = ignoredNames.Contains(name);

                results.Add(new PackageDirectoryInfo
                {
                    Path = dir.Replace("\\", "/"),
                    Name = name,
                    IsIgnored = isIgnored
                });
            }

            return results;
        }

        /// <summary>
        ///     指定ディレクトリをパイプラインの手順どおりに出力する。
        /// </summary>
        /// <param name="directories"> 出力対象のディレクトリ。 </param>
        /// <param name="pipeline"> 実行する手順を持つパイプライン。 </param>
        public static void Export(string[] directories, AssetStoreToolsPackagePipeline pipeline)
        {
            AssetStoreToolsPackagePlan plan = CreatePlan(directories, pipeline);
            // 設定不足や対象無しで計画を作れない場合は、出力処理へ進まない。
            if (plan == null) { return; }

            Export(plan);
        }

        /// <summary>
        ///     出力内容を確定した計画を組み立てる。
        /// </summary>
        /// <remarks> この時点ではファイルを出力しない。 </remarks>
        /// <param name="directories"> 出力対象のディレクトリ。 </param>
        /// <param name="pipeline"> 実行する手順を持つパイプライン。 </param>
        /// <returns> 出力計画。対象が無い場合や設定を読み込めない場合はnull。 </returns>
        internal static AssetStoreToolsPackagePlan CreatePlan(
            string[] directories,
            AssetStoreToolsPackagePipeline pipeline)
        {
            // パイプライン無しでは手順の順序と出力形式を確定できない。
            if (pipeline == null)
            {
                SymphonyDebugLogger.LogDirect($"[{nameof(AssetStoreToolsPackager)}]\nパイプラインが指定されていません。", LogKindEnum.Error);
                return null;
            }

            return AssetStoreToolsPackagePipelineRunner.CreatePlan(
                directories, pipeline.Steps, pipeline.name);
        }

        /// <summary>
        ///     確定済みの計画に従ってパイプラインを実行する。
        /// </summary>
        /// <param name="plan"> 出力する計画。 </param>
        internal static void Export(AssetStoreToolsPackagePlan plan)
        {
            // 計画作成時に確定した手順と対象を変更せずランナーへ渡す。
            AssetStoreToolsPackagePipelineRunner.Export(plan);
        }

        /// <summary>
        ///     パスの拡張子が強制包含の一覧に含まれるか判定する。
        /// </summary>
        /// <param name="path"> 判定するアセットのパス。 </param>
        /// <param name="forceIncludeExtensions"> 強制包含する拡張子の一覧。 </param>
        /// <returns> 大文字小文字を無視して一致する拡張子があればtrue。 </returns>
        internal static bool HasForceIncludeExtension(
            string path,
            IReadOnlyList<string> forceIncludeExtensions)
        {
            // 強制包含規則が無ければ、通常の依存関係判定だけへ委ねる。
            if (forceIncludeExtensions == null || forceIncludeExtensions.Count == 0) { return false; }

            string extension = Path.GetExtension(path);
            // 拡張子の無いアセットは、拡張子規則では強制包含できない。
            if (string.IsNullOrEmpty(extension)) { return false; }

            // プラットフォーム間で拡張子の大文字小文字が異なっても同じ規則として扱う。
            for (int i = 0; i < forceIncludeExtensions.Count; i++)
            {
                if (string.Equals(
                        extension,
                        forceIncludeExtensions[i],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     パッケージ候補ディレクトリの表示名、パス、除外状態を保持する。
        /// </summary>
        public sealed class PackageDirectoryInfo
        {
            #region 外部向けAPI

            /// <summary> パッケージ対象ディレクトリのパス。 </summary>
            public string Path;

            /// <summary> UIへ表示するディレクトリ名。 </summary>
            public string Name;

            /// <summary> 除外設定に含まれているかを示す。 </summary>
            public bool IsIgnored;

            #endregion
        }

        #endregion

        #region 内部処理

        #endregion
    }
}
