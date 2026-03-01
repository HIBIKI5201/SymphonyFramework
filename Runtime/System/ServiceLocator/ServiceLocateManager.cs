using SymphonyFrameWork.Core;
using System;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.System.ServiceLocate
{
    public class ServiceLocateManager
    {
        public ServiceLocateManager(ServiceLocateData data)
        {
            _data = data;
        }

        /// <summary>
        ///     指定されたインスタンスをロケーターに登録します。
        /// </summary>
        /// <typeparam name="T">登録するインスタンスの型。クラスである必要があります。</typeparam>
        /// <param name="instance">登録するインスタンス。</param>
        /// <param name="type">登録の種類（SingletonまたはLocator）。</param>
        public bool RegisterInstance(Type type, object instance, LocateType locateType = LocateType.Singleton)
        {
            // 既に同じ型のインスタンスが登録されている場合は、新しいインスタンスを登録せずに処理を中断する。
            // 中断すると対象をDisposeして終了。
            if (!_data.Add(type, instance))
            {
                Dispose(instance);
                Debug.Log($"{type.Name}は既に登録されています。新しいインスタンスは破棄されました。");
                return false;
            }

#if UNITY_EDITOR
            // デバッグログ。
            if (EditorPrefs.GetBool(EditorSymphonyConstant.ServiceLocatorSetInstanceKey,
                EditorSymphonyConstant.ServiceLocatorSetInstanceDefault))
            {
                string instanceName = instance is Component c ? c.name : instance.GetType().Name;
                Debug.Log($"{type.Name}クラスの{instanceName}が{locateType switch { LocateType.Locator => "ロケート", LocateType.Singleton => "シングルトン", _ => string.Empty }}登録されました");
            }
#endif

            _data.InvokeWaitingAction(type, instance);

            // 登録タイプがSingletonで、かつインスタンスがComponentの場合、
            // ServiceLocatorのGameObjectの子要素にして、シーン内で管理しやすくします。
            if (locateType == LocateType.Singleton && instance is Component componentInstance)
            {
                componentInstance.transform.SetParent(_data.Instance.transform);
            }

            return true;
        }

        /// <summary>
        ///     指定した型のインスタンスをロケーターから登録解除します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool UnregisterInstance(Type type)
        {
            // 渡されたインスタンスが、指定された型で登録されているものと同一であるかを確認します。
            if (!_data.IsLocate(type))
            {
                Debug.LogWarning($"{type.Name}は登録されていません。");
                return false;
            }

            _data.Remove(type);

#if UNITY_EDITOR
            //ログを出力。
            if (EditorPrefs.GetBool(EditorSymphonyConstant.ServiceLocatorDestroyInstanceKey,
                EditorSymphonyConstant.ServiceLocatorDestroyInstanceDefault))
                Debug.Log($"{type.Name}が登録解除されました。");
#endif
            return true;
        }

        /// <summary>
        ///     指定した型のインスタンスを破棄します。
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型。</typeparam>

        public bool DestroyInstance(Type type)
        {
            object instance = _data.Get(type);
            if (instance != null)
            {
                Dispose(instance);
                return true;
            }

            return false;
        }


        private readonly ServiceLocateData _data;

        private bool Dispose(object instance)
        {
            bool isDispose = false;

            if (instance is Component component)
            {
                UnityEngine.Object.Destroy(component.gameObject);
                isDispose = true;
            }

            // IDisposableを実装していれば、Disposeメソッドを呼び出してリソースを解放します。
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
                isDispose = true;
            }

            return isDispose;
        }
    }
}
