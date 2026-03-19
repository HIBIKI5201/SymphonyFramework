using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    public static class SymphonyEditorConfigLocator
    {
        /// <summary>
        ///     指定した型のアセットを取得する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetConfig<T>() where T : ScriptableSingleton<T>
        {
            return ScriptableSingleton<T>.instance;
        }
    }
}