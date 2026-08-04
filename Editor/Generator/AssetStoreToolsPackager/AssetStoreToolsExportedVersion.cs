using Newtonsoft.Json;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     パッケージが出力された時点のリビジョン。
    ///     各パッケージ対象ディレクトリ直下のExportedVersion.jsonとして保存され、パッケージへ同梱される。
    /// </summary>
    /// <remarks>
    ///     インポート先のプロジェクトでは、このファイルが「今そこに入っているパッケージのリビジョン」を表す。
    ///     出力先のマニフェストと突き合わせることで、インポートが必要なパッケージを判定する。
    /// </remarks>
    internal sealed class AssetStoreToolsExportedVersion
    {
        /// <summary> パッケージ対象ディレクトリの名前。 </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary> 出力した時点のリビジョン。 </summary>
        [JsonProperty("version")]
        public int Version;

        /// <summary> 出力した日時。記録用で、比較には使わない。 </summary>
        [JsonProperty("exportedAt")]
        public string ExportedAt;
    }
}
