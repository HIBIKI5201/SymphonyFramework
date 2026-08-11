using Newtonsoft.Json;

using System.Collections.Generic;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     1回の出力で作成したパッケージ名とリビジョンを保持する。
    /// </summary>
    /// <remarks>
    ///     出力先フォルダのPackageManifest.jsonとして保存する。
    ///     個別出力したパッケージだけを記録する。統合パッケージはディレクトリ単位で取り出せず、
    ///     差分インポートの単位にならないため含めない。
    /// </remarks>
    internal sealed class AssetStoreToolsPackageManifest
    {
        #region 外部向けAPI

        /// <summary> 出力した日時。 </summary>
        /// <remarks> 記録用で、比較には使わない。 </remarks>
        [JsonProperty("exportedAt")]
        public string ExportedAt;

        /// <summary> 出力した個別パッケージの一覧。 </summary>
        [JsonProperty("packages")]
        public List<AssetStoreToolsPackageManifestEntry> Packages = new();

        #endregion
    }

    /// <summary>
    ///     個別パッケージ1件を記録する。
    /// </summary>
    internal sealed class AssetStoreToolsPackageManifestEntry
    {
        #region 外部向けAPI

        /// <summary> パッケージ対象ディレクトリの名前。 </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary> 出力した時点のリビジョン。 </summary>
        [JsonProperty("version")]
        public int Version;

        /// <summary> 出力したパッケージのファイル名。 </summary>
        [JsonProperty("fileName")]
        public string FileName;

        #endregion
    }
}
