using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    public static class SymphonyEditorConfigLocator
    {
        /// <summary>
        ///     それぞれのパスを取得する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetFullPath<T>() where T : ScriptableObject
        {
            var name = SymphonyConfigLocator.GetConfigPathInResources<T>();
            return $"{EditorSymphonyConstant.RESOURCES_EDITOR_PATH}/{name}";
        }

        /// <summary>
        ///     指定した型のアセットを取得する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetConfig<T>() where T : ScriptableObject
        {
            var paths = GetFullPath<T>();
            if (paths == null) return null;

            return AssetDatabase.LoadAssetAtPath<T>(paths);
        }
    }
}