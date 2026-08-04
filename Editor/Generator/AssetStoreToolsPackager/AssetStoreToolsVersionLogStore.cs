using Newtonsoft.Json;

using SymphonyFrameWork.Core;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     バージョンログ、出力時バージョン、出力マニフェストの読み書きを担う。
    /// </summary>
    internal static class AssetStoreToolsVersionLogStore
    {
        /// <summary>
        ///     現在の対象フォルダ設定からバージョンログのパスを組み立てる。
        /// </summary>
        /// <returns> バージョンログのパス。対象フォルダが未設定の場合は空文字。 </returns>
        internal static string GetVersionLogFilePath()
        {
            string root = AssetStoreToolsPackagerData.AssetStoreToolsPath;
            if (string.IsNullOrEmpty(root))
            {
                return string.Empty;
            }

            return CombinePath(root, EditorSymphonyConstant.ASSET_STORE_TOOLS_VERSION_LOG_FILE_NAME);
        }

        /// <summary>
        ///     バージョンログを読み込む。ファイルが存在しない場合は現在のディレクトリ一覧から生成する。
        /// </summary>
        /// <remarks>
        ///     壊れたファイルを既定値へフォールバックさせない。全ディレクトリのリビジョンが巻き戻り、
        ///     インポート先が「更新なし」と誤判定するため。
        /// </remarks>
        /// <returns> 正規化済みのバージョンログ。読み込みに失敗した場合はnull。 </returns>
        internal static AssetStoreToolsVersionLog Load()
        {
            string logPath = GetVersionLogFilePath();
            if (string.IsNullOrEmpty(logPath))
            {
                Debug.LogError($"{LOG_PREFIX}\nAssetStoreToolsフォルダのパスが設定されていません。");
                return null;
            }

            if (!File.Exists(logPath))
            {
                return CreateVersionLogFile(logPath);
            }

            try
            {
                string json = File.ReadAllText(logPath);
                var log = JsonConvert.DeserializeObject<AssetStoreToolsVersionLog>(json);

                if (log == null)
                {
                    Debug.LogError($"{LOG_PREFIX}\nバージョンログの内容が空です: {logPath}");
                    return null;
                }

                return log.Normalize();
            }
            catch (JsonException e)
            {
                Debug.LogError($"{LOG_PREFIX}\nバージョンログの解析に失敗しました: {logPath}\n{e.Message}");
                return null;
            }
            catch (IOException e)
            {
                Debug.LogError($"{LOG_PREFIX}\nバージョンログの読み込みに失敗しました: {logPath}\n{e.Message}");
                return null;
            }
        }

        /// <summary>
        ///     バージョンログをJSONへ保存する。
        /// </summary>
        /// <param name="log"> 保存するバージョンログ。正規化してから書き出す。 </param>
        /// <returns> 書き込みに成功した場合はtrue。 </returns>
        internal static bool Save(AssetStoreToolsVersionLog log)
        {
            if (log == null)
            {
                return false;
            }

            string logPath = GetVersionLogFilePath();
            if (string.IsNullOrEmpty(logPath))
            {
                Debug.LogError($"{LOG_PREFIX}\nAssetStoreToolsフォルダのパスが設定されていません。");
                return false;
            }

            return TryWriteJson(logPath, log.Normalize(), "バージョンログ");
        }

        /// <summary>
        ///     パッケージ対象ディレクトリへ出力時バージョンを書き出す。
        /// </summary>
        /// <param name="directoryPath"> 書き出し先のディレクトリパス。 </param>
        /// <param name="name"> パッケージ対象ディレクトリの名前。 </param>
        /// <param name="version"> 出力する時点のリビジョン。 </param>
        /// <returns> 書き込みに成功した場合はtrue。 </returns>
        internal static bool TryWriteExportedVersion(string directoryPath, string name, int version)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                return false;
            }

            var exportedVersion = new AssetStoreToolsExportedVersion
            {
                Name = name,
                Version = version,
                ExportedAt = AssetStoreToolsVersionLog.CreateTimestamp(),
            };

            string path = CombinePath(
                directoryPath,
                EditorSymphonyConstant.ASSET_STORE_TOOLS_EXPORTED_VERSION_FILE_NAME);

            return TryWriteJson(path, exportedVersion, "出力時バージョン");
        }

        /// <summary>
        ///     出力先フォルダへパッケージ一覧のマニフェストを書き出す。
        /// </summary>
        /// <param name="exportFullPath"> 出力先フォルダの絶対パス。 </param>
        /// <param name="manifest"> 書き出すマニフェスト。 </param>
        /// <returns> 書き込みに成功した場合はtrue。 </returns>
        internal static bool TryWriteManifest(
            string exportFullPath,
            AssetStoreToolsPackageManifest manifest)
        {
            if (string.IsNullOrEmpty(exportFullPath) || manifest == null)
            {
                return false;
            }

            string path = Path.Combine(
                exportFullPath,
                EditorSymphonyConstant.ASSET_STORE_TOOLS_MANIFEST_FILE_NAME);

            return TryWriteJson(path, manifest, "パッケージマニフェスト");
        }

        private const string LOG_PREFIX = "[" + nameof(AssetStoreToolsVersionLogStore) + "]";

        /// <summary>
        ///     バージョンログを新規生成する。現在存在するディレクトリをリビジョン1で登録する。
        /// </summary>
        /// <param name="logPath"> 生成するバージョンログのパス。 </param>
        /// <returns> 生成した正規化済みのバージョンログ。生成に失敗した場合はnull。 </returns>
        private static AssetStoreToolsVersionLog CreateVersionLogFile(string logPath)
        {
            string root = AssetStoreToolsPackagerData.AssetStoreToolsPath;
            if (!Directory.Exists(root))
            {
                Debug.LogError($"{LOG_PREFIX}\nAssetStoreToolsフォルダが存在しません: {root}");
                return null;
            }

            IEnumerable<string> directoryNames = Directory
                .GetDirectories(root)
                .Select(Path.GetFileName);

            AssetStoreToolsVersionLog log = AssetStoreToolsVersionLog.CreateDefault(directoryNames);

            if (!TryWriteJson(logPath, log.Normalize(), "バージョンログ"))
            {
                return null;
            }

            Debug.Log($"{LOG_PREFIX}\nバージョンログを生成しました: {logPath}");
            return log.Normalize();
        }

        /// <summary>
        ///     オブジェクトを整形済みJSONとして書き出す。
        /// </summary>
        /// <param name="path"> 書き出し先のパス。 </param>
        /// <param name="value"> 書き出す内容。 </param>
        /// <param name="displayName"> 失敗ログへ出す対象の呼び名。 </param>
        /// <returns> 書き込みに成功した場合はtrue。 </returns>
        private static bool TryWriteJson(string path, object value, string displayName)
        {
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                Debug.LogError($"{LOG_PREFIX}\n{displayName}の書き出し先が存在しません: {directory}");
                return false;
            }

            try
            {
                string json = JsonConvert.SerializeObject(value, Formatting.Indented);
                File.WriteAllText(path, json);
                return true;
            }
            catch (IOException e)
            {
                Debug.LogError($"{LOG_PREFIX}\n{displayName}の保存に失敗しました: {path}\n{e.Message}");
                return false;
            }
        }

        /// <summary> ディレクトリとファイル名をスラッシュ区切りで連結する。 </summary>
        private static string CombinePath(string directory, string fileName)
            => directory.Replace("\\", "/").TrimEnd('/') + "/" + fileName;
    }
}
