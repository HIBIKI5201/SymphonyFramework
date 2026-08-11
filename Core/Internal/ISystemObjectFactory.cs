using UnityEngine;

namespace SymphonyFrameWork.Core
{
    /// <summary>
    ///     シーン遷移をまたいで保持するシステムオブジェクトの生成契約。
    /// </summary>
    internal interface ISystemObjectFactory
    {
        #region 外部向けAPI

        /// <summary>
        ///     指定名のシステムオブジェクトを生成する。
        /// </summary>
        /// <param name="name"> 生成するGameObjectの名前。 </param>
        /// <returns> 生成したGameObject。 </returns>
        GameObject CreateObject(string name);

        /// <summary>
        ///     指定名のシステムオブジェクトへコンポーネントを追加して生成する。
        /// </summary>
        /// <typeparam name="T"> 生成するコンポーネントの型。 </typeparam>
        /// <param name="name"> 生成するGameObjectの名前。 </param>
        /// <returns> 生成したコンポーネント。 </returns>
        T CreateComponent<T>(string name) where T : Component;

        #endregion
    }
}
