using System;
using System.IO;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     1回のパッケージ出力で共有する名前、日時、入出力パスを保持する。
    /// </summary>
    /// <remarks>
    ///     <c>ref struct</c>はフィールドへ保持できず、パイプラインの拡張点へ渡せないため、
    ///     <see cref="AssetStoreToolsPackageExportContext" />へ置き換えた。
    /// </remarks>
    [Obsolete(
        "パイプラインへ移行しました。" + nameof(AssetStoreToolsPackageExportContext) + "を使用してください。",
        error: false)]
    public readonly ref struct AssetStoreToolsPackageContext
    {
        #region 外部向けAPI

        /// <summary>
        ///     出力元と出力先から不変のパッケージ処理コンテキストを生成する。
        /// </summary>
        public AssetStoreToolsPackageContext(
            string basePackageName,
            string exportRoot,
            string[] exportDirectories)
        {
            // 出力開始時刻を1回だけ取得し、表示名と記録日時のずれを防ぐ。
            ExportDirectories = exportDirectories;
            this.DateTime = DateTime.Now;
            PackageName = GeneratePackageName(basePackageName, DateTime);

            // ファイルI/O用の絶対パスとAssetDatabase用のローカルパスを同じ設定値から組み立てる。
            ExportRoot = Path.Combine(Application.dataPath, "..", exportRoot);
            ExportLocalPath = Path.Combine(exportRoot, PackageName);
            ExportFullPath = Path.Combine(ExportRoot, PackageName);
        }

        /// <summary> 日時を含む出力パッケージ名。 </summary>
        public readonly string PackageName;

        /// <summary> パッケージ出力先の絶対ルートパス。 </summary>
        public readonly string ExportRoot;

        /// <summary> AssetDatabaseから扱えるパッケージ出力先パス。 </summary>
        public readonly string ExportLocalPath;

        /// <summary> 今回のパッケージ出力先となる絶対パス。 </summary>
        public readonly string ExportFullPath;

        /// <summary> パッケージ化するディレクトリ一覧。 </summary>
        public readonly string[] ExportDirectories;

        /// <summary> パッケージ処理を開始した日時。 </summary>
        public readonly DateTime DateTime;

        #endregion

        #region 内部処理

        /// <summary>
        ///     基本名と実行日時から重複しにくい出力パッケージ名を生成する。
        /// </summary>
        private static string GeneratePackageName(string baseName, DateTime dateTime)
            => $"Export_{baseName}_{dateTime:yyyyMMdd_HHmmss}";

        #endregion
    }
}
