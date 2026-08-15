using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     AssetStoreToolsフォルダをパッケージ化する。
    /// </summary>
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
        ///     指定ディレクトリを選択された形式で出力する。
        /// </summary>
        /// <remarks>
        ///     指定を等価な手順の並びへ変換してパイプラインへ委譲する。動作は3.5.0までと同じ。
        /// </remarks>
        /// <param name="directories"> 出力対象のディレクトリ。 </param>
        /// <param name="mode"> 個別出力と統合出力の指定。 </param>
        /// <param name="createZip"> 出力後にZIP化するか。 </param>
        /// <param name="usedDependencies"> 使用中アセットと強制包含拡張子だけへ絞るか。 </param>
#pragma warning disable CS0618
        [Obsolete(
            "パイプラインへ移行しました。Export(string[], "
            + nameof(AssetStoreToolsPackagePipeline) + ")を使用してください。",
            error: false)]
        public static void Export(
            string[] directories,
            PackageModeEnum mode,
            bool createZip = false,
            bool usedDependencies = false)
        {
            AssetStoreToolsPackagePlan plan = AssetStoreToolsPackagePipelineRunner.CreatePlan(
                directories,
                CreateStepsFromOptions(mode, createZip, usedDependencies),
                LEGACY_PIPELINE_NAME);

            // 旧APIでも計画を作れない場合は、新しい実行経路へ不完全な入力を渡さない。
            if (plan == null) { return; }

            Export(plan);
        }
#pragma warning restore CS0618

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
        ///     個別または統合パッケージの出力形式を表す。
        /// </summary>
        /// <remarks>
        ///     <see cref="Combine" />を除くと<see cref="Singles" />と<see cref="Nothing" />しか残らず、
        ///     enumとして意味を失うため、次のメジャー更新でenumごと削除する。
        /// </remarks>
        [Flags]
        [Obsolete(
            "パイプラインへ移行しました。" + nameof(AssetStoreToolsPackagePipeline) + "を使用してください。",
            error: false)]
        public enum PackageModeEnum : byte
        {
            #region 外部向けAPI

            /// <summary> パッケージを出力しない無効な状態。 </summary>
            Nothing = 0,

            /// <summary> 選択ディレクトリを個別のパッケージとして出力する。 </summary>
            Singles = 1 << 0,

            /// <summary> 選択ディレクトリを1つの統合パッケージとして出力する。 </summary>
            /// <remarks>
            ///     統合パッケージはディレクトリ単位で取り出せないため、差分インポートの単位にならない。
            ///     この形式で出力してもPackageManifest.jsonは作られない。
            /// </remarks>
            [Obsolete(
                "差分インポートに対応しないため廃止予定です。" + nameof(Singles) + "を使用してください。",
                error: false)]
            Combine = 1 << 1,

            #endregion
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

        /// <summary> 旧来のフラグ指定で組み立てた計画に付ける名前。 </summary>
        private const string LEGACY_PIPELINE_NAME = "Export Mode Options";

        /// <summary>
        ///     旧来のフラグ指定を、等価な手順の並びへ変換する。
        /// </summary>
        /// <remarks>
        ///     並び順は3.5.0までの実行順と一致させている。
        ///     絞り込みはPlan段階のため、Execute段階の手順より先に走る。
        /// </remarks>
        /// <param name="mode"> 個別出力と統合出力の指定。 </param>
        /// <param name="createZip"> 出力後にZIP化するか。 </param>
        /// <param name="usedDependencies"> 使用中アセットと強制包含拡張子だけへ絞るか。 </param>
        /// <returns> 指定に対応する手順の並び。 </returns>
        // 非推奨のPackageModeEnumは削除まで動作を維持する必要があるため、
        // 廃止予定の警告をここでは抑止する。利用側の指定に対しては警告が出る。
#pragma warning disable CS0618
        private static List<AssetStoreToolsPackageStepStrategy> CreateStepsFromOptions(
            PackageModeEnum mode,
            bool createZip,
            bool usedDependencies)
        {
            List<AssetStoreToolsPackageStepStrategy> steps = new();

            // 旧APIの絞り込みはPlan段階で実行する必要があるため、最初の手順へ変換する。
            if (usedDependencies) { steps.Add(new AssetStoreToolsUsedDependenciesStrategy()); }

            // フラグの組み合わせを維持し、個別出力を統合出力より先に実行する。
            if ((mode & PackageModeEnum.Singles) != 0) { steps.Add(new AssetStoreToolsSinglePackageStrategy()); }
            if ((mode & PackageModeEnum.Combine) != 0) { steps.Add(new AssetStoreToolsCombinePackageStrategy()); }

            // ZIP化は先行手順の成果物をまとめるため、常に最後へ置く。
            if (createZip) { steps.Add(new AssetStoreToolsCreateZipStrategy()); }

            return steps;
        }
#pragma warning restore CS0618

        #endregion
    }
}
