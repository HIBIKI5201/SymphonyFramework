using Newtonsoft.Json;
using SymphonyFramework.Core;
using SymphonyFramework.Debugger.Logger;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     PackagerConfig.jsonの読み書きを担う。
    /// </summary>
    internal static class AssetStoreToolsPackagerConfigStore
    {
        #region 外部向けAPI

        /// <summary>
        ///     現在の対象フォルダ設定から設定ファイルのパスを組み立てる。
        /// </summary>
        /// <returns> 設定ファイルのパス。対象フォルダが未設定の場合は空文字。 </returns>
        internal static string GetConfigFilePath()
        {
            string root = AssetStoreToolsPackagerData.AssetStoreToolsPath;
            // 未設定のルートを有効な相対パスとして扱わない。
            if (string.IsNullOrEmpty(root)) { return string.Empty; }

            return CombinePath(root, EditorSymphonyConstant.ASSET_STORE_TOOLS_CONFIG_FILE_NAME);
        }

        /// <summary>
        ///     設定を読み込む。
        /// </summary>
        /// <remarks> ファイルが存在しない場合は既定値で生成する。 </remarks>
        /// <returns> 正規化済みの設定。読み込みに失敗した場合はnull。 </returns>
        internal static AssetStoreToolsPackagerConfig Load()
        {
            string configPath = GetConfigFilePath();
            // 対象フォルダが未設定なら、設定ファイルの読み込み先を決められない。
            if (string.IsNullOrEmpty(configPath))
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nAssetStoreToolsフォルダのパスが設定されていません。", LogKindEnum.Error);
                return null;
            }

            // 初回利用時は既定値で生成する。
            if (!File.Exists(configPath)) { return CreateConfigFile(configPath); }

            try
            {
                string json = File.ReadAllText(configPath);
                AssetStoreToolsPackagerConfig config =
                    JsonConvert.DeserializeObject<AssetStoreToolsPackagerConfig>(json);

                // 空ファイルやnullリテラルはnullになる。既定値へフォールバックすると
                // 除外設定が無視されるため、壊れたファイルとして扱う。
                if (config == null)
                {
                    SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\n設定ファイルの内容が空です: {configPath}", LogKindEnum.Error);
                    return null;
                }

                return config.Normalize();
            }
            catch (JsonException e)
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\n設定ファイルの解析に失敗しました: {configPath}\n{e.Message}", LogKindEnum.Error);
                return null;
            }
            catch (IOException e)
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\n設定ファイルの読み込みに失敗しました: {configPath}\n{e.Message}", LogKindEnum.Error);
                return null;
            }
        }

        /// <summary>
        ///     設定をJSONへ保存する。
        /// </summary>
        /// <param name="config"> 保存する設定。正規化してから書き出す。 </param>
        /// <returns> 書き出せた場合はtrue。 </returns>
        internal static bool Save(AssetStoreToolsPackagerConfig config)
        {
            // 保存対象が無ければ既存設定を上書きしない。
            if (config == null) { return false; }

            string configPath = GetConfigFilePath();
            // 対象フォルダが未設定なら、設定ファイルの保存先を決められない。
            if (string.IsNullOrEmpty(configPath))
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nAssetStoreToolsフォルダのパスが設定されていません。", LogKindEnum.Error);
                return false;
            }

            // 保存先が無い場合は、別の場所へ暗黙に生成しない。
            if (!TryEnsureDirectory(configPath)) { return false; }

            try
            {
                string json = JsonConvert.SerializeObject(config.Normalize(), Formatting.Indented);
                File.WriteAllText(configPath, json);
                AssetDatabase.Refresh();
                return true;
            }
            catch (IOException e)
            {
                SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\n設定ファイルの保存に失敗しました: {configPath}\n{e.Message}", LogKindEnum.Error);
                return false;
            }
        }

        #endregion

        #region 内部処理

        private const string LOG_PREFIX = "[" + nameof(AssetStoreToolsPackagerConfigStore) + "]";

        /// <summary>
        ///     設定ファイルを新規生成する。
        /// </summary>
        /// <param name="configPath"> 生成する設定ファイルのパス。 </param>
        /// <returns> 生成した正規化済みの設定。生成に失敗した場合はnull。 </returns>
        private static AssetStoreToolsPackagerConfig CreateConfigFile(string configPath)
        {
            // 対象フォルダが無い場合は、設定を別の場所へ生成しない。
            if (!TryEnsureDirectory(configPath)) { return null; }

            AssetStoreToolsPackagerConfig config = AssetStoreToolsPackagerConfig.CreateDefault();

            // 生成できなかった設定を返さない。返すと、除外設定が保存されていないまま
            // パッケージ出力へ進み、次回の読み込みでも同じ状態が繰り返される。
            if (!Save(config)) { return null; }

            SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\n設定ファイルを生成しました: {configPath}");

            return config.Normalize();
        }

        /// <summary>
        ///     設定ファイルを置くディレクトリの存在を確認する。
        /// </summary>
        /// <returns> ディレクトリが存在すればtrue。 </returns>
        private static bool TryEnsureDirectory(string configPath)
        {
            string directory = Path.GetDirectoryName(configPath);
            // 設定済みの保存先が存在する場合だけ、ファイル書き込みを許可する。
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) { return true; }

            SymphonyDebugLogger.LogDirect($"{LOG_PREFIX}\nAssetStoreToolsフォルダが存在しません: {directory}", LogKindEnum.Error);
            return false;
        }

        /// <summary>
        ///     ディレクトリとファイル名をスラッシュ区切りで連結する。
        /// </summary>
        private static string CombinePath(string directory, string fileName)
            => directory.Replace("\\", "/").TrimEnd('/') + "/" + fileName;

        #endregion
    }
}
