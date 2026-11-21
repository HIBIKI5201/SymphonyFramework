namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     ブラウザで開けるFrameworkのドキュメントページ。
    /// </summary>
    /// <remarks> 各値は<c>Documentation~/</c>配下の文書と1対1で対応する。 </remarks>
    public enum SymphonyDocumentPageEnum
    {
        #region 外部向けAPI

        /// <summary> 全ドキュメントの索引。 </summary>
        Index,

        /// <summary> Partituraモジュール。 </summary>
        Partitura,

        /// <summary> Attaccaモジュール。 </summary>
        Attacca,

        /// <summary> Suiteモジュール。 </summary>
        Suite,

        /// <summary> Manuscriptモジュール。 </summary>
        Manuscript,

        /// <summary> Sonorityモジュール。 </summary>
        Sonority,

        /// <summary> Pause Managerモジュール。 </summary>
        PauseManager,

        /// <summary> HUD、ログ、処理時間計測のモジュール。 </summary>
        Debug,

        /// <summary> Awaitable、Tween、文字列、Componentの補助モジュール。 </summary>
        Utility,

        /// <summary> Inspector属性モジュール。 </summary>
        InspectorAttributes,

        /// <summary> AutoEnumGeneratorモジュール。 </summary>
        AutoEnumGenerator,

        /// <summary> Asset Store Tools Packagerモジュール。 </summary>
        AssetStoreToolsPackager,

        /// <summary> FolderGenerator、AssemblyGenerator、SymphonyPackageLoaderのモジュール。 </summary>
        ProjectStructureTools,

        /// <summary> Editor機能の索引と、単一モジュールに属さない横断的な仕組み。 </summary>
        EditorTools,

        /// <summary> ビルド前のUXML依存とフォント設定の検証。 </summary>
        BuildValidation,

        #endregion
    }
}
