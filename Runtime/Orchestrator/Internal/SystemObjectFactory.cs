using SymphonyFrameWork.Core;

using UnityEngine;

namespace SymphonyFrameWork.Orchestrator
{
    /// <summary>
    ///     シーン遷移をまたいで保持するシステムオブジェクトを生成する。
    /// </summary>
    internal sealed class SystemObjectFactory : ISystemObjectFactory
    {
        #region 外部向けAPI

        /// <inheritdoc />
        public GameObject CreateObject(string name)
        {
            // シーン遷移で破棄されない共通の所有先として生成する。
            GameObject gameObject = new(name);
            Object.DontDestroyOnLoad(gameObject);
            return gameObject;
        }

        /// <inheritdoc />
        public T CreateComponent<T>(string name) where T : Component
        {
            // 永続GameObjectへComponentを追加し、生成規則を全サブシステムで統一する。
            GameObject gameObject = CreateObject(name);
            return gameObject.AddComponent<T>();
        }

        #endregion
    }
}
