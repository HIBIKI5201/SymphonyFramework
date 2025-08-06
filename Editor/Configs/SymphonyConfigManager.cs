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
            FileCheck<SceneManagerConfig>(ConfigType.Runtime);
            FileCheck<AudioManagerConfig>(ConfigType.Runtime);
            FileCheck<AutoEnumGeneratorConfig>(ConfigType.Editor);
        }

        /// <summary>
        ///     ファイルが存在するか確認する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private static void FileCheck<T>(ConfigType type) where T : ScriptableObject
        {
            var path = type switch
            {
                ConfigType.Runtime => SymphonyConfigLocator.GetFullPath<T>(),
                ConfigType.Editor => SymphonyEditorConfigLocator.GetFullPath<T>(),
                _ => null
            };
            if (path == null)
            {
                Debug.LogWarning(typeof(T).Name + " doesn't exist!");
                return;
            }

            // ファイルが存在するなら終了
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;

            string directory = type switch
            {
                ConfigType.Runtime => SymphonyConstant.RESOURCES_RUNTIME_PATH,
                ConfigType.Editor => EditorSymphonyConstant.RESOURCES_EDITOR_PATH,
                _ => null
            };

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

        private enum ConfigType : int
        {
            Runtime,
            Editor,
        }
    }
}