using SymphonyFrameWork.Core;
using UnityEngine;

namespace SymphonyFrameWork.Config
{
    public static class SymphonyConfigLocator
    {
        public static string GetConfigPathInResources<T>() where T : ScriptableObject =>
            $"{typeof(T).Name}.asset";

        /// <summary>
        ///     それぞれのパスを取得する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetFullPath<T>() where T : ScriptableObject => 
            $"{SymphonyConstant.RESOURCES_RUNTIME_PATH}/{GetConfigPathInResources<T>()}";
        
        /// <summary>
        ///     指定した型のアセットを取得する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetConfig<T>() where T : ScriptableObject
        {
            var path = $"{SymphonyConstant.SYMPHONY_FRAMEWORK}/{typeof(T).Name}";
            if (path == null) return null;
            
            return Resources.Load<T>(path);
        }
    }
}