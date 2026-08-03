using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;
using SymphonyFrameWork.Exceptions;

using UnityEngine;

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
            return RegisterInstance(
                type,
                instance,
                locateType,
                disposeOnFailure: false);
        }

        /// <summary>
        ///     指定されたインスタンスを登録し、型重複で失敗した候補を自動解放します。
        /// </summary>
        /// <typeparam name="T"> 登録するインスタンスの型。 </typeparam>
        /// <param name="instance"> 登録するインスタンス。 </param>
        /// <param name="type"> SingletonまたはLocatorの登録方式。 </param>
        /// <returns> 登録できた場合はtrue。 </returns>
        public static bool RegisterInstanceWithAutoDispose<T>(
            T instance,
            LocateType type = DEFAULT_LOCATE_TYPE)
            where T : class
        {
            return RegisterInstanceWithAutoDispose(
                typeof(T),
                instance,
                type);
        }

        /// <summary>
        ///     指定されたインスタンスを登録し、型重複で失敗した候補を自動解放します。
        /// </summary>
        /// <param name="type"> 登録時のキーとして使用する実行時型。 </param>
        /// <param name="instance"> 登録するインスタンス。 </param>
        /// <param name="locateType"> SingletonまたはLocatorの登録方式。 </param>
        /// <returns> 登録できた場合はtrue。 </returns>
        public static bool RegisterInstanceWithAutoDispose(
            Type type,
            object instance,
            LocateType locateType = DEFAULT_LOCATE_TYPE)
        {
            return RegisterInstance(
                type,
                instance,
                locateType,
                disposeOnFailure: true);
        }

        /// <summary>
        ///     指定したインスタンスをロケーターから登録解除します。
        /// </summary>
        /// <typeparam name="T"> 登録解除するインスタンスの型。 </typeparam>
        /// <param name="instance"> 現在の登録と同一か確認するインスタンス。 </param>
        /// <returns> 同一インスタンスの登録を解除できた場合はtrue。 </returns>
        public static bool UnregisterInstance<T>(T instance) where T : class
        {
            EnsureInitialized();
            if (instance == null) { return false; }
            if (!_query.TryGetInstance(
                typeof(T),
                out object registeredInstance)
                || !ReferenceEquals(instance, registeredInstance))
            {
                return false;
            }

            return UnregisterInstance(typeof(T));
        }

        /// <summary>
        ///     指定したタイプをロケーターから登録解除します。
        /// </summary>
        /// <param name="type"> 登録解除する実行時型。 </param>
        /// <returns> 登録を解除できた場合はtrue。 </returns>
        public static bool UnregisterInstance(Type type)
        {
            EnsureInitialized();

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            bool unregistered = _service.Unregister(type);
            if (!unregistered)
            {
                Debug.LogWarning($"{type.Name}は登録されていません。");
                return false;
            }

#if UNITY_EDITOR
            if (ServiceLocateLogOption.IsDestroyInstanceLogEnabled)
            {
                Debug.Log($"{type.Name}が登録解除されました。");
            }
#endif
            return true;
        }

        /// <summary>
        ///     指定した型のインスタンスをロケーターから登録解除します。
        /// </summary>
        /// <typeparam name="T"> 登録解除するインスタンスの型。 </typeparam>
        /// <returns> 登録を解除できた場合はtrue。 </returns>
        public static bool UnregisterInstance<T>() where T : class
        {
            EnsureInitialized();
            return UnregisterInstance(typeof(T));
        }

        /// <summary>
        ///     指定したインスタンスと同じ型の登録済みインスタンスを破棄します。
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型。</typeparam>
        /// <param name="instance">破棄の対象となるインスタンス。</param>
        public static bool DestroyInstance<T>(T instance) where T : class
        {
            EnsureInitialized();
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
            EnsureInitialized();
            Type type = typeof(T);

            if (!_query.Contains(type))
            {
                Debug.LogWarning($"{type.Name}は登録されていません");
                return false;
            }

            _service.Destroy(type);

#if UNITY_EDITOR
            // ログを出力する。
            if (ServiceLocateLogOption.IsDestroyInstanceLogEnabled)
            {
                Debug.Log($"{typeof(T).Name}が破棄されました");
            }
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
            EnsureInitialized();

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _query.Contains(type);
        }

        /// <summary> 登録中Serviceの不変なスナップショット一覧を取得する。 </summary>
        /// <returns> 型の完全名を基準にordinal昇順で並んだ登録情報一覧。 </returns>
        public static IReadOnlyList<ServiceRegistrationInfo> GetRegistrationInfos()
        {
            EnsureInitialized();
            return _query.GetInfos();
        }

        /// <summary> 指定した型の登録情報を不変なスナップショットとして取得する。 </summary>
        /// <param name="serviceType"> 検索する登録キー。 </param>
        /// <param name="registrationInfo"> 取得できた登録情報。 </param>
        /// <returns> 登録情報を取得できた場合はtrue。 </returns>
        public static bool TryGetRegistrationInfo(
            Type serviceType,
            out ServiceRegistrationInfo registrationInfo)
        {
            EnsureInitialized();

            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            return _query.TryGetInfo(serviceType, out registrationInfo);
        }

        /// <summary>
        ///     登録されたインスタンスを取得します。
        /// </summary>
        /// <typeparam name="T">取得したいインスタンスの型。</typeparam>
        /// <returns>指定した型のインスタンス。見つからない場合や破棄済みの場合はnull。</returns>
        public static T GetInstance<T>() where T : class
        {
            EnsureInitialized();
#if UNITY_EDITOR
            // ログを出力する。
            if (ServiceLocateLogOption.IsGetInstanceLogEnabled)
            {
                SymphonyDebugLogger.AddText($"ServiceLocator\n{typeof(T).Name}の取得がリクエストされました。");
            }
#endif
            T instance = _query.TryGetInstance(
                typeof(T),
                out object registeredInstance)
                ? (T)registeredInstance
                : default;
            return IsAvailableInstance(instance) ? instance : default;
        }

        /// <summary>
        ///     登録済みであることが必須のインスタンスを取得する。
        /// </summary>
        /// <typeparam name="T"> 取得したい必須サービスの型。 </typeparam>
        /// <returns> 登録済みのインスタンス。 </returns>
        /// <exception cref="ServiceNotRegisteredException"> 指定型が登録されていない場合。 </exception>
        public static T GetRequiredInstance<T>() where T : class
        {
            EnsureInitialized();

            T instance = _query.TryGetInstance(
                typeof(T),
                out object registeredInstance)
                ? (T)registeredInstance
                : default;
            if (!IsAvailableInstance(instance))
            {
                throw new ServiceNotRegisteredException(typeof(T));
            }

            return instance;
        }

        /// <summary>
        ///     指定した型のインスタンスが登録されているかどうかを確認し、登録されていればそのインスタンスを返します。
        /// </summary>
        /// <typeparam name="T"> 取得するインスタンスの型。 </typeparam>
        /// <param name="result"> 取得できた登録済みインスタンス。 </param>
        /// <returns> インスタンスを取得できた場合はtrue。 </returns>
        public static bool TryGetInstance<T>(out T result) where T : class
        {
            EnsureInitialized();
            result = _query.TryGetInstance(
                typeof(T),
                out object registeredInstance)
                ? (T)registeredInstance
                : default;
            if (IsAvailableInstance(result))
            {
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        ///     指定した型のインスタンスが登録されるまで非同期で待機し、取得します。
        /// </summary>
        /// <typeparam name="T">取得したいインスタンスの型。</typeparam>
        /// <param name="grace"> 最大待機時間（秒）。 </param>
        /// <param name="token">キャンセルトークン。</param>
        /// <returns> 指定した型のインスタンス。 </returns>
        /// <exception cref="TimeoutException"> 制限時間内にインスタンスが登録されなかった場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static async ValueTask<T> GetInstanceAsync<T>(
            byte grace = 120,
            CancellationToken token = default) where T : class
        {
            EnsureInitialized();

            // 既に登録されている場合は即座に返します。
            if (TryGetInstance<T>(out var instance))
            {
                return instance;
            }

            // 登録されるまで待機します。
            ServiceLocateService service = _service;
            TaskCompletionSource<T> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Action<T> locatedHandler = value => completionSource.TrySetResult(value);

            service.RegisterWaitingAction(locatedHandler);

            using CancellationTokenSource timeoutSource = new();
            using CancellationTokenRegistration timeoutRegistration = timeoutSource.Token.Register(() =>
            {
                completionSource.TrySetException(
                    new TimeoutException(
                        $"[{nameof(ServiceLocator)}] {typeof(T).FullName} の登録待機が{grace}秒でタイムアウトしました。"));
            });
            using CancellationTokenRegistration cancellationRegistration = token.Register(
                () => completionSource.TrySetCanceled());

            timeoutSource.CancelAfter(grace * 1000);

            try
            {
                return await completionSource.Task;
            }
            finally
            {
                service.UnregisterWaitingAction(locatedHandler);
            }
        }

        /// <summary>
        ///     制限時間内に指定型のインスタンスを取得し、成否と結果を返す。
        ///     呼び出し側からのキャンセルは失敗へ変換せず伝播する。
        /// </summary>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
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
            catch (TimeoutException)
            {
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
            EnsureInitialized();

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // 既にインスタンスが登録済みであれば、即座にアクションを実行します。
            if (_query.Contains(typeof(T)))
            {
                action?.Invoke();
                return;
            }

            // まだ登録されていなければ、待機リストに追加します。
            _service.RegisterWaitingAction<T>(action);
        }

        /// <summary>
        ///     指定した型のオブジェクトが登録された時に、そのインスタンスを引数としてアクションを実行します。
        ///     既に登録済みの場合は即座に実行されます。
        /// </summary>
        /// <typeparam name="T">待機するインスタンスの型。</typeparam>
        /// <param name="action">実行するアクション。引数として登録されたインスタンスを受け取ります。</param>
        public static void RegisterAfterLocate<T>(Action<T> action) where T : class
        {
            EnsureInitialized();

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // 既にインスタンスが登録済みであれば、そのインスタンスを引数にして即座にアクションを実行します。
            if (_query.TryGetInstance(
                typeof(T),
                out object registeredInstance))
            {
                T instance = (T)registeredInstance;
                action?.Invoke(instance);
                return;
            }

            _service.RegisterWaitingAction(action);
        }

        /// <summary> Service Locatorが初期化済みかどうか。 </summary>
        internal static bool IsInitialized =>
            _service != null
            && _registry != null
            && _query != null
            && _viewModel != null
            && _host != null;

        /// <summary> Compositionが生成した表示用ViewModel。 </summary>
        internal static ServiceLocateViewModel CurrentViewModel => _viewModel;

        /// <summary> Compositionが生成した所有先を使用してLocator状態を初期化する。 </summary>
        /// <param name="host"> Singleton Componentの所有と解放を行うHost。 </param>
        internal static void Initialize(ServiceHostComponent host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            ResetRuntimeState();
            _host = host;
            _registry = new ServiceLocateRegistry();
            _service = new ServiceLocateService(_registry, host);
            _query = new ServiceLocateQuery(_registry);
            _viewModel = new ServiceLocateViewModel(_query, _service);
        }

        /// <summary> 登録状態を消去してLocatorを未初期化状態へ戻す。 </summary>
        internal static void ResetRuntimeState()
        {
            _viewModel?.Dispose();
            _registry?.Clear();
            _host?.DisposeHost();
            _viewModel = null;
            _query = null;
            _service = null;
            _registry = null;
            _host = null;
        }

        /// <summary> Service Locatorが利用可能な状態か検証する。 </summary>
        private static void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new SymphonyNotInitializedException(typeof(ServiceLocator));
            }
        }

        /// <summary> 通常のnullと破棄済みUnity Objectを取得不能として判定する。 </summary>
        /// <param name="instance"> 判定する登録payload。 </param>
        /// <returns> 現在利用可能な参照の場合はtrue。 </returns>
        private static bool IsAvailableInstance(object instance)
        {
            if (instance == null)
            {
                return false;
            }

            return instance is not UnityEngine.Object unityObject
                || unityObject != null;
        }

        /// <summary> 登録入力を検証し、所有権方針を指定してServiceへ転送する。 </summary>
        private static bool RegisterInstance(
            Type type,
            object instance,
            LocateType locateType,
            bool disposeOnFailure)
        {
            EnsureInitialized();

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (instance != null && !type.IsInstanceOfType(instance))
            {
                throw new ArgumentException(
                    $"{type.FullName} として登録できるインスタンスを指定してください。",
                    nameof(instance));
            }

            if (locateType != LocateType.Locator
                && locateType != LocateType.Singleton)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(locateType),
                    locateType,
                    "有効な登録方式を指定してください。");
            }

            bool registered = _service.Register(
                type,
                instance,
                locateType,
                disposeOnFailure);

#if UNITY_EDITOR
            if (registered && ServiceLocateLogOption.IsSetInstanceLogEnabled)
            {
                string instanceName = instance is Component component
                    ? component.name
                    : instance.GetType().Name;
                string locateTypeName = locateType switch
                {
                    LocateType.Locator => "ロケート",
                    LocateType.Singleton => "シングルトン",
                    _ => string.Empty
                };
                Debug.Log($"{type.Name}クラスの{instanceName}が{locateTypeName}登録されました");
            }
#endif
            return registered;
        }

        private const LocateType DEFAULT_LOCATE_TYPE = LocateType.Locator;

        private static ServiceHostComponent _host;
        private static ServiceLocateRegistry _registry;
        private static ServiceLocateService _service;
        private static ServiceLocateQuery _query;
        private static ServiceLocateViewModel _viewModel;
    }
}
