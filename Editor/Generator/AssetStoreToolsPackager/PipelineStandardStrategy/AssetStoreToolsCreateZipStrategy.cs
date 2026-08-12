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
    public sealed class AssetStoreToolsCreateZipStrategy : AssetStoreToolsStandardStrategy
    {
        #region 外部向けAPI

        /// <inheritdoc />
        public override string DisplayName => "Create ZIP";

        #endregion

        #region 内部処理

        /// <inheritdoc />
        protected internal override void Execute(AssetStoreToolsPackageExportContext context)
        {
            try
            {
                string zipFullPath = Path.Combine(context.ExportRoot, $"{context.PackageName}.zip");

                // 前段の出力手順がフォルダを用意できなかった場合は、空のZIPを作らない。
                if (!Directory.Exists(context.ExportFullPath))
                {
                    Debug.LogError($"ZIP対象フォルダが存在しません: {context.ExportFullPath}");
                    return;
                }

                // 同名ZIPがある場合だけ置き換え、CreateFromDirectoryの既存ファイル例外を避ける。
                if (File.Exists(zipFullPath)) { File.Delete(zipFullPath); }

                // 個別パッケージとマニフェストを含む完成済みフォルダを最後に圧縮する。
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
                // ZIP化だけの失敗として記録し、先に作成したパッケージは残す。
                Debug.LogError($"ZIP作成失敗\n{e}");
            }
        }

        #endregion
    }
}
