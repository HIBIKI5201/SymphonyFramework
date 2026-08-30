namespace SymphonyFrameWork.Editor
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

        /// <summary> Service Locatorモジュール。 </summary>
        ServiceLocator,

        /// <summary> Scene Loaderモジュール。 </summary>
        SceneLoader,

        /// <summary> Scene Blockモジュール。 </summary>
        SceneBlock,

        /// <summary> Save Data Systemモジュール。 </summary>
        SaveDataSystem,

        /// <summary> Audio Managerモジュール。 </summary>
        AudioManager,

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

        #endregion
    }
}
