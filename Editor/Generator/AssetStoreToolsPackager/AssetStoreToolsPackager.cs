using SymphonyFrameWork.Core;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     AssetStoreToolsフォルダをパッケージ化するクラス。
    /// </summary>
    public static class AssetStoreToolsPackager
    {
        /// <summary> パッケージ候補ディレクトリの表示名、パス、除外状態を保持する。 </summary>
        public sealed class PackageDirectoryInfo
        {
            /// <summary> パッケージ対象ディレクトリのパス。 </summary>
            public string Path;

            /// <summary> UIへ表示するディレクトリ名。 </summary>
            public string Name;

            /// <summary> 除外設定に含まれているかを示す。 </summary>
            public bool IsIgnored;
        }

        /// <summary> 個別または統合パッケージの出力形式を表すフラグ。 </summary>
        [Flags]
        public enum PackageModeEnum : byte
        {
            /// <summary> パッケージを出力しない無効な状態。 </summary>
            Nothing = 0,

            /// <summary> 選択ディレクトリを個別のパッケージとして出力する。 </summary>
            Singles = 1 << 0,

            /// <summary>
            ///     選択ディレクトリを1つの統合パッケージとして出力する。
            /// </summary>
            /// <remarks>
            ///     統合パッケージはディレクトリ単位で取り出せないため、差分インポートの単位にならない。
            ///     この形式で出力してもPackageManifest.jsonは作られない。
            /// </remarks>
            [Obsolete(
                "差分インポートに対応しないため廃止予定です。" + nameof(Singles) + "を使用してください。",
                error: false)]
            Combine = 1 << 1,
        }

        /// <summary>
        ///     AssetStoreToolsフォルダをパッケージ化してExportedPackagesフォルダに保存します。
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(ExportAssetStoreToolsFolder), priority = 100)]
        public static void ExportAssetStoreToolsFolder()
        {
            AssetStoreToolsPackageWindow.ShowWindow();
        }

        /// <summary>
        ///     パッケージ対象のディレクトリ一覧を取得します。無視リストのフィルタリングもここで行います。
        /// </summary>
        public static IReadOnlyList<PackageDirectoryInfo> GetPackageDirectories()
        {
            List<PackageDirectoryInfo> results = new();

            if (!AssetDatabase.IsValidFolder(AssetStoreToolsPackagerData.AssetStoreToolsPath))
            {
                return results;
            }

            // 設定ファイルの確認と作成。読み込めない場合は除外設定が失われるため何も返さない。
            AssetStoreToolsPackagerConfig config = AssetStoreToolsPackagerConfigStore.Load();
            if (config == null)
            {
                return results;
            }

            HashSet<string> ignoredNames = new(config.IgnoredDirectories, StringComparer.OrdinalIgnoreCase);

            // ディレクトリの取得と情報の生成。
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

        /// <summary> 指定ディレクトリをパイプラインの手順どおりに出力する。 </summary>
        /// <param name="directories"> 出力対象のディレクトリ。 </param>
        /// <param name="pipeline"> 実行する手順を持つパイプライン。 </param>
        public static void Export(string[] directories, AssetStoreToolsPackagePipeline pipeline)
        {
            AssetStoreToolsPackagePlan plan = CreatePlan(directories, pipeline);
            if (plan == null)
            {
                return;
            }

            Export(plan);
        }

        /// <summary> 指定ディレクトリを選択された形式で出力し、必要に応じてZIP化する。 </summary>
        /// <param name="directories"> 出力対象のディレクトリ。 </param>
        /// <param name="mode"> 個別出力と統合出力の指定。 </param>
        /// <param name="createZip"> 出力後にZIP化するか。 </param>
        /// <param name="usedDependencies"> 使用中アセットと強制包含拡張子だけへ絞るか。 </param>
        public static void Export(string[] directories, PackageModeEnum mode, bool createZip = false, bool usedDependencies = false)
        {
            AssetStoreToolsPackagePlan plan = CreatePlan(directories, mode, createZip, usedDependencies);
            if (plan == null)
            {
                return;
            }

            Export(plan);
        }

        /// <summary>
        ///     出力内容を確定した計画を組み立てる。この時点ではファイルを出力しない。
        /// </summary>
        /// <param name="directories"> 出力対象のディレクトリ。 </param>
        /// <param name="pipeline"> 実行する手順を持つパイプライン。 </param>
        /// <returns> 出力計画。対象が無い場合や設定を読み込めない場合はnull。 </returns>
        internal static AssetStoreToolsPackagePlan CreatePlan(
            string[] directories,
            AssetStoreToolsPackagePipeline pipeline)
        {
            if (pipeline == null)
            {
                Debug.LogError($"[{nameof(AssetStoreToolsPackager)}]\nパイプラインが指定されていません。");
                return null;
            }

            return AssetStoreToolsPackagePipelineRunner.CreatePlan(
                directories, pipeline.Steps, pipeline.name);
        }

        /// <summary>
        ///     旧来のフラグ指定から計画を組み立てる。この時点ではファイルを出力しない。
        /// </summary>
        /// <param name="directories"> 出力対象のディレクトリ。 </param>
        /// <param name="mode"> 個別出力と統合出力の指定。 </param>
        /// <param name="createZip"> 出力後にZIP化するか。 </param>
        /// <param name="usedDependencies"> 使用中アセットと強制包含拡張子だけへ絞るか。 </param>
        /// <returns> 出力計画。対象が無い場合や設定を読み込めない場合はnull。 </returns>
        internal static AssetStoreToolsPackagePlan CreatePlan(
            string[] directories,
            PackageModeEnum mode,
            bool createZip,
            bool usedDependencies)
        {
            return AssetStoreToolsPackagePipelineRunner.CreatePlan(
                directories,
                CreateStepsFromOptions(mode, createZip, usedDependencies),
                LEGACY_PIPELINE_NAME);
        }

        /// <summary>
        ///     確定済みの計画に従ってパイプラインを実行する。
        /// </summary>
        /// <param name="plan"> 出力する計画。 </param>
        internal static void Export(AssetStoreToolsPackagePlan plan)
        {
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
            if (forceIncludeExtensions == null || forceIncludeExtensions.Count == 0)
            {
                return false;
            }

            string extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            for (int i = 0; i < forceIncludeExtensions.Count; i++)
            {
                if (string.Equals(extension, forceIncludeExtensions[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

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
        private static List<AssetStoreToolsPackageStepStrategy> CreateStepsFromOptions(
            PackageModeEnum mode,
            bool createZip,
            bool usedDependencies)
        {
            List<AssetStoreToolsPackageStepStrategy> steps = new();

            if (usedDependencies)
            {
                steps.Add(new AssetStoreToolsUsedDependenciesStrategy());
            }

            if ((mode & PackageModeEnum.Singles) != 0)
            {
                steps.Add(new AssetStoreToolsSinglePackageStrategy());
            }

            // 非推奨のCombineは削除まで動作を維持する必要があるため、
            // 廃止予定の警告をここでは抑止する。利用側の指定に対しては警告が出る。
#pragma warning disable CS0618
            if ((mode & PackageModeEnum.Combine) != 0)
            {
                steps.Add(new AssetStoreToolsCombinePackageStrategy());
            }
#pragma warning restore CS0618

            if (createZip)
            {
                steps.Add(new AssetStoreToolsCreateZipStrategy());
            }

            return steps;
        }
    }
}
