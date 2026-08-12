using Newtonsoft.Json;
using SymphonyFrameWork.Core;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     PackagerConfig.jsonの読み書きと、旧ignore.txtからの移行を担う。
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
        /// <remarks> ファイルが存在しない場合は生成し、ignore.txtがあれば内容を移行する。 </remarks>
        /// <returns> 正規化済みの設定。読み込みに失敗した場合はnull。 </returns>
        internal static AssetStoreToolsPackagerConfig Load()
        {
            string configPath = GetConfigFilePath();
            // 対象フォルダが未設定なら、設定ファイルの読み込み先を決められない。
            if (string.IsNullOrEmpty(configPath))
            {
                Debug.LogError($"{LOG_PREFIX}\nAssetStoreToolsフォルダのパスが設定されていません。");
                return null;
            }

            // 初回利用時は既定値を生成し、旧設定があれば同じ処理内で移行する。
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
                    Debug.LogError($"{LOG_PREFIX}\n設定ファイルの内容が空です: {configPath}");
                    return null;
                }

                return config.Normalize();
            }
            catch (JsonException e)
            {
                Debug.LogError($"{LOG_PREFIX}\n設定ファイルの解析に失敗しました: {configPath}\n{e.Message}");
                return null;
            }
            catch (IOException e)
            {
                Debug.LogError($"{LOG_PREFIX}\n設定ファイルの読み込みに失敗しました: {configPath}\n{e.Message}");
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
                Debug.LogError($"{LOG_PREFIX}\nAssetStoreToolsフォルダのパスが設定されていません。");
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
                Debug.LogError($"{LOG_PREFIX}\n設定ファイルの保存に失敗しました: {configPath}\n{e.Message}");
                return false;
            }
        }

        #endregion

        #region 内部処理

        private const string LOG_PREFIX = "[" + nameof(AssetStoreToolsPackagerConfigStore) + "]";

        /// <summary> 移行元となる旧設定ファイルの名前。 </summary>
        /// <remarks> 次のメジャー更新で移行処理ごと削除する。 </remarks>
        private const string IGNORE_FILE_NAME = "ignore.txt";

        /// <summary>
        ///     設定ファイルを新規生成する。
        /// </summary>
        /// <remarks> ignore.txtが存在すれば除外フォルダ名を引き継ぐ。 </remarks>
        /// <param name="configPath"> 生成する設定ファイルのパス。 </param>
        /// <returns> 生成した正規化済みの設定。生成に失敗した場合はnull。 </returns>
        private static AssetStoreToolsPackagerConfig CreateConfigFile(string configPath)
        {
            // 対象フォルダが無い場合は、設定を別の場所へ生成しない。
            if (!TryEnsureDirectory(configPath)) { return null; }

            // 新形式の既定値を基点にして、旧形式から移せる項目だけを上書きする。
            AssetStoreToolsPackagerConfig config = AssetStoreToolsPackagerConfig.CreateDefault();

            string ignorePath = CombinePath(
                AssetStoreToolsPackagerData.AssetStoreToolsPath,
                IGNORE_FILE_NAME);
            bool isMigrated = false;

            // 旧設定が残っている初回生成時だけ、除外フォルダ名を新形式へ引き継ぐ。
            if (File.Exists(ignorePath))
            {
                try
                {
                    config.IgnoredDirectories = ReadIgnoreFile(ignorePath);
                    isMigrated = true;
                }
                catch (IOException e)
                {
                    Debug.LogError($"{LOG_PREFIX}\nignore.txtの読み込みに失敗しました: {ignorePath}\n{e.Message}");
                    return null;
                }
            }

            // 生成できなかった設定を返さない。返すと、除外設定が保存されていないまま
            // パッケージ出力へ進み、次回の読み込みでも同じ状態が繰り返される。
            if (!Save(config)) { return null; }

            // 移行の有無に応じて、旧ファイルの後処理が必要かを利用者へ伝える。
            if (isMigrated)
            {
                Debug.Log($"{LOG_PREFIX}\nignore.txtの内容を移行しました: {configPath}"
                          + $"\n以降ignore.txtは読み込まれません。不要であれば削除してください: {ignorePath}");
            }
            else
            {
                Debug.Log($"{LOG_PREFIX}\n設定ファイルを生成しました: {configPath}");
            }

            return config.Normalize();
        }

        /// <summary>
        ///     ignore.txtから除外フォルダ名を読み出す。
        /// </summary>
        private static List<string> ReadIgnoreFile(string ignorePath)
        {
            List<string> names = new();

            // 旧形式のコメントと空行は設定値ではないため移行しない。
            foreach (string line in File.ReadAllLines(ignorePath))
            {
                string trimmed = line.Trim();
                // 空行と#から始まる説明行は、除外フォルダ名ではないため移行しない。
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#")) { names.Add(trimmed); }
            }

            return names;
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

            Debug.LogError($"{LOG_PREFIX}\nAssetStoreToolsフォルダが存在しません: {directory}");
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
