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
        /// <typeparam name="T"> 登録するインスタンスの型。 </typeparam>
        /// <param name="instance"> 登録するインスタンス。 </param>
        /// <param name="type"> SingletonまたはLocatorの登録方式。 </param>
        public static bool RegisterInstance<T>(T instance, LocateType type = DEFAULT_LOCATE_TYPE) where T : class
        {
            return RegisterInstance(typeof(T), instance, type);
        }

        /// <summary>
        ///     指定されたインスタンスをロケーターに登録します。
        /// </summary>
        /// <param name="type"> 登録時のキーとして使用する実行時型。 </param>
        /// <param name="instance"> 登録するインスタンス。 </param>
        /// <param name="locateType"> SingletonまたはLocatorの登録方式。 </param>
        public static bool RegisterInstance(Type type, object instance, LocateType locateType = DEFAULT_LOCATE_TYPE)
        {
            return _manager.RegisterInstance(type, instance, locateType);
        }

        /// <summary>
        ///     指定したインスタンスをロケーターから登録解除します。
        /// </summary>
        /// <typeparam name="T"> 登録解除するインスタンスの型。 </typeparam>
        /// <param name="instance"> 現在の登録と同一か確認するインスタンス。 </param>
        /// <returns> 同一インスタンスの登録を解除できた場合はtrue。 </returns>
        public static bool UnregisterInstance<T>(T instance) where T : class
        {
            if (instance == null) { return false; }
            if (instance != _data.Get<T>()) { return false; }

            return UnregisterInstance(typeof(T));
        }

        /// <summary>
        ///     指定したタイプをロケーターから登録解除します。
        /// </summary>
        /// <param name="type"> 登録解除する実行時型。 </param>
        /// <returns> 登録を解除できた場合はtrue。 </returns>
        public static bool UnregisterInstance(Type type)
        {
            return _manager.UnregisterInstance(type);
        }

        /// <summary>
        ///     指定した型のインスタンスをロケーターから登録解除します。
        /// </summary>
        /// <typeparam name="T"> 登録解除するインスタンスの型。 </typeparam>
        /// <returns> 登録を解除できた場合はtrue。 </returns>
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

            if (!_data.IsLocate(type))
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

        /// <summary> 指定型のインスタンスが登録されているか確認する。 </summary>
        public static bool IsExistInstance<T>() where T : class
        {
            return IsExistInstance(typeof(T));
        }

        /// <summary> 指定インスタンスの型が登録されているか確認する。 </summary>
        public static bool IsExistInstance<T>(T instance) where T : class
        {
            return IsExistInstance(typeof(T));
        }

        /// <summary> 実行時型のインスタンスが登録されているか確認する。 </summary>
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
        /// <typeparam name="T"> 取得するインスタンスの型。 </typeparam>
        /// <param name="result"> 取得できた登録済みインスタンス。 </param>
        /// <returns> インスタンスを取得できた場合はtrue。 </returns>
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

        /// <summary> 制限時間内に指定型のインスタンスを取得し、成否と結果を返す。 </summary>
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

        /// <summary> Locator状態を初期化し、システム破棄時のリセットを登録する。 </summary>
        internal static void Initialize(CancellationToken destroyCancellationToken)
        {
            _destroyRegistration.Dispose();
            ResetRuntimeState();
            _data = new();
            _manager = new(_data);
            _destroyRegistration = destroyCancellationToken.Register(ResetRuntimeState);
        }

        /// <summary> 登録状態を消去してLocatorを未初期化状態へ戻す。 </summary>
        private static void ResetRuntimeState()
        {
            _data?.Clear();
            _manager = null;
            _data = null;
        }

        private const LocateType DEFAULT_LOCATE_TYPE = LocateType.Locator;

        private static ServiceLocateManager _manager;
        private static ServiceLocateData _data;
        private static CancellationTokenRegistration _destroyRegistration;
    }
}
