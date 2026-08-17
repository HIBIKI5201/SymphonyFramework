using Newtonsoft.Json;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;

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
        #region 外部向けAPI

        /// <summary>
        ///     現在の対象フォルダ設定からバージョンログのパスを組み立てる。
        /// </summary>
        /// <returns> バージョンログのパス。対象フォルダが未設定の場合は空文字。 </returns>
        internal static string GetVersionLogFilePath()
        {
            string root = AssetStoreToolsPackagerData.AssetStoreToolsPath;
            // 未設定のルートを有効な相対パスとして扱わない。
            if (string.IsNullOrEmpty(root)) { return string.Empty; }

            return CombinePath(root, EditorSymphonyConstant.ASSET_STORE_TOOLS_VERSION_LOG_FILE_NAME);
        }

        /// <summary>
        ///     バージョンログを読み込む。
        /// </summary>
        /// <remarks>
        ///     ファイルが存在しない場合は現在のディレクトリ一覧から生成する。
        ///     壊れたファイルを既定値へフォールバックさせない。全ディレクトリのリビジョンが巻き戻り、
        ///     インポート先が「更新なし」と誤判定するため。
        /// </remarks>
        /// <returns> 正規化済みのバージョンログ。読み込みに失敗した場合はnull。 </returns>
        internal static AssetStoreToolsVersionLog Load()
        {
            string logPath = GetVersionLogFilePath();
            // 対象フォルダが未設定なら、バージョンログの読み込み先を決められない。
            if (string.IsNullOrEmpty(logPath))
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nAssetStoreToolsフォルダのパスが設定されていません。", LogKindEnum.Error);
                return null;
            }

            // 初回利用時は、現在存在するディレクトリから初期ログを生成する。
            if (!File.Exists(logPath)) { return CreateVersionLogFile(logPath); }

            try
            {
                string json = File.ReadAllText(logPath);
                AssetStoreToolsVersionLog log =
                    JsonConvert.DeserializeObject<AssetStoreToolsVersionLog>(json);

                // 空ファイルやnullリテラルから既定値を生成すると、既存リビジョンが巻き戻る。
                if (log == null)
                {
                    SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nバージョンログの内容が空です: {logPath}", LogKindEnum.Error);
                    return null;
                }

                return log.Normalize();
            }
            catch (JsonException e)
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nバージョンログの解析に失敗しました: {logPath}\n{e.Message}", LogKindEnum.Error);
                return null;
            }
            catch (IOException e)
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nバージョンログの読み込みに失敗しました: {logPath}\n{e.Message}", LogKindEnum.Error);
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
            // 保存対象が無ければ既存ログを上書きしない。
            if (log == null) { return false; }

            string logPath = GetVersionLogFilePath();
            // 対象フォルダが未設定なら、バージョンログの保存先を決められない。
            if (string.IsNullOrEmpty(logPath))
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nAssetStoreToolsフォルダのパスが設定されていません。", LogKindEnum.Error);
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
            // 出力先が無ければバージョンファイルの配置先を決められない。
            if (string.IsNullOrEmpty(directoryPath)) { return false; }

            // パッケージへ同梱する時点のリビジョンと記録時刻を固定する。
            AssetStoreToolsExportedVersion exportedVersion = new()
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
        ///     パッケージ対象ディレクトリの出力時バージョンを読み込む。
        /// </summary>
        /// <remarks>
        ///     インポート先では、このファイルが「今そこに入っているパッケージのリビジョン」を表す。
        ///     ファイルが無い場合は、そのパッケージが未導入であることを意味する。
        /// </remarks>
        /// <param name="directoryPath"> 読み込み元のディレクトリパス。 </param>
        /// <param name="version"> 読み込んだリビジョン。読み込めない場合は0。 </param>
        /// <returns> ファイルが存在し読み込めた場合はtrue。 </returns>
        internal static bool TryReadExportedVersion(string directoryPath, out int version)
        {
            version = 0;

            // 読み込み元が無ければ未導入として扱う。
            if (string.IsNullOrEmpty(directoryPath)) { return false; }

            string path = CombinePath(
                directoryPath,
                EditorSymphonyConstant.ASSET_STORE_TOOLS_EXPORTED_VERSION_FILE_NAME);

            // バージョンファイルが無いパッケージは未導入として扱う。
            if (!File.Exists(path)) { return false; }

            try
            {
                AssetStoreToolsExportedVersion exportedVersion =
                    JsonConvert.DeserializeObject<AssetStoreToolsExportedVersion>(
                        File.ReadAllText(path));

                // 空ファイルやnullリテラルは、導入済みリビジョンとして扱えない。
                if (exportedVersion == null)
                {
                    SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\n出力時バージョンの内容が空です: {path}", LogKindEnum.Error);
                    return false;
                }

                version = exportedVersion.Version;
                return true;
            }
            catch (JsonException e)
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\n出力時バージョンの解析に失敗しました: {path}\n{e.Message}", LogKindEnum.Error);
                return false;
            }
            catch (IOException e)
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\n出力時バージョンの読み込みに失敗しました: {path}\n{e.Message}", LogKindEnum.Error);
                return false;
            }
        }

        /// <summary>
        ///     出力先フォルダのマニフェストを読み込む。
        /// </summary>
        /// <param name="exportDirectoryPath"> 読み込み元の出力先フォルダ。 </param>
        /// <returns> 読み込んだマニフェスト。読み込めない場合はnull。 </returns>
        internal static AssetStoreToolsPackageManifest LoadManifest(string exportDirectoryPath)
        {
            // 出力済みフォルダが未指定なら、マニフェストを特定できない。
            if (string.IsNullOrEmpty(exportDirectoryPath)) { return null; }

            string path = Path.Combine(
                exportDirectoryPath,
                EditorSymphonyConstant.ASSET_STORE_TOOLS_MANIFEST_FILE_NAME);

            // 統合パッケージだけの出力ではマニフェストを作らないため、未検出は正常系とする。
            if (!File.Exists(path)) { return null; }

            try
            {
                AssetStoreToolsPackageManifest manifest =
                    JsonConvert.DeserializeObject<AssetStoreToolsPackageManifest>(
                        File.ReadAllText(path));

                // ファイルは存在しても内容が無ければ、候補を構築できないことを記録する。
                if (manifest == null) { SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nマニフェストの内容が空です: {path}", LogKindEnum.Error); }

                return manifest;
            }
            catch (JsonException e)
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nマニフェストの解析に失敗しました: {path}\n{e.Message}", LogKindEnum.Error);
                return null;
            }
            catch (IOException e)
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nマニフェストの読み込みに失敗しました: {path}\n{e.Message}", LogKindEnum.Error);
                return null;
            }
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
            // 出力先か内容が無ければ、意味のないマニフェストを生成しない。
            if (string.IsNullOrEmpty(exportFullPath) || manifest == null) { return false; }

            string path = Path.Combine(
                exportFullPath,
                EditorSymphonyConstant.ASSET_STORE_TOOLS_MANIFEST_FILE_NAME);

            return TryWriteJson(path, manifest, "パッケージマニフェスト");
        }

        #endregion

        #region 内部処理

        private const string LOG_PREFIX = "[" + nameof(AssetStoreToolsVersionLogStore) + "]";

        /// <summary>
        ///     バージョンログを新規生成する。
        /// </summary>
        /// <remarks> 現在存在するディレクトリをリビジョン1で登録する。 </remarks>
        /// <param name="logPath"> 生成するバージョンログのパス。 </param>
        /// <returns> 生成した正規化済みのバージョンログ。生成に失敗した場合はnull。 </returns>
        private static AssetStoreToolsVersionLog CreateVersionLogFile(string logPath)
        {
            string root = AssetStoreToolsPackagerData.AssetStoreToolsPath;
            // 初期値の列挙元が存在しなければ、空ログを生成して設定誤りを隠さない。
            if (!Directory.Exists(root))
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nAssetStoreToolsフォルダが存在しません: {root}", LogKindEnum.Error);
                return null;
            }

            IEnumerable<string> directoryNames = Directory
                .GetDirectories(root)
                .Select(Path.GetFileName);

            AssetStoreToolsVersionLog log = AssetStoreToolsVersionLog.CreateDefault(directoryNames);

            // 永続化できなかったログを返すと、メモリ上だけでリビジョン管理が始まるため失敗とする。
            if (!TryWriteJson(logPath, log.Normalize(), "バージョンログ")) { return null; }

            SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nバージョンログを生成しました: {logPath}");
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
            // 保存先が無い場合は、親ディレクトリを暗黙に生成せず設定誤りとして扱う。
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\n{displayName}の書き出し先が存在しません: {directory}", LogKindEnum.Error);
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
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\n{displayName}の保存に失敗しました: {path}\n{e.Message}", LogKindEnum.Error);
                return false;
            }
        }

        /// <summary>
        ///     ディレクトリとファイル名をスラッシュ区切りで連結する。
        /// </summary>
        private static string CombinePath(string directory, string fileName)
            => directory.Replace("\\", "/").TrimEnd('/') + "/" + fileName;

        #endregion
    }
}
