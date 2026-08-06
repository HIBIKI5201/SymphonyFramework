using System;
using System.IO;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Execute段階の手順が共有する、出力先パスと確定済みの計画。
    /// </summary>
    /// <remarks>
    ///     生成できるのは<see cref="AssetStoreToolsPackagePipelineRunner" />だけで、
    ///     1回の出力につき1つだけ作られる。
    /// </remarks>
    public sealed class AssetStoreToolsPackageExportContext
    {
        /// <summary> 出力内容を確定した計画。 </summary>
        public AssetStoreToolsPackagePlan Plan { get; }

        /// <summary> 日時を含む出力パッケージ名。 </summary>
        public string PackageName { get; }

        /// <summary> パッケージ出力先の絶対ルートパス。 </summary>
        public string ExportRoot { get; }

        /// <summary> AssetDatabaseから扱えるパッケージ出力先パス。 </summary>
        public string ExportLocalPath { get; }

        /// <summary> 今回のパッケージ出力先となる絶対パス。 </summary>
        public string ExportFullPath { get; }

        /// <summary> パッケージ処理を開始した日時。 </summary>
        public DateTime DateTime { get; }

        /// <summary> 計画と出力先設定から、1回分の出力コンテキストを生成する。 </summary>
        /// <param name="plan"> 出力内容を確定した計画。 </param>
        /// <param name="basePackageName"> 出力フォルダ名の基本部分。 </param>
        /// <param name="exportRoot"> 出力先フォルダのプロジェクト相対パス。 </param>
        internal AssetStoreToolsPackageExportContext(
            AssetStoreToolsPackagePlan plan,
            string basePackageName,
            string exportRoot)
        {
            Plan = plan;
            DateTime = DateTime.Now;
            PackageName = $"Export_{basePackageName}_{DateTime:yyyyMMdd_HHmmss}";

            ExportRoot = Path.Combine(Application.dataPath, "..", exportRoot);
            ExportLocalPath = Path.Combine(exportRoot, PackageName);
            ExportFullPath = Path.Combine(ExportRoot, PackageName);
        }
    }
}
