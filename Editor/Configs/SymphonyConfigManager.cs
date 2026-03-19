using System.IO;
using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     コンフィグ用データを生成するクラス
    /// </summary>
    public static class SymphonyConfigManager
    {
        internal static void AllConfigCheck()
        {
            // Runtime用 (ScriptableObject)
            FileCheck<SceneManagerConfig>();
            FileCheck<AudioManagerConfig>();
            
            // Editor用 (ScriptableSingleton)
            // GetConfigを呼ぶだけで、アセットが存在しなければ自動生成、あればロードされる
            EditorFileCheck<AutoEnumGeneratorConfig>();
        }

        /// <summary>
        ///     Runtimeファイルが存在するか確認する (Resources内)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private static void FileCheck<T>() where T : ScriptableObject
        {
            string path = SymphonyConfigLocator.GetFullPath<T>();
            if (path == null)
            {
                Debug.LogWarning(typeof(T).Name + " doesn't exist!");
                return;
            }

            // ファイルが存在するなら終了
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;

            string directory = SymphonyConstant.RESOURCES_RUNTIME_PATH;

            // リソースフォルダがなければ生成
            CreateResourcesFolder(directory);

            // 対象のアセットを生成してResources内に配置
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);

            // 変更を反映させるためにRefresh()を呼び出す
            AssetDatabase.Refresh();

            // アセットを保存
            AssetDatabase.SaveAssets();

            SymphonyDebugLogger.DirectLog($"'{path}' に新しい {typeof(T).Name} を作成しました。");
        }

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
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
                AssetDatabase.Refresh();
            }
        }
    }
}