using System.Reflection;
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
            // FilePathAttributeがある場合はそのパスを返す
            var filePathAttr = typeof(T).GetCustomAttribute<FilePathAttribute>();
            if (filePathAttr != null)
            {
                var field = typeof(FilePathAttribute).GetField("filepath", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    return field.GetValue(filePathAttr) as string;
                }
            }

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
            // ScriptableSingletonを継承している場合はinstanceを返す
            var instanceProperty = typeof(T).GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instanceProperty != null)
            {
                return (T)instanceProperty.GetValue(null);
            }

            var paths = GetFullPath<T>();
            if (paths == null) return null;

            return AssetDatabase.LoadAssetAtPath<T>(paths);
        }
    }
}