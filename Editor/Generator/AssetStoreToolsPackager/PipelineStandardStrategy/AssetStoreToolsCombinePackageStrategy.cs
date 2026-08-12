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
    public sealed class AssetStoreToolsCombinePackageStrategy : AssetStoreToolsStandardStrategy
    {
        #region 外部向けAPI

        /// <inheritdoc />
        public override string DisplayName => "Combine";

        #endregion

        #region 内部処理

        /// <inheritdoc />
        protected internal override void Execute(AssetStoreToolsPackageExportContext context)
        {
            // 個別パッケージが無い構成では差分インポートできないことを出力前に通知する。
            WarnIfNotDiffImportable(context);

            try
            {
                string combinedName =
                    $"AllPackages_{context.DateTime:yyyyMMdd_HHmmss}.unitypackage";

                string[] exportFiles;
                ExportPackageOptions options;

                // 絞り込み済みなら確定したファイルだけを出力し、未絞り込みなら各ディレクトリを再帰出力する。
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

                // 出力内容とオプションを確定してから、1つの統合パッケージへ書き出す。
                AssetDatabase.ExportPackage(
                    exportFiles,
                    Path.Combine(context.ExportLocalPath, combinedName),
                    options
                );

                Debug.Log($"統合パッケージ作成: {combinedName}");
            }
            catch (Exception e)
            {
                // 統合パッケージの失敗を記録し、ランナーが後続手順を続行できるよう例外を閉じ込める。
                Debug.LogError($"統合パッケージの出力に失敗\n{e}");
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

            // 個別パッケージがあれば、それを差分インポートへ利用できるため警告しない。
            if (hasSingles) { return; }

            Debug.LogWarning(
                $"[{nameof(AssetStoreToolsCombinePackageStrategy)}]\n"
                + "統合パッケージだけの出力は差分インポートの対象になりません。"
                + "ディレクトリ単位で取り出せないためです。"
                + $"\n差分インポートが必要な場合は、パイプラインへ"
                + $"{nameof(AssetStoreToolsSinglePackageStrategy)}を追加してください。");
        }

        #endregion
    }
}
