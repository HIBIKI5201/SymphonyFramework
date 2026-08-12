namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     インポート先での状態を表す。
    /// </summary>
    internal enum AssetStoreToolsImportStateEnum
    {
        #region 外部向けAPI

        /// <summary> このプロジェクトへまだ導入されていない。 </summary>
        New,

        /// <summary> 導入済みだが、出力側の方が新しい。 </summary>
        Updated,

        /// <summary> 導入済みで、リビジョンが一致している。 </summary>
        UpToDate,

        /// <summary> 導入済みで、ローカルの方が新しい。 </summary>
        Newer,

        #endregion
    }

    /// <summary>
    ///     インポート候補1件を保持する。
    /// </summary>
    internal sealed class AssetStoreToolsImportCandidate
    {
        #region 外部向けAPI

        /// <summary> パッケージ対象ディレクトリの名前。 </summary>
        public string Name;

        /// <summary> 出力先フォルダにあるパッケージのファイル名。 </summary>
        public string FileName;

        /// <summary> マニフェストが持つリビジョン。 </summary>
        public int ManifestVersion;

        /// <summary> 現在導入されているリビジョン。 </summary>
        /// <remarks> 未導入の場合はnull。 </remarks>
        public int? LocalVersion;

        /// <summary> インポートの要否を表す状態。 </summary>
        public AssetStoreToolsImportStateEnum State;

        /// <summary> ユーザーがインポート対象として選択しているかを示す。 </summary>
        public bool IsSelected;

        #endregion
    }
}
