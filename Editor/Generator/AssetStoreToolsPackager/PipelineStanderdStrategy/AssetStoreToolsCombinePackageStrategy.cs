using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     全ディレクトリを1つの<c>.unitypackage</c>へまとめて出力する手順。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Execute段階の手順。
    ///         <b>統合パッケージはディレクトリ単位で取り出せないため、差分インポートの単位にならない。</b>
    ///         この手順だけで出力した場合、<c>PackageManifest.json</c>は作られない。
    ///     </para>
    ///     <para>
    ///         上記の理由から既定テンプレート（<see cref="AssetStoreToolsPackagePipeline" />の
    ///         テンプレート）には含めていない。統合パッケージが必要な場合だけ、
    ///         パイプラインへ明示的に追加する。
    ///     </para>
    /// </remarks>
    [Serializable]
    public sealed class AssetStoreToolsCombinePackageStrategy : AssetStoreToolsStanderdStrategy
    {
        /// <inheritdoc />
        public override string DisplayName => "Combine";

        /// <inheritdoc />
        protected internal override void Execute(AssetStoreToolsPackageExportContext context)
        {
            WarnIfNotDiffImportable(context);

            try
            {
                string combinedName =
                    $"AllPackages_{context.DateTime:yyyyMMdd_HHmmss}.unitypackage";

                string[] exportFiles;
                ExportPackageOptions options;

                if (context.Plan.IsFiltered)
                {
                    // 空判定は出力時バージョンを加える前に行う。加えた後では常に非空になる。
                    if (context.Plan.Entries.All(entry => entry.AssetPaths.Count == 0))
                    {
                        Debug.LogWarning("使用中アセットが存在しないため統合パッケージを作成しませんでした。");
                        return;
                    }

                    exportFiles = context.Plan.Entries
                        .SelectMany(AssetStoreToolsPackagePipelineRunner.BuildExportFiles)
                        .Distinct()
                        .ToArray();
                    options = ExportPackageOptions.Default;
                }
                else
                {
                    exportFiles = context.Plan.Entries
                        .Select(entry => entry.DirectoryPath)
                        .ToArray();
                    options = ExportPackageOptions.Recurse;
                }

                AssetDatabase.ExportPackage(
                    exportFiles,
                    Path.Combine(context.ExportLocalPath, combinedName),
                    options
                );

                Debug.Log($"合成パッケージ作成: {combinedName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"合計パッケージの出力に失敗\n{e}");
            }
        }

        /// <summary>
        ///     個別出力を伴わない場合に、差分インポートの対象外であることを警告する。
        /// </summary>
        /// <param name="context"> 実行中の出力コンテキスト。 </param>
        private static void WarnIfNotDiffImportable(AssetStoreToolsPackageExportContext context)
        {
            bool hasSingles = context.Plan.Steps
                .Any(step => step is AssetStoreToolsSinglePackageStrategy);

            if (hasSingles)
            {
                return;
            }

            Debug.LogWarning(
                $"[{nameof(AssetStoreToolsCombinePackageStrategy)}]\n"
                + "統合パッケージだけの出力は差分インポートの対象になりません。"
                + "ディレクトリ単位で取り出せないためです。"
                + $"\n差分インポートが必要な場合は、パイプラインへ"
                + $"{nameof(AssetStoreToolsSinglePackageStrategy)}を追加してください。");
        }
    }
}
