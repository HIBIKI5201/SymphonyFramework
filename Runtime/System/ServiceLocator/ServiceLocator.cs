using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using SymphonyFrameWork.Debugger;
using SymphonyFrameWork.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     シングルトンのインスタンスを統括して管理するクラスです。
    ///     このクラスを通じて、Component、interface、または通常のクラスのインスタンスを登録し、アプリケーションのどこからでもアクセスできるようにします。
    ///     インスタンスを一時的にシーンロードから切り離したい時にも使用できます。
    /// </summary>
    public static class ServiceLocator
    {
        /// <summary>
        ///     指定されたインスタンスをロケーターに登録します。
        /// </summary>
        /// <typeparam name="T">登録するインスタンスの型。クラスである必要があります。</typeparam>
        /// <param name="instance">登録するインスタンス。</param>
        /// <param name="type">登録の種類（SingletonまたはLocator）。</param>
        public static bool RegisterInstance<T>(T instance, LocateType type = LocateType.Singleton) where T : class
        {
            return _manager.RegisterInstance(typeof(T), instance, type);
        }

        /// <summary>
        ///     指定されたインスタンスをロケーターに登録します。
        /// </summary>
        /// <typeparam name="T">登録するインスタンスの型。クラスである必要があります。</typeparam>
        /// <param name="instance">登録するインスタンス。</param>
        /// <param name="type">登録の種類（SingletonまたはLocator）。</param>
        public static bool RegisterInstance(Type type, object instance, LocateType locateType = LocateType.Singleton)
        {
            return _manager.RegisterInstance(type, instance, locateType);
        }

        /// <summary>
        ///     指定したインスタンスをロケーターから登録解除します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static bool UnregisterInstance<T>(T instance) where T : class
        {
            if (instance == null) { return false; }
            if (instance != _data.Get<T>()) { return false; }

            return _manager.UnregisterInstance(typeof(T));
        }

        /// <summary>
        ///     指定した型のインスタンスをロケーターから登録解除します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool UnregisterInstance<T>() where T : class
        {
            return _manager.UnregisterInstance(typeof(T));
        }

        /// <summary>
        ///     指定したインスタンスと同じ型の登録済みインスタンスを破棄します。
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型。</typeparam>
        /// <param name="instance">破棄の対象となるインスタンス。</param>
        public static bool DestroyInstance<T>(T instance) where T : class
        {
            if (instance == null) return false;

            DestroyInstance<T>();

            return true;
        }

        /// <summary>
        ///     指定した型のインスタンスを破棄します。
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型。</typeparam>
        public static bool DestroyInstance<T>() where T : class
        {
            Type type = typeof(T);

            if (!_data.LocateObjects.TryGetValue(type, out var md))
            {
                Debug.LogWarning($"{type.Name}は登録されていません");
                return false;
            }

            _manager.DestroyInstance(type);

#if UNITY_EDITOR
            //ログを出力
            if (EditorPrefs.GetBool(EditorSymphonyConstant.ServiceLocatorDestroyInstanceKey,
                EditorSymphonyConstant.ServiceLocatorDestroyInstanceDefault))
                Debug.Log($"{typeof(T).Name}が破棄されました");
#endif
            return true;
        }

        public static bool IsExistInstance(Type type)
        {
            return _data.IsLocate(type);
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
                SymphonyDebugLogger.AddText($"ServiceLocator\n{typeof(T).Name}の取得がリクエストされました。");
#endif
            return _data.Get<T>();
        }

        /// <summary>
        ///     指定した型のインスタンスが登録されているかどうかを確認し、登録されていればそのインスタンスを返します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGetInstance<T>(out T result) where T : class
        {
            result = _data.Get<T>();
            return result != null;
        }

        /// <summary>
        ///     指定した型のインスタンスが登録されるまで非同期で待機し、取得します。
        /// </summary>
        /// <typeparam name="T">取得したいインスタンスの型。</typeparam>
        /// <param name="grace">最大待機時間（秒）。この時間を超えるとnullを返します。</param>
        /// <param name="token">キャンセルトークン。</param>
        /// <returns>指定した型のインスタンス。見つからない場合はnull。</returns>
        public static ValueTask<T> GetInstanceAsync<T>(
            byte grace = 120,
            CancellationToken token = default) where T : class
        {
            // 既に登録されている場合は即座に返します。
            if (TryGetInstance<T>(out var instance))
            {
                return new ValueTask<T>(instance);
            }

            // 登録されるまで待機します。
            TaskCompletionSource<T> tcs = new();
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.Token.Register(() => tcs.TrySetCanceled());
            cts.CancelAfter(grace * 1000);

            // インスタンスが登録されたらタスクを完了させるアクションを登録します。
            RegisterAfterLocate<T>(t =>
            {
                tcs.TrySetResult(t);
                cts.Dispose();
            });
            return new ValueTask<T>(tcs.Task);
        }

        public static async ValueTask<(bool success, T result)> TryGetInstanceAsync<T>(
            byte grace = 120,
            CancellationToken token = default)
            where T : class
        {
            try
            {
                // 指定した型のインスタンスが登録されるまで待機し、取得します。
                T result = await GetInstanceAsync<T>(grace, token);
                return (result != null, result);
            }
            catch
            {
                // 例外をキャッチして失敗を返す
                return (false, null);
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
            if (_data.IsLocate(typeof(T)))
            {
                action?.Invoke();
                return;
            }

            // まだ登録されていなければ、待機リストに追加します。
            _data.RegisterAction<T>(action);
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
            if (_data.IsLocate(typeof(T)))
            {
                T instance = _data.Get<T>();
                action?.Invoke(instance);
                return;
            }

            _data.RegisterAction(action);
        }

        internal static void Initialize()
        {
            _data = new();
            _manager = new(_data);
        }

        private static ServiceLocateManager _manager;
        private static ServiceLocateData _data;
    }
}