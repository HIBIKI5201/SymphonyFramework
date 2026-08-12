using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     パッケージ対象ディレクトリごとの現在リビジョンを保持する。
    /// </summary>
    /// <remarks>
    ///     対象フォルダ直下のPackageVersions.jsonとして保存する。
    ///     リビジョンは「このプロジェクトでの編集の履歴」を表す。
    ///     パッケージへ同梱される出力時バージョンとは意味が異なる。
    /// </remarks>
    internal sealed class AssetStoreToolsVersionLog
    {
        #region 外部向けAPI

        /// <summary> ディレクトリごとのリビジョン。 </summary>
        [JsonProperty("directories")]
        public List<AssetStoreToolsVersionEntry> Directories = new();

        /// <summary>
        ///     指定したディレクトリ名をリビジョン1で登録した初期状態を生成する。
        /// </summary>
        /// <param name="directoryNames"> 登録するディレクトリ名。 </param>
        /// <returns> 全ディレクトリをリビジョン1で持つバージョンログ。 </returns>
        internal static AssetStoreToolsVersionLog CreateDefault(IEnumerable<string> directoryNames)
        {
            // 入力が無い場合も保存可能な空のログを返す。
            AssetStoreToolsVersionLog log = new();

            // ディレクトリ一覧が未指定なら、登録対象が無いものとして扱う。
            if (directoryNames == null) { return log; }

            string timestamp = CreateTimestamp();
            // 同じ初期化処理で生成した項目には、同一の記録時刻を付ける。
            foreach (string name in directoryNames)
            {
                // 名前の無い項目は後から変更パスと対応付けられないため除外する。
                if (string.IsNullOrWhiteSpace(name)) { continue; }

                log.Directories.Add(new AssetStoreToolsVersionEntry
                {
                    Name = name.Trim(),
                    Version = 1,
                    UpdatedAt = timestamp,
                });
            }

            return log;
        }

        /// <summary>
        ///     記録用のタイムスタンプを生成する。
        /// </summary>
        /// <returns> ラウンドトリップ可能な書式のUTC時刻。 </returns>
        internal static string CreateTimestamp() => DateTime.UtcNow.ToString("o");

        /// <summary>
        ///     空要素や不正なリビジョンを取り除いた正規化済みのログを返す。
        /// </summary>
        /// <remarks> 元のインスタンスは変更しない。 </remarks>
        /// <returns> 名前の空要素を除き、負のリビジョンを0へ丸めたログ。 </returns>
        internal AssetStoreToolsVersionLog Normalize() => new()
        {
            Directories = NormalizeEntries(Directories),
        };

        /// <summary>
        ///     指定ディレクトリの現在リビジョンを取得する。
        /// </summary>
        /// <param name="name"> ディレクトリ名。 </param>
        /// <returns> 登録済みのリビジョン。未登録の場合は0。 </returns>
        internal int GetVersion(string name)
        {
            AssetStoreToolsVersionEntry entry = FindEntry(name);
            return entry?.Version ?? 0;
        }

        /// <summary>
        ///     指定ディレクトリのリビジョンを1つ進め、更新日時を記録する。
        /// </summary>
        /// <remarks> 未登録のディレクトリはリビジョン1で登録する。 </remarks>
        /// <param name="name"> ディレクトリ名。 </param>
        internal void IncrementVersion(string name)
        {
            // 名前の無い変更は永続化後に対象ディレクトリを特定できないため記録しない。
            if (string.IsNullOrWhiteSpace(name)) { return; }

            AssetStoreToolsVersionEntry entry = FindEntry(name);

            // 初めて変更されたディレクトリは、初期リビジョン1として登録する。
            if (entry == null)
            {
                Directories.Add(new AssetStoreToolsVersionEntry
                {
                    Name = name.Trim(),
                    Version = 1,
                    UpdatedAt = CreateTimestamp(),
                });
                return;
            }

            entry.Version++;
            entry.UpdatedAt = CreateTimestamp();
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     名前で登録済みエントリを検索する。
        /// </summary>
        /// <param name="name"> ディレクトリ名。 </param>
        /// <returns> 一致したエントリ。無い場合はnull。 </returns>
        private AssetStoreToolsVersionEntry FindEntry(string name)
        {
            // 検索名か一覧が無ければ一致する項目は存在しない。
            if (string.IsNullOrWhiteSpace(name) || Directories == null) { return null; }

            string trimmedName = name.Trim();
            // ファイルシステム上のディレクトリ名として、大文字小文字を区別せず照合する。
            return Directories.FirstOrDefault(entry =>
                entry != null
                && string.Equals(entry.Name, trimmedName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     エントリから無効値を除き、保存可能な形へ揃える。
        /// </summary>
        private static List<AssetStoreToolsVersionEntry> NormalizeEntries(
            List<AssetStoreToolsVersionEntry> source)
        {
            // JSONで一覧自体が欠けていても、利用側へnullを渡さない。
            if (source == null) { return new List<AssetStoreToolsVersionEntry>(); }

            // 名前の無い項目を除外し、比較と加算に使える値へ正規化する。
            return source
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.Name))
                .Select(entry => new AssetStoreToolsVersionEntry
                {
                    Name = entry.Name.Trim(),

                    // 手で編集して負値になった場合は未登録と同じ扱いへ丸める。
                    Version = entry.Version < 0 ? 0 : entry.Version,
                    UpdatedAt = entry.UpdatedAt,
                })
                .ToList();
        }

        #endregion
    }

    /// <summary>
    ///     ディレクトリ1件のリビジョンを保持する。
    /// </summary>
    internal sealed class AssetStoreToolsVersionEntry
    {
        #region 外部向けAPI

        /// <summary> パッケージ対象ディレクトリの名前。 </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary> 現在のリビジョン。 </summary>
        [JsonProperty("version")]
        public int Version;

        /// <summary> 最後にリビジョンを進めた日時。 </summary>
        /// <remarks> 記録用で、比較には使わない。 </remarks>
        [JsonProperty("updatedAt")]
        public string UpdatedAt;

        #endregion
    }
}
