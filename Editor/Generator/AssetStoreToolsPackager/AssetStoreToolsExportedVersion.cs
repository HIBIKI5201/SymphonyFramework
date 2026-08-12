using Newtonsoft.Json;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     パッケージが出力された時点のリビジョンを保持する。
    /// </summary>
    /// <remarks>
    ///     各パッケージ対象ディレクトリ直下のExportedVersion.jsonとして保存し、パッケージへ同梱する。
    ///     インポート先のプロジェクトでは、このファイルが「今そこに入っているパッケージのリビジョン」を表す。
    ///     出力先のマニフェストと突き合わせることで、インポートが必要なパッケージを判定する。
    /// </remarks>
    internal sealed class AssetStoreToolsExportedVersion
    {
        #region 外部向けAPI

        /// <summary> パッケージ対象ディレクトリの名前。 </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary> 出力した時点のリビジョン。 </summary>
        [JsonProperty("version")]
        public int Version;

        /// <summary> 出力した日時。 </summary>
        /// <remarks> 記録用で、比較には使わない。 </remarks>
        [JsonProperty("exportedAt")]
        public string ExportedAt;

        #endregion
    }
}
