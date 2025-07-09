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
    ///     シングルトンのインスタンスを統括して管理するクラスです。
    ///     このクラスを通じて、Component、interface、または通常のクラスのインスタンスを登録し、アプリケーションのどこからでもアクセスできるようにします。
    ///     インスタンスを一時的にシーンロードから切り離したい時にも使用できます。
    /// </summary>
    public static class ServiceLocator
    {
        /// <summary>
        ///     登録するインスタンスの種類を定義します。
        /// </summary>
        public enum LocateType
        {
            /// <summary>
            ///     通常のシングルトンとして登録します。
            ///     Componentの場合、ServiceLocatorのGameObjectの子オブジェクトになります。
            /// </summary>
            Singleton,
            /// <summary>
            ///     インスタンスをServiceLocatorに登録しますが、親子関係は設定しません。
            /// </summary>
            Locator
        }

        /// <summary>
        ///     ServiceLocatorのシングルトンインスタンス（GameObject）を返します。
        /// </summary>
        public static GameObject Instance
        {
            get => _data.Value.Instance;
        }

        // Lazy<T> を使用して、ServiceLocatorData のインスタンスが必要になるまで生成を遅延させます。
        private static Lazy<ServiceLocatorData> _data;

        /// <summary>
        ///     ServiceLocatorのデータを保持するためのMonoBehaviourです。
        ///     Unityのライフサイクルイベントを利用するために使用されます。
        /// </summary>
        private class ServiceLocatorData : MonoBehaviour
        {
            public GameObject Instance => _instance;
            public Dictionary<Type, object> SingletonObjects => _singletonObjects;
            public Dictionary<Type, Action> WaitingActions => _waitingActions;
            public Dictionary<Type, Delegate> WaitingActionsWithInstance => _waitingActionsWithInstance;

            [Tooltip("シングルトン化するインスタンスのコンテナとなるGameObject")]
            private GameObject _instance;

            [Tooltip("登録されているインスタンスを型をキーにして保持する辞書")]
            private readonly Dictionary<Type, object> _singletonObjects = new();

            [Tooltip("インスタンス登録まで待機してから実行されるコールバックアクションを保持する辞書")]
            private readonly Dictionary<Type, Action> _waitingActions = new();
            [Tooltip("インスタンス登録まで待機し、登録されたインスタンスを引数として受け取るコールバックアクションを保持する辞書")]
            private readonly Dictionary<Type, Delegate> _waitingActionsWithInstance = new();

            public void Init(GameObject instance)
            {
                _instance = instance;
            }
        }

        /// <summary>
        ///     ServiceLocatorを初期化します。
        ///     SymphonyCoreSystemから呼び出されることを想定しています。
        /// </summary>
        internal static void Initialize()
        {
            _data = new Lazy<ServiceLocatorData>(CreateData);
        }

        /// <summary>
        ///     指定されたインスタンスをロケーターに登録します。
        /// </summary>
        /// <typeparam name="T">登録するインスタンスの型。クラスである必要があります。</typeparam>
        /// <param name="instance">登録するインスタンス。</param>
        /// <param name="type">登録の種類（SingletonまたはLocator）。</param>
        public static void SetInstance<T>(T instance, LocateType type = LocateType.Locator) where T : class
        {
            // 既に同じ型のインスタンスが登録されている場合は、新しいインスタンスを登録せずに処理を中断します。
            // 登録しようとしたインスタンスがComponentだった場合は、そのGameObjectを破棄します。
            if (!_data.Value.SingletonObjects.TryAdd(typeof(T), instance))
            {
                if (instance is Component component)
                {
                    Object.Destroy(component.gameObject);
                }
                return;
            }

#if UNITY_EDITOR
            var instanceName = instance is Component c ? c.name : instance.GetType().Name;
            //ログを出力
            if (EditorPrefs.GetBool(EditorSymphonyConstant.ServiceLocatorSetInstanceKey,
                EditorSymphonyConstant.ServiceLocatorSetInstanceDefault))
                Debug.Log($"{typeof(T).Name}クラスの{instanceName}が" +
                    $"{type switch { LocateType.Locator => "ロケート", LocateType.Singleton => "シングルトン", _ => string.Empty }}登録されました");
#endif

            #region 待機中のイベントを発火
            // この型のインスタンスが登録されるのを待っていたアクションがあれば、ここで実行します。
            if (_data.Value.WaitingActions.TryGetValue(typeof(T), out var waitingAction))
            {
                waitingAction?.Invoke();
                _data.Value.WaitingActions.Remove(typeof(T)); //実行したら解放
            }

            // 同様に、インスタンスを引数に取る待機アクションも実行します。
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

            // 登録タイプがSingletonで、かつインスタンスがComponentの場合、
            // ServiceLocatorのGameObjectの子要素にして、シーン内で管理しやすくします。
            if (type == LocateType.Singleton && instance is Component componentInstance)
            {
                componentInstance.transform.SetParent(Instance.transform);
            }
        }

        /// <summary>
        ///     指定したインスタンスと同じ型の登録済みインスタンスを破棄します。
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型。</typeparam>
        /// <param name="instance">破棄の対象となるインスタンス。</param>
        public static void DestroyInstance<T>(T instance) where T : class
        {
            if (instance == null) return;

            // 渡されたインスタンスが、指定された型で登録されているものと同一であるかを確認します。
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
        ///     指定した型のインスタンスを破棄します。
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型。</typeparam>
        public static void DestroyInstance<T>() where T : class
        {
            if (_data.Value.SingletonObjects.TryGetValue(typeof(T), out var md))
            {
                // インスタンスがComponentなら、GameObjectごと破棄します。
                if (md is Component component)
                {
                    Object.Destroy(component.gameObject);
                }
                // IDisposableを実装していれば、Disposeメソッドを呼び出してリソースを解放します。
                else if (md is IDisposable disposable)
                {
                    disposable.Dispose();
                }

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
        ///     登録されたインスタンスを取得します。
        /// </summary>
        /// <typeparam name="T">取得したいインスタンスの型。</typeparam>
        /// <returns>指定した型のインスタンス。見つからない場合や破棄済みの場合はnull。</returns>
        public static T GetInstance<T>() where T : class
        {
#if UNITY_EDITOR
            //ログを出力
            if (EditorPrefs.GetBool(EditorSymphonyConstant.ServiceLocatorGetInstanceKey,
                EditorSymphonyConstant.ServiceLocatorGetInstanceDefault))
                SymphonyDebugLog.AddText($"ServiceLocator\n{typeof(T).Name}の取得がリクエストされました。");
#endif

            if (_data.Value.SingletonObjects.TryGetValue(typeof(T), out var md))
            {
                // Unityのオブジェクト（Componentなど）は、C#的にはnullでなくても破棄されている場合があるため、
                // Componentの場合はその状態をチェックします。
                if (md is Component component && !component)
                {
                    OutputLog($"{typeof(T).Name} は破棄されています。", SymphonyDebugLog.LogKind.Warning);
                    return null;
                }

                // インスタンスがnullでなければ、要求された型にキャストして返します。
                if (md != null)
                {
                    OutputLog($"正常に行われました。");
                    return (T)md;
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
        ///     指定した型のオブジェクトが登録された時に、指定したアクションを実行します。
        ///     既に登録済みの場合は即座に実行されます。
        /// </summary>
        /// <typeparam name="T">待機するインスタンスの型。</typeparam>
        /// <param name="action">実行するアクション。</param>
        public static void RegisterAfterLocate<T>(Action action) where T : class
        {
            // 既にインスタンスが登録済みであれば、即座にアクションを実行します。
            if (_data.Value.SingletonObjects.ContainsKey(typeof(T)))
            {
                action?.Invoke();
                return;
            }

            // まだ登録されていなければ、待機リストに追加します。
            if (!_data.Value.WaitingActions.TryAdd(typeof(T), action))
            {
                _data.Value.WaitingActions[typeof(T)] += action;
            }
        }

        /// <summary>
        ///     指定した型のオブジェクトが登録された時に、そのインスタンスを引数としてアクションを実行します。
        ///     既に登録済みの場合は即座に実行されます。
        /// </summary>
        /// <typeparam name="T">待機するインスタンスの型。</typeparam>
        /// <param name="action">実行するアクション。引数として登録されたインスタンスを受け取ります。</param>
        public static void RegisterAfterLocate<T>(Action<T> action) where T : class
        {
            // 既にインスタンスが登録済みであれば、そのインスタンスを引数にして即座にアクションを実行します。
            if (_data.Value.SingletonObjects.TryGetValue(typeof(T), out var instance))
            {
                action?.Invoke((T)instance);
                return;
            }

            // まだ登録されていなければ、待機リストに追加します。
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
        ///     指定した型のインスタンスが登録されるまで非同期で待機し、取得します。
        /// </summary>
        /// <typeparam name="T">取得したいインスタンスの型。</typeparam>
        /// <param name="grace">最大待機時間（秒）。この時間を超えるとnullを返します。</param>
        /// <param name="token">キャンセルトークン。</param>
        /// <returns>指定した型のインスタンス。見つからない場合はnull。</returns>
        public static async Task<T> GetInstanceAsync<T>(byte grace = 120, CancellationToken token = default) where T : class
        {
            float time = Time.time;

            // 指定された時間が経過するまで、またはインスタンスが取得できるまでループします。
            while (grace + time > Time.time)
            {
                T result = GetInstance<T>();

                if (result != null)
                {
                    return result;
                }

                // 次のフレームまで待機します。
                await Awaitable.NextFrameAsync(token);
            }

            return null;
        }

        /// <summary>
        ///     ServiceLocatorDataのインスタンスを生成し、初期化します。
        /// </summary>
        /// <returns>初期化済みのServiceLocatorDataインスタンス。</returns>
        private static ServiceLocatorData CreateData()
        {
            var instance = new GameObject(nameof(ServiceLocator));
            ServiceLocatorData data = instance.AddComponent<ServiceLocatorData>();
            data.Init(instance);
            SymphonyCoreSystem.MoveObjectToSymphonySystem(instance);
            return data;
        }
    }
}