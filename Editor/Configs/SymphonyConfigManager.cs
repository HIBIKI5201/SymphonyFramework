using System;
using System.IO;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     コンフィグ用データを生成するクラス
    /// </summary>
    public static class SymphonyConfigManager
    {
        /// <summary> Runtime用とEditor用のすべての設定アセットが存在することを保証する。 </summary>
        /// <returns> AssetDatabaseの更新が必要な変更を行った場合はtrue。 </returns>
        internal static bool AllConfigCheck()
        {
            bool hasAssetChanges = false;

            // Runtime用 (ScriptableObject)
            hasAssetChanges |= FileCheck<SceneLoadConfig>();
            hasAssetChanges |= FileCheck<AudioConfig>();
            hasAssetChanges |= FileCheck<SaveDataConfig>();

            // Editor用 (ScriptableSingleton)
            // GetConfigを呼ぶだけで、アセットが存在しなければ自動生成、あればロードされる
            CreateUserSettingFolder();
            EditorFileCheck<AutoEnumGeneratorConfig>();
            EditorFileCheck<SymphonyUserSettingConfig>();

            return hasAssetChanges;
        }

        /// <summary>
        ///     Runtimeファイルが存在するか確認する (Resources内)
        /// </summary>
        /// <typeparam name="T"> 存在を保証するRuntime設定アセットの型。 </typeparam>
        /// <returns> 設定アセットまたは配置先フォルダを生成した場合はtrue。 </returns>
        private static bool FileCheck<T>() where T : ScriptableObject
        {
            string path = SymphonyConfigLocator.GetFullPath<T>();
            if (path == null)
            {
                Debug.LogWarning(typeof(T).Name + " doesn't exist!");
                return false;
            }

            // ファイルが存在するなら終了
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                return false;
            }

            string directory = SymphonyConstant.RESOURCES_RUNTIME_PATH;

            // リソースフォルダがなければ生成
            CreateResourcesFolder(directory);

            // 対象のアセットを生成してResources内に配置
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);

            // アセットを保存
            AssetDatabase.SaveAssets();

            SymphonyDebugLogger.LogDirect($"'{path}' に新しい {typeof(T).Name} を作成しました。");
            return true;
        }

        /// <summary> Editor用ScriptableSingletonを取得して必要に応じて生成する。 </summary>
        private static void EditorFileCheck<T>() where T : ScriptableSingleton<T>
        {
            _ = SymphonyEditorConfigLocator.GetConfig<T>();
        }

        /// <summary>
        ///     リソースフォルダが無ければ生成
        /// </summary>
        private static void CreateResourcesFolder(string resourcesPath)
        {
            //リソースがなければ生成
            if (Directory.Exists(resourcesPath))
            {
                return;
            }

            Directory.CreateDirectory(resourcesPath);
        }

        /// <summary> UserSettings内にFramework設定の保存先フォルダを生成する。 </summary>
        private static void CreateUserSettingFolder()
        {
            try
            {
                Directory.CreateDirectory(EditorSymphonyConstant.USER_SETTING_FILE_PATH);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[{nameof(SymphonyConfigManager)}] UserSettingsの保存先を生成できませんでした。" +
                    $" path: '{EditorSymphonyConstant.USER_SETTING_FILE_PATH}', reason: '{exception.Message}'");
            }
        }
    }
}
