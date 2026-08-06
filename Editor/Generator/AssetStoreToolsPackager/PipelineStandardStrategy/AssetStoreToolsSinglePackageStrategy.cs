using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     ディレクトリごとに<c>.unitypackage</c>を出力し、差分インポート用のマニフェストを書く手順。
    /// </summary>
    /// <remarks>
    ///     Execute段階の手順。個別出力したパッケージだけが差分インポートの単位になる。
    /// </remarks>
    [Serializable]
    public sealed class AssetStoreToolsSinglePackageStrategy : AssetStoreToolsStandardStrategy
    {
        /// <inheritdoc />
        public override string DisplayName => "Singles";

        /// <inheritdoc />
        protected internal override void Execute(AssetStoreToolsPackageExportContext context)
        {
            List<AssetStoreToolsPackagePlanEntry> exported = new();
            foreach (AssetStoreToolsPackagePlanEntry entry in context.Plan.Entries)
            {
                if (ExportEntry(context, entry))
                {
                    exported.Add(entry);
                }
            }

            WriteManifest(context, exported);
        }

        /// <summary> 出力単位1件分をパッケージ化する。 </summary>
        /// <param name="context"> 出力先を保持するコンテキスト。 </param>
        /// <param name="entry"> 出力する単位。 </param>
        /// <returns> パッケージを出力できた場合はtrue。 </returns>
        private static bool ExportEntry(
            AssetStoreToolsPackageExportContext context,
            AssetStoreToolsPackagePlanEntry entry)
        {
            try
            {
                string[] exportFiles;
                ExportPackageOptions options;

                if (entry.IsFiltered)
                {
                    if (entry.AssetPaths.Count == 0)
                    {
                        Debug.LogWarning($"使用中アセットなし: {entry.DirectoryPath}");
                        return false;
                    }

                    // 出力時バージョンは計画に含めず、ここで加える。
                    exportFiles = AssetStoreToolsPackagePipelineRunner.BuildExportFiles(entry);
                    options = ExportPackageOptions.Default;
                }
                else
                {
                    // 丸ごと出力する経路。計画のAssetPathsは提示用で、出力はディレクトリ単位で行う。
                    exportFiles = new[] { entry.DirectoryPath };
                    options = ExportPackageOptions.Recurse;
                }

                AssetDatabase.ExportPackage(
                    exportFiles,
                    Path.Combine(context.ExportLocalPath, $"{entry.Name}.unitypackage"),
                    options
                );

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"パッケージの出力に失敗しました: {entry.DirectoryPath}\n{e}");
                return false;
            }
        }

        /// <summary>
        ///     出力先フォルダへ、個別出力したパッケージ一覧のマニフェストを書き出す。
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         ZIPへ含めるため、ZIP化より前に書く必要がある。
        ///         パイプラインの並び順で<see cref="AssetStoreToolsCreateZipStrategy" />より後ろに置くと、
        ///         マニフェストがZIPへ含まれない。
        ///     </para>
        ///     <para>
        ///         <b>出力できたものだけを書く。</b>失敗した単位を載せると、インポート側が
        ///         存在しないファイルを開こうとする。マニフェストは「出力したパッケージの一覧」であり、
        ///         「出力しようとした一覧」ではない。
        ///     </para>
        /// </remarks>
        /// <param name="context"> 出力先を保持するコンテキスト。 </param>
        /// <param name="exported"> パッケージを出力できた単位。 </param>
        private static void WriteManifest(
            AssetStoreToolsPackageExportContext context,
            IReadOnlyList<AssetStoreToolsPackagePlanEntry> exported)
        {
            var manifest = new AssetStoreToolsPackageManifest
            {
                ExportedAt = AssetStoreToolsVersionLog.CreateTimestamp(),
                Packages = exported
                    .Select(entry => new AssetStoreToolsPackageManifestEntry
                    {
                        Name = entry.Name,
                        Version = entry.Version,
                        FileName = $"{entry.Name}.unitypackage",
                    })
                    .ToList(),
            };

            AssetStoreToolsVersionLogStore.TryWriteManifest(context.ExportFullPath, manifest);
        }
    }
}
