namespace SymphonyFrameWork.Editor
{
    /// <summary> インポート先から見たパッケージの状態。 </summary>
    internal enum AssetStoreToolsImportStateEnum
    {
        /// <summary> このプロジェクトへまだ導入されていない。 </summary>
        New,

        /// <summary> 導入済みだが、出力側の方が新しい。 </summary>
        Updated,

        /// <summary> 導入済みで、リビジョンが一致している。 </summary>
        UpToDate,

        /// <summary> 導入済みで、ローカルの方が新しい。 </summary>
        Newer,
    }

    /// <summary> インポート候補となるパッケージ1件分の内容。 </summary>
    internal sealed class AssetStoreToolsImportCandidate
    {
        /// <summary> パッケージ対象ディレクトリの名前。 </summary>
        public string Name;

        /// <summary> 出力先フォルダにあるパッケージのファイル名。 </summary>
        public string FileName;

        /// <summary> マニフェストが持つリビジョン。 </summary>
        public int ManifestVersion;

        /// <summary> 現在導入されているリビジョン。未導入の場合はnull。 </summary>
        public int? LocalVersion;

        /// <summary> インポートの要否を表す状態。 </summary>
        public AssetStoreToolsImportStateEnum State;

        /// <summary> ユーザーがインポート対象として選択しているかを示す。 </summary>
        public bool IsSelected;
    }
}
