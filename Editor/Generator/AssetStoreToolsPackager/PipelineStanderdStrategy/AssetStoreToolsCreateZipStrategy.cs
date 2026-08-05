using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     出力フォルダをZIP化する手順。
    /// </summary>
    /// <remarks>
    ///     Execute段階の手順。
    ///     <b>出力を行う手順より後ろへ置くこと。</b>先頭へ置くと空のフォルダを圧縮する。
    ///     順序はフレームワークが並べ替えず、パイプラインの並びをそのまま実行する。
    /// </remarks>
    [Serializable]
    public sealed class AssetStoreToolsCreateZipStrategy : AssetStoreToolsStanderdStrategy
    {
        /// <inheritdoc />
        public override string DisplayName => "Create ZIP";

        /// <inheritdoc />
        protected internal override void Execute(AssetStoreToolsPackageExportContext context)
        {
            try
            {
                string zipFullPath = Path.Combine(context.ExportRoot, $"{context.PackageName}.zip");

                if (!Directory.Exists(context.ExportFullPath))
                {
                    Debug.LogError($"ZIP対象フォルダが存在しません: {context.ExportFullPath}");
                    return;
                }

                if (File.Exists(zipFullPath))
                {
                    File.Delete(zipFullPath);
                }

                ZipFile.CreateFromDirectory(
                    context.ExportFullPath,
                    zipFullPath,
                    CompressionLevel.Optimal,
                    true
                );

                Debug.Log($"ZIP作成完了: {zipFullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"ZIP作成失敗\n{e}");
            }
        }
    }
}
