using Newtonsoft.Json;

using System.Collections.Generic;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     1回の出力で作成したパッケージ名とリビジョンの一覧。
    ///     出力先フォルダのPackageManifest.jsonとして保存される。
    /// </summary>
    /// <remarks>
    ///     個別出力したパッケージだけを記録する。統合パッケージはディレクトリ単位で取り出せず、
    ///     差分インポートの単位にならないため含めない。
    /// </remarks>
    internal sealed class AssetStoreToolsPackageManifest
    {
        /// <summary> 出力した日時。記録用で、比較には使わない。 </summary>
        [JsonProperty("exportedAt")]
        public string ExportedAt;

        /// <summary> 出力した個別パッケージの一覧。 </summary>
        [JsonProperty("packages")]
        public List<AssetStoreToolsPackageManifestEntry> Packages = new();
    }

    /// <summary> 出力した個別パッケージ1件分の記録。 </summary>
    internal sealed class AssetStoreToolsPackageManifestEntry
    {
        /// <summary> パッケージ対象ディレクトリの名前。 </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary> 出力した時点のリビジョン。 </summary>
        [JsonProperty("version")]
        public int Version;

        /// <summary> 出力したパッケージのファイル名。 </summary>
        [JsonProperty("fileName")]
        public string FileName;
    }
}
