using SymphonyFrameWork.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     パイプラインの手順を2段階で実行する。
    /// </summary>
    /// <remarks>
    ///     Plan段階で計画を組み立て、確認を経てExecute段階で出力する。
    ///     計画とコンテキストを生成できるのはこのクラスだけで、手順から新しい計画は作れない。
    /// </remarks>
    internal static class AssetStoreToolsPackagePipelineRunner
    {
        /// <summary>
        ///     出力内容を確定した計画を組み立てる。この時点ではファイルを出力しない。
        /// </summary>
        /// <param name="directories"> 出力対象のディレクトリ。 </param>
        /// <param name="steps"> 実行する手順。null要素は取り除かれる。 </param>
        /// <param name="pipelineName"> 確認ウィンドウへ表示するパイプライン名。 </param>
        /// <returns> 出力計画。対象が無い場合や設定を読み込めない場合はnull。 </returns>
        internal static AssetStoreToolsPackagePlan CreatePlan(
            string[] directories,
            IReadOnlyList<AssetStoreToolsPackageStepStrategy> steps,
            string pipelineName)
        {
            if (directories == null || directories.Length == 0)
            {
                Debug.LogWarning("パッケージ化するフォルダが存在しませんでした。");
                return null;
            }

            AssetStoreToolsPackagerConfig config = AssetStoreToolsPackagerConfigStore.Load();
            if (config == null)
            {
                Debug.LogError($"{LOG_PREFIX}\n設定を読み込めなかったためパッケージを出力しませんでした。");
                return null;
            }

            // 読み込めない場合もリビジョン0として出力は続行する。
            // 差分インポート側で常に新規と判定されるだけで、既存の出力機能は損なわれない。
            AssetStoreToolsVersionLog versionLog = AssetStoreToolsVersionLogStore.Load();

            List<AssetStoreToolsPackagePlanEntry> entries = new();
            foreach (string dir in directories)
            {
                string name = Path.GetFileName(dir);

                entries.Add(new AssetStoreToolsPackagePlanEntry(
                    dir,
                    name,
                    versionLog?.GetVersion(name) ?? 0,
                    CollectAssets(dir, config.ForceIncludeExtensions)));
            }

            var plan = new AssetStoreToolsPackagePlan(
                pipelineName,
                SelectValidSteps(steps),
                entries,
                config.ForceIncludeExtensions);

            RunPlanSteps(plan);

            return plan;
        }

        /// <summary>
        ///     計画へ持ち込める手順だけを取り出す。
        /// </summary>
        /// <remarks>
        ///     サブクラスセレクターは&lt;null&gt;を選べるため、null要素がそのまま配列へ残る。
        /// </remarks>
        /// <param name="steps"> パイプラインが保持する手順。 </param>
        /// <returns> null要素を取り除いた手順。 </returns>
        internal static AssetStoreToolsPackageStepStrategy[] SelectValidSteps(
            IReadOnlyList<AssetStoreToolsPackageStepStrategy> steps)
        {
            return steps == null
                ? Array.Empty<AssetStoreToolsPackageStepStrategy>()
                : steps.Where(step => step != null).ToArray();
        }

        /// <summary>
        ///     Plan段階の手順を並び順に実行する。
        /// </summary>
        /// <remarks>
        ///     1つの手順の失敗で計画全体を捨てない。
        ///     絞り込みが効かないまま出力されることは確認ウィンドウで判別できる。
        /// </remarks>
        /// <param name="plan"> 組み立て中の出力計画。 </param>
        internal static void RunPlanSteps(AssetStoreToolsPackagePlan plan)
        {
            foreach (AssetStoreToolsPackageStepStrategy step in plan.Steps)
            {
                try
                {
                    step.Plan(plan);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{LOG_PREFIX}\n手順の計画に失敗しました: {step.DisplayName}\n{e}");
                }
            }
        }

        /// <summary>
        ///     Execute段階の手順を並び順に実行する。
        /// </summary>
        /// <remarks>
        ///     1つの手順の失敗で残りの出力まで巻き添えにしない。
        /// </remarks>
        /// <param name="context"> 出力先と確定済みの計画を保持するコンテキスト。 </param>
        internal static void RunExecuteSteps(AssetStoreToolsPackageExportContext context)
        {
            foreach (AssetStoreToolsPackageStepStrategy step in context.Plan.Steps)
            {
                try
                {
                    step.Execute(context);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{LOG_PREFIX}\n手順の実行に失敗しました: {step.DisplayName}\n{e}");
                }
            }
        }

        /// <summary>
        ///     確定済みの計画に従ってパイプラインを実行する。
        /// </summary>
        /// <param name="plan"> 出力する計画。 </param>
        internal static void Export(AssetStoreToolsPackagePlan plan)
        {
            if (plan == null || plan.Entries.Count == 0)
            {
                Debug.LogWarning("パッケージ化するフォルダが存在しませんでした。");
                return;
            }

            var context = new AssetStoreToolsPackageExportContext(
                plan,
                PACKAGE_NAME,
                AssetStoreToolsPackagerData.ExportedPackagesPath);

            // 出力フォルダ作成
            if (!Directory.Exists(context.ExportFullPath))
            {
                Directory.CreateDirectory(context.ExportFullPath);
            }

            // 出力時バージョンを先に書き、AssetDatabaseへ載せてからパッケージ化する。
            // Refreshを省くと新規ファイルがAssetDatabaseに載らず、Recurseでも明示指定でも出力されない。
            // すべての出力手順が前提にする準備のため、手順にはせずランナーが必ず実行する。
            WriteExportedVersions(plan);
            AssetDatabase.Refresh();

            if (plan.Steps.Count == 0)
            {
                Debug.LogWarning(
                    $"{LOG_PREFIX}\n手順が1つも設定されていないため、出力時バージョンの書き出しだけを行いました。");
            }

            RunExecuteSteps(context);

            Debug.Log($"{LOG_PREFIX}\nパッケージを出力しました\npath : {context.ExportLocalPath}");
        }

        /// <summary>
        ///     出力対象アセットへ出力時バージョンファイルのパスを加える。
        /// </summary>
        /// <remarks>
        ///     絞り込みを行った経路では計画の一覧がそのままExportPackageの引数になるため、
        ///     ここで加えないとバージョンファイルがパッケージへ含まれない。
        ///     計画そのものへは加えない。加えると空のディレクトリを検出できなくなる。
        /// </remarks>
        /// <param name="entry"> 出力単位。 </param>
        /// <returns> パスの昇順で並んだ出力対象アセットのパス。 </returns>
        internal static string[] BuildExportFiles(AssetStoreToolsPackagePlanEntry entry)
        {
            return entry.AssetPaths
                .Append(BuildExportedVersionPath(entry.DirectoryPath))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        ///     指定ディレクトリから出力候補のアセットを収集する。
        /// </summary>
        /// <remarks>
        ///     ファイルシステムではなくAssetDatabaseを走査する。
        ///     .bundleや.frameworkはUnityが単一アセットとして扱うため、
        ///     ファイル列挙では中身のファイルしか拾えず、ExportPackageへ渡しても出力されない。
        /// </remarks>
        /// <param name="dir"> 収集対象のディレクトリ。 </param>
        /// <param name="forceIncludeExtensions"> 依存関係に関わらず含める拡張子の一覧。 </param>
        /// <returns> パスの昇順で並んだ出力候補アセットのパス。通常のフォルダは含まない。 </returns>
        internal static string[] CollectAssets(
            string dir,
            IReadOnlyList<string> forceIncludeExtensions)
        {
            return AssetDatabase.FindAssets(string.Empty, new[] { dir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct()
                .Where(path => IsCollectTarget(path, forceIncludeExtensions))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private const string LOG_PREFIX = "[" + nameof(AssetStoreToolsPackager) + "]";

        private const string PACKAGE_NAME = "AssetStoreToolsPackage";

        /// <summary>
        ///     絞り込み前の収集対象に含めるか判定する。
        /// </summary>
        /// <remarks>
        ///     通常のフォルダは出力対象にしない。
        ///     .bundle等のフォルダ形式アセットは、IsValidFolderの結果に関わらず拡張子一致で残す。
        /// </remarks>
        /// <param name="path"> 判定するアセットのパス。 </param>
        /// <param name="forceIncludeExtensions"> 強制包含する拡張子の一覧。 </param>
        /// <returns> 収集対象に含める場合はtrue。 </returns>
        private static bool IsCollectTarget(
            string path,
            IReadOnlyList<string> forceIncludeExtensions)
        {
            if (AssetStoreToolsPackager.HasForceIncludeExtension(path, forceIncludeExtensions))
            {
                return true;
            }

            return !AssetDatabase.IsValidFolder(path);
        }

        /// <summary> ディレクトリ直下の出力時バージョンファイルのパスを組み立てる。 </summary>
        /// <param name="directoryPath"> 出力単位となるディレクトリのパス。 </param>
        /// <returns> スラッシュ区切りのアセットパス。 </returns>
        private static string BuildExportedVersionPath(string directoryPath)
            => directoryPath.Replace("\\", "/").TrimEnd('/')
               + "/" + EditorSymphonyConstant.ASSET_STORE_TOOLS_EXPORTED_VERSION_FILE_NAME;

        /// <summary>
        ///     計画中の各ディレクトリへ出力時バージョンを書き出す。
        /// </summary>
        /// <remarks>
        ///     書き込みに失敗しても出力は続行する。バージョンファイルの無いパッケージは
        ///     インポート側で常に新規と判定されるだけで、既存の出力機能は損なわれない。
        /// </remarks>
        /// <param name="plan"> 出力する計画。 </param>
        private static void WriteExportedVersions(AssetStoreToolsPackagePlan plan)
        {
            foreach (AssetStoreToolsPackagePlanEntry entry in plan.Entries)
            {
                AssetStoreToolsVersionLogStore.TryWriteExportedVersion(
                    entry.DirectoryPath,
                    entry.Name,
                    entry.Version);
            }
        }
    }
}
