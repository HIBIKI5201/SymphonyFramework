using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using SymphonyFrameWork.Debugger;
using SymphonyFrameWork.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     シングルトンのインスタンスを統括して管理するクラス
    ///     インスタンスを一時的にシーンロードから切り離したい時にも使用できる
    /// </summary>
    public static class ServiceLocator
    {
        public enum LocateType
        {
            Singleton,
            Locator
        }

        public static GameObject Instance
        {
            get => _data.Value.Instance;
        }

        private static Lazy<ServiceLocatorData> _data =
            new Lazy<ServiceLocatorData>(CreateData);

        private class ServiceLocatorData : MonoBehaviour
        {
            public GameObject Instance => _instance;
            public Dictionary<Type, Component> SingletonObjects => _singletonObjects;
            public Dictionary<Type, Action> WaitingActions => _waitingActions;
            public Dictionary<Type, Delegate> WaitingActionsWithInstance => _waitingActionsWithInstance;

            [Tooltip("シングルトン化するインスタンスのコンテナ")]
            private GameObject _instance;

            [Tooltip("シングルトン登録されている型のインスタンス辞書")]
            private readonly Dictionary<Type, Component> _singletonObjects = new();

            [Tooltip("シングルトン登録まで待機してから実行されるイベント")]
            private readonly Dictionary<Type, Action> _waitingActions = new();
            [Tooltip("シングルトン登録まで待機してから実行されるイベントのインスタンスを返すイベント")]
            private readonly Dictionary<Type, Delegate> _waitingActionsWithInstance = new();
        }

        internal static void Initialize()
        {
            _data = new Lazy<ServiceLocatorData> (CreateData);
        }

        /// <summary>
        ///     入れられたコンポーネントをロケーターに登録する
        /// </summary>
        /// <typeparam name="T">登録する型</typeparam>
        /// <param name="instance">インスタンス</param>
        /// <returns>登録が成功したらtrue、失敗したらfalse</returns>
        public static void SetInstance<T>(T instance, LocateType type = LocateType.Locator) where T : Component
        {
            // 既に登録されている場合は追加できない
            if (!_data.Value.SingletonObjects.TryAdd(typeof(T), instance))
            {
                Object.Destroy(instance.gameObject);
                return;
            }

#if UNITY_EDITOR
            //ログを出力
            if (EditorPrefs.GetBool(EditorSymphonyConstant.ServiceLocatorSetInstanceKey,
                EditorSymphonyConstant.ServiceLocatorSetInstanceDefault))
                Debug.Log($"{typeof(T).Name}クラスの{instance.name}が" +
                    $"{type switch { LocateType.Locator => "ロケート", LocateType.Singleton => "シングルトン", _ => string.Empty }}登録されました");
#endif

            #region 待機中のイベントを発火
            if (_data.Value.WaitingActions.TryGetValue(typeof(T), out var waitingAction))
            {
                waitingAction?.Invoke();
                _data.Value.WaitingActions.Remove(typeof(T)); //実行したら解放
            }

            if (_data.Value.WaitingActionsWithInstance
                .TryGetValue(typeof(T), out var del))
            {
                if (del is Action<T> action)
                {
                    action.Invoke(instance);
                }
                _data.Value.WaitingActionsWithInstance.Remove(typeof(T)); //実行したら解放
            }

            #endregion

            if (type == LocateType.Singleton) //もしシングルトンなら親をLocatorに設定
            {
                instance.transform.SetParent(Instance.transform);
            }
        }

        /// <summary>
        ///     指定したインスタンスを破棄する
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型</typeparam>
        public static void DestroyInstance<T>(T instance) where T : Component
        {
            if (instance == null) return;

            //インスタンスが登録されたコンポーネントか
            if (_data.Value.SingletonObjects
                .TryGetValue(typeof(T), out var md) && md == instance)
            {
                DestroyInstance<T>();
            }
            else
            {
                Debug.LogWarning($"{typeof(T).Name}は登録されていません");
            }
        }

        /// <summary>
        ///     指定した型のインスタンスを破棄する
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型</typeparam>
        public static void DestroyInstance<T>() where T : Component
        {
            if (_data.Value.SingletonObjects.TryGetValue(typeof(T), out var md))
            {
                Object.Destroy(md.gameObject);
                _data.Value.SingletonObjects.Remove(typeof(T));

#if UNITY_EDITOR
                //ログを出力
                if (EditorPrefs.GetBool(EditorSymphonyConstant.ServiceLocatorDestroyInstanceKey,
                    EditorSymphonyConstant.ServiceLocatorDestroyInstanceDefault))
                    Debug.Log($"{typeof(T).Name}が破棄されました");
#endif

            }
            else
            {
                Debug.LogWarning($"{typeof(T).Name}は登録されていません");
            }
        }

        /// <summary>
        ///     登録されたインスタンスを返す
        /// </summary>
        /// <typeparam name="T">取得したいインスタンスの型</typeparam>
        /// <returns>指定した型のインスタンス</returns>
        public static T GetInstance<T>() where T : Component
        {
#if UNITY_EDITOR
            //ログを出力
            if (EditorPrefs.GetBool(EditorSymphonyConstant.ServiceLocatorGetInstanceKey,
                EditorSymphonyConstant.ServiceLocatorGetInstanceDefault))
                SymphonyDebugLog.AddText($"ServiceLocator\n{typeof(T).Name}の取得がリクエストされました。");
#endif

            if (_data.Value.SingletonObjects.TryGetValue(typeof(T), out var md))
            {
                if (md)
                {
                    OutputLog($"正常に行われました。");
                    return md as T;
                }

                OutputLog($"{typeof(T).Name} は破棄されています。", SymphonyDebugLog.LogKind.Warning);
                return null;
            }

            OutputLog($"{typeof(T).Name} は登録されていません。", SymphonyDebugLog.LogKind.Warning);
            return null;

            void OutputLog(string text, SymphonyDebugLog.LogKind kind = SymphonyDebugLog.LogKind.Normal)
            {
#if UNITY_EDITOR
                if (EditorPrefs.GetBool(EditorSymphonyConstant.ServiceLocatorGetInstanceKey,
                    EditorSymphonyConstant.ServiceLocatorGetInstanceDefault))
                {
                    SymphonyDebugLog.AddText(text);
                    SymphonyDebugLog.TextLog(kind);
                }
#endif
            }
        }

        /// <summary>
        ///     オブジェクトが登録された時に実行される
        /// </summary>
        /// <param name="action"></param>
        public static void RegisterAfterLocate<T>(Action action) where T : Component
        {
            //既にロードされていたら終了
            if (_data.Value.SingletonObjects.ContainsKey(typeof(T)))
            {
                action?.Invoke();
                return;
            }

            //ロードされていなければ登録する
            if (!_data.Value.WaitingActions.TryAdd(typeof(T), action))
            {
                _data.Value.WaitingActions[typeof(T)] += action;
            }
        }

        /// <summary>
        ///     オブジェクトが登録された時に実行される
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public static void RegisterAfterLocate<T>(Action<T> action) where T : Component
        {
            //既にロードされていたら終了
            if (_data.Value.SingletonObjects.TryGetValue(typeof(T), out var instance))
            {
                action?.Invoke((T)instance);
                return;
            }

            //ロードされていなければ登録する
            if (_data.Value.WaitingActionsWithInstance.TryGetValue(typeof(T), out var existing))
            {
                _data.Value.WaitingActionsWithInstance[typeof(T)] = Delegate.Combine(existing, action);
            }
            else
            {
                _data.Value.WaitingActionsWithInstance[typeof(T)] = action;
            }
        }

        /// <summary>
        ///     インスタンスが返されるまで待機する
        /// </summary>
        /// <param name="grace">最大待機フレーム数</param>
        /// <param name="token"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<T> GetInstanceAsync<T>(byte grace = 120, CancellationToken token = default) where T : Component
        {
            float time = Time.time;
            
            while (grace + time > Time.time)
            {
                T result = GetInstance<T>();
                
                if (result) //nullでなければ返して終了
                    return result;
                
                await Awaitable.NextFrameAsync(token);
            }

            return null;
        }

        private static ServiceLocatorData CreateData()
        {
            var instance = new GameObject("ServiceLocatorData");
            SymphonyCoreSystem.MoveObjectToSymphonySystem(instance);
            return instance.AddComponent<ServiceLocatorData>();
        }
    }
}