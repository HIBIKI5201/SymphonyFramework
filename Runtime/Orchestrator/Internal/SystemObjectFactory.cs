using SymphonyFrameWork.Core;

using UnityEngine;

namespace SymphonyFrameWork.Orchestrator
{
    /// <summary>
    ///     シーン遷移をまたいで保持するシステムオブジェクトを生成する。
    /// </summary>
    internal sealed class SystemObjectFactory : ISystemObjectFactory
    {
        /// <inheritdoc />
        public GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            Object.DontDestroyOnLoad(gameObject);
            return gameObject;
        }

        /// <inheritdoc />
        public T CreateComponent<T>(string name) where T : Component
        {
            GameObject gameObject = CreateObject(name);
            return gameObject.AddComponent<T>();
        }
    }
}
