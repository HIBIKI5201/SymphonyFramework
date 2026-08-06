using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     パッケージ対象ディレクトリごとの現在リビジョン。
    ///     対象フォルダ直下のPackageVersions.jsonとして保存される。
    /// </summary>
    /// <remarks>
    ///     リビジョンは「このプロジェクトでの編集の履歴」を表す。
    ///     パッケージへ同梱される出力時バージョンとは意味が異なる。
    /// </remarks>
    internal sealed class AssetStoreToolsVersionLog
    {
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
            var log = new AssetStoreToolsVersionLog();

            if (directoryNames == null)
            {
                return log;
            }

            string timestamp = CreateTimestamp();
            foreach (string name in directoryNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                log.Directories.Add(new AssetStoreToolsVersionEntry
                {
                    Name = name.Trim(),
                    Version = 1,
                    UpdatedAt = timestamp,
                });
            }

            return log;
        }

        /// <summary> 記録用のタイムスタンプを生成する。 </summary>
        /// <returns> ラウンドトリップ可能な書式のUTC時刻。 </returns>
        internal static string CreateTimestamp() => DateTime.UtcNow.ToString("o");

        /// <summary>
        ///     空要素や不正なリビジョンを取り除いた正規化済みのログを返す。元のインスタンスは変更しない。
        /// </summary>
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
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            AssetStoreToolsVersionEntry entry = FindEntry(name);

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

        /// <summary> 名前で登録済みエントリを検索する。大文字小文字は区別しない。 </summary>
        /// <param name="name"> ディレクトリ名。 </param>
        /// <returns> 一致したエントリ。無い場合はnull。 </returns>
        private AssetStoreToolsVersionEntry FindEntry(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || Directories == null)
            {
                return null;
            }

            string trimmedName = name.Trim();
            return Directories.FirstOrDefault(entry =>
                entry != null
                && string.Equals(entry.Name, trimmedName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary> エントリから空要素を捨て、名前の空白を落として負のリビジョンを丸める。 </summary>
        private static List<AssetStoreToolsVersionEntry> NormalizeEntries(
            List<AssetStoreToolsVersionEntry> source)
        {
            if (source == null)
            {
                return new List<AssetStoreToolsVersionEntry>();
            }

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
    }

    /// <summary> パッケージ対象ディレクトリ1件分のリビジョン。 </summary>
    internal sealed class AssetStoreToolsVersionEntry
    {
        /// <summary> パッケージ対象ディレクトリの名前。 </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary> 現在のリビジョン。 </summary>
        [JsonProperty("version")]
        public int Version;

        /// <summary> 最後にリビジョンを進めた日時。記録用で、比較には使わない。 </summary>
        [JsonProperty("updatedAt")]
        public string UpdatedAt;
    }
}
