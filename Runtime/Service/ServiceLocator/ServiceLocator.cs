using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;
using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.Utility;

using UnityEngine;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     アプリケーションで共有するサービスの登録と取得を管理する。
    /// </summary>
    /// <remarks>
    ///     Component、interface、通常のクラスを登録でき、Locator登録ではシーン階層を維持する。
    /// </remarks>
    public static class ServiceLocator
    {
        #region 外部向けAPI

        /// <summary>
        ///     指定されたインスタンスをロケーターに登録する。
        /// </summary>
        /// <typeparam name="T"> 登録するインスタンスの型。 </typeparam>
        /// <param name="instance"> 登録するインスタンス。 </param>
        /// <param name="type"> SingletonまたはLocatorの登録方式。 </param>
        public static bool RegisterInstance<T>(T instance, LocateTypeEnum type = DEFAULT_LOCATE_TYPE) where T : class
        {
            return RegisterInstance(typeof(T), instance, type);
        }

        /// <summary>
        ///     指定されたインスタンスをロケーターに登録する。
        /// </summary>
        /// <param name="type"> 登録時のキーとして使用する実行時型。 </param>
        /// <param name="instance"> 登録するインスタンス。 </param>
        /// <param name="locateType"> SingletonまたはLocatorの登録方式。 </param>
        public static bool RegisterInstance(Type type, object instance, LocateTypeEnum locateType = DEFAULT_LOCATE_TYPE)
        {
            return RegisterInstance(
                type,
                instance,
                locateType,
                disposeOnFailure: false);
        }

        /// <summary>
        ///     指定されたインスタンスを登録する。
        /// </summary>
        /// <typeparam name="T"> 登録するインスタンスの型。 </typeparam>
        /// <param name="instance"> 登録するインスタンス。 </param>
        /// <param name="type"> SingletonまたはLocatorの登録方式。 </param>
        /// <returns> 登録できた場合はtrue。 </returns>
        /// <remarks> 型重複で登録に失敗した場合は候補を自動解放する。 </remarks>
        public static bool RegisterInstanceWithAutoDispose<T>(
            T instance,
            LocateTypeEnum type = DEFAULT_LOCATE_TYPE)
            where T : class
        {
            return RegisterInstanceWithAutoDispose(
                typeof(T),
                instance,
                type);
        }

        /// <summary>
        ///     指定されたインスタンスを登録する。
        /// </summary>
        /// <param name="type"> 登録時のキーとして使用する実行時型。 </param>
        /// <param name="instance"> 登録するインスタンス。 </param>
        /// <param name="locateType"> SingletonまたはLocatorの登録方式。 </param>
        /// <returns> 登録できた場合はtrue。 </returns>
        /// <remarks> 型重複で登録に失敗した場合は候補を自動解放する。 </remarks>
        public static bool RegisterInstanceWithAutoDispose(
            Type type,
            object instance,
            LocateTypeEnum locateType = DEFAULT_LOCATE_TYPE)
        {
            return RegisterInstance(
                type,
                instance,
                locateType,
                disposeOnFailure: true);
        }

        /// <summary>
        ///     指定したインスタンスをロケーターから登録解除する。
        /// </summary>
        /// <typeparam name="T"> 登録解除するインスタンスの型。 </typeparam>
        /// <param name="instance"> 現在の登録と同一か確認するインスタンス。 </param>
        /// <remarks> 未初期化の場合は何も登録されていないものとしてfalseを返す。 </remarks>
        /// <returns> 同一インスタンスの登録を解除できた場合はtrue。 </returns>
        public static bool UnregisterInstance<T>(T instance) where T : class
        {
            // 未初期化または候補がnullなら、解除対象の登録は存在しない。
            if (!IsInitialized) { return false; }
            if (instance == null) { return false; }

            // 現在の登録と同一参照でない候補から、別のインスタンスを解除させない。
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
        ///     指定したタイプをロケーターから登録解除する。
        /// </summary>
        /// <param name="type"> 登録解除する実行時型。 </param>
        /// <remarks> 未初期化の場合は何も登録されていないものとしてfalseを返す。 </remarks>
        /// <returns> 登録を解除できた場合はtrue。 </returns>
        public static bool UnregisterInstance(Type type)
        {
            // null引数は状態によらず呼び出し側の誤りなので、未初期化判定より先に検証する。
            if (type == null) { throw new ArgumentNullException(nameof(type)); }

            // 終了処理でOrchestratorが解放した後の解除は失敗ではない。
            // 警告ログを出さずに返し、終了のたびにコンソールへ積まないようにする。
            if (!IsInitialized) { return false; }

            bool unregistered = _service.Unregister(type);

            // 登録が無い型の解除要求だけを警告し、呼び出し側へ失敗を返す。
            if (!unregistered)
            {
                SymphonyDebugLogger.LogDirect($"{type.Name}は登録されていません。", LogKindEnum.Warning);
                return false;
            }

#if UNITY_EDITOR
            // Editor向け解除ログが有効な場合だけ、正常な状態変更を記録する。
            if (ServiceLocateLogOption.IsDestroyInstanceLogEnabled)
            {
                SymphonyDebugLogger.LogDirect($"{type.Name}が登録解除されました。");
            }
#endif
            return true;
        }

        /// <summary>
        ///     指定した型のインスタンスをロケーターから登録解除する。
        /// </summary>
        /// <typeparam name="T"> 登録解除するインスタンスの型。 </typeparam>
        /// <remarks> 未初期化の場合は何も登録されていないものとしてfalseを返す。 </remarks>
        /// <returns> 登録を解除できた場合はtrue。 </returns>
        public static bool UnregisterInstance<T>() where T : class
        {
            return UnregisterInstance(typeof(T));
        }

        /// <summary>
        ///     指定したインスタンスと同じ型の登録済みインスタンスを破棄する。
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型。</typeparam>
        /// <param name="instance">破棄の対象となるインスタンス。</param>
        /// <remarks> 未初期化の場合は何も登録されていないものとしてfalseを返す。 </remarks>
        public static bool DestroyInstance<T>(T instance) where T : class
        {
            // 未初期化または候補がnullなら、破棄対象の登録は存在しない。
            if (!IsInitialized) { return false; }
            if (instance == null) { return false; }

            // 既存挙動を維持し、候補の同一性は確認せず型の登録を破棄する。
            DestroyInstance<T>();

            return true;
        }

        /// <summary>
        ///     指定した型のインスタンスを破棄する。
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型。</typeparam>
        /// <remarks> 未初期化の場合は何も登録されていないものとしてfalseを返す。 </remarks>
        public static bool DestroyInstance<T>() where T : class
        {
            // 解除と同じく、警告ログを出さずに返す。
            if (!IsInitialized) { return false; }

            Type type = typeof(T);

            // 未登録型は破棄できないため警告し、Hostへ処理を渡さない。
            if (!_query.Contains(type))
            {
                SymphonyDebugLogger.LogDirect($"{type.Name}は登録されていません", LogKindEnum.Warning);
                return false;
            }

            // 登録payloadを解放し、Registryから除去する。
            _service.Destroy(type);

#if UNITY_EDITOR
            // Editor向け破棄ログが有効な場合だけ、正常な状態変更を記録する。
            if (ServiceLocateLogOption.IsDestroyInstanceLogEnabled)
            {
                SymphonyDebugLogger.LogDirect($"{typeof(T).Name}が破棄されました");
            }
#endif
            return true;
        }

        /// <summary>
        ///     指定型のインスタンスが登録されているか確認する。
        /// </summary>
        public static bool IsExistInstance<T>() where T : class
        {
            return IsExistInstance(typeof(T));
        }

        /// <summary>
        ///     指定インスタンスの型が登録されているか確認する。
        /// </summary>
        public static bool IsExistInstance<T>(T instance) where T : class
        {
            return IsExistInstance(typeof(T));
        }

        /// <summary>
        ///     実行時型のインスタンスが登録されているか確認する。
        /// </summary>
        /// <remarks> 未初期化の場合は何も登録されていないものとしてfalseを返す。 </remarks>
        public static bool IsExistInstance(Type type)
        {
            // null引数は状態によらず呼び出し側の誤りなので、未初期化判定より先に検証する。
            if (type == null) { throw new ArgumentNullException(nameof(type)); }

            // 未初期化時は登録が存在しないものとして扱う。
            if (!IsInitialized) { return false; }

            return _query.Contains(type);
        }

        /// <summary>
        ///     登録中Serviceの不変なスナップショット一覧を取得する。
        /// </summary>
        /// <remarks> 未初期化の場合は何も登録されていないものとして空一覧を返す。 </remarks>
        /// <returns> 型の完全名を基準にordinal昇順で並んだ登録情報一覧。 </returns>
        public static IReadOnlyList<ServiceRegistrationInfo> GetRegistrationInfos()
        {
            // 未初期化時もQueryと同じ具象型の空一覧を返し、観測可能な型の差を作らない。
            if (!IsInitialized) { return _emptyRegistrationInfos; }

            return _query.GetInfos();
        }

        /// <summary>
        ///     指定した型の登録情報を不変なスナップショットとして取得する。
        /// </summary>
        /// <param name="serviceType"> 検索する登録キー。 </param>
        /// <param name="registrationInfo"> 取得できた登録情報。 </param>
        /// <remarks> 未初期化の場合は何も登録されていないものとしてfalseを返す。 </remarks>
        /// <returns> 登録情報を取得できた場合はtrue。 </returns>
        public static bool TryGetRegistrationInfo(
            Type serviceType,
            out ServiceRegistrationInfo registrationInfo)
        {
            // null引数は状態によらず呼び出し側の誤りなので、未初期化判定より先に検証する。
            if (serviceType == null) { throw new ArgumentNullException(nameof(serviceType)); }

            // 未初期化時は出力を既定値へ揃え、登録が無いものとして扱う。
            if (!IsInitialized)
            {
                registrationInfo = default;
                return false;
            }

            return _query.TryGetInfo(serviceType, out registrationInfo);
        }

        /// <summary>
        ///     登録されたインスタンスを取得する。
        /// </summary>
        /// <typeparam name="T">取得したいインスタンスの型。</typeparam>
        /// <returns>指定した型のインスタンス。見つからない場合や破棄済みの場合はnull。</returns>
        /// <remarks> 未初期化の場合は何も登録されていないものとしてnullを返す。 </remarks>
        public static T GetInstance<T>() where T : class
        {
            // 未初期化時は登録が存在しないものとして扱う。
            if (!IsInitialized) { return default; }
#if UNITY_EDITOR
            // Editor向け取得ログが有効な場合だけ、要求された型を記録する。
            if (ServiceLocateLogOption.IsGetInstanceLogEnabled)
            {
                SymphonyDebugLogger.AddText(
                    $"ServiceLocator\n{typeof(T).Name}の取得がリクエストされました。");
            }
#endif
            // 登録payloadを取得し、破棄済みUnity Objectは未取得として返す。
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
            // 必須取得では未初期化を未登録と区別して通知する。
            EnsureInitialized();

            // 通常のnullと破棄済みUnity Objectを、どちらも必須サービスの欠落として扱う。
            T instance = _query.TryGetInstance(
                typeof(T),
                out object registeredInstance)
                ? (T)registeredInstance
                : default;
            if (!IsAvailableInstance(instance)) { throw new ServiceNotRegisteredException(typeof(T)); }

            return instance;
        }

        /// <summary>
        ///     指定した型の登録済みインスタンスを取得する。
        /// </summary>
        /// <typeparam name="T"> 取得するインスタンスの型。 </typeparam>
        /// <param name="result"> 取得できた登録済みインスタンス。 </param>
        /// <remarks> 未初期化の場合は何も登録されていないものとしてfalseを返す。 </remarks>
        /// <returns> インスタンスを取得できた場合はtrue。 </returns>
        public static bool TryGetInstance<T>(out T result) where T : class
        {
            // 未初期化時は出力を既定値へ揃え、登録が無いものとして扱う。
            if (!IsInitialized)
            {
                result = default;
                return false;
            }

            // 登録payloadを取得し、通常のnullと破棄済みUnity Objectを除外する。
            result = _query.TryGetInstance(
                typeof(T),
                out object registeredInstance)
                ? (T)registeredInstance
                : default;
            if (IsAvailableInstance(result)) { return true; }

            // 取得不能な参照を呼び出し側へ漏らさないよう、失敗時は既定値へ戻す。
            result = default;
            return false;
        }

        /// <summary>
        ///     指定した型のインスタンスを非同期で取得する。
        /// </summary>
        /// <typeparam name="T">取得したいインスタンスの型。</typeparam>
        /// <param name="grace"> 最大待機時間（秒）。 </param>
        /// <param name="token">キャンセルトークン。</param>
        /// <returns> 指定した型のインスタンス。 </returns>
        /// <exception cref="TimeoutException"> 制限時間内にインスタンスが登録されなかった場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static Awaitable<T> GetInstanceAsync<T>(
            byte grace = 120,
            CancellationToken token = default) where T : class
        {
            // 登録待機を開始できるCompositionが構築済みであることを保証する。
            EnsureInitialized();
            return SymphonyAwaitable.FromTask(GetInstanceInternalAsync<T>(grace, token));
        }

        /// <summary>
        ///     指定した型のインスタンスを非同期で取得する。
        /// </summary>
        /// <typeparam name="T"> 取得したいインスタンスの型。 </typeparam>
        /// <param name="grace"> 最大待機時間（秒）。 </param>
        /// <param name="token"> キャンセルトークン。 </param>
        /// <returns> 制限時間内に取得できた場合はtrueとインスタンス。 </returns>
        /// <remarks> 呼び出し側からのキャンセルは失敗へ変換せず伝播する。 </remarks>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static Awaitable<(bool success, T result)> TryGetInstanceAsync<T>(
            byte grace = 120,
            CancellationToken token = default)
            where T : class
        {
            // 登録待機を開始できるCompositionが構築済みであることを保証する。
            EnsureInitialized();
            return SymphonyAwaitable.FromTask(
                TryGetInstanceInternalAsync<T>(grace, token));
        }

        /// <summary>
        ///     指定型の登録後にアクションを実行する。
        /// </summary>
        /// <remarks> 既に登録済みの場合は即座に実行する。 </remarks>
        /// <typeparam name="T">待機するインスタンスの型。</typeparam>
        /// <param name="action">実行するアクション。</param>
        public static void RegisterAfterLocate<T>(Action action) where T : class
        {
            // 登録状態と待機一覧を利用できるCompositionが構築済みであることを保証する。
            EnsureInitialized();

            // 登録時まで失敗を遅らせないよう、nullのcallbackは待機一覧へ追加しない。
            if (action == null) { throw new ArgumentNullException(nameof(action)); }

            // 既にインスタンスが登録済みであれば、即座にアクションを実行する。
            if (_query.Contains(typeof(T)))
            {
                action?.Invoke();
                return;
            }

            // まだ登録されていなければ、待機リストに追加する。
            _service.RegisterWaitingAction<T>(action);
        }

        /// <summary>
        ///     指定型の登録後にpayload付きアクションを実行する。
        /// </summary>
        /// <remarks> 既に登録済みの場合は即座に実行する。 </remarks>
        /// <typeparam name="T">待機するインスタンスの型。</typeparam>
        /// <param name="action">実行するアクション。引数として登録されたインスタンスを受け取る。</param>
        public static void RegisterAfterLocate<T>(Action<T> action) where T : class
        {
            // 登録状態と待機一覧を利用できるCompositionが構築済みであることを保証する。
            EnsureInitialized();

            // 登録時まで失敗を遅らせないよう、nullのcallbackは待機一覧へ追加しない。
            if (action == null) { throw new ArgumentNullException(nameof(action)); }

            // 既にインスタンスが登録済みであれば、そのインスタンスを引数にして即座にアクションを実行する。
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

        #endregion

        #region 内部処理

        /// <summary> Service Locatorが初期化済みかどうか。 </summary>
        internal static bool IsInitialized =>
            _service != null
            && _registry != null
            && _query != null
            && _viewModel != null
            && _host != null;

        /// <summary> Compositionが生成した表示用ViewModel。 </summary>
        internal static ServiceLocateViewModel CurrentViewModel => _viewModel;

        /// <summary>
        ///     型を指定して登録済みインスタンスを取得する。
        /// </summary>
        /// <param name="serviceType"> 取得するサービス型。 </param>
        /// <param name="instance"> 取得できた登録済みインスタンス。 </param>
        /// <returns> 取得できた場合はtrue。 </returns>
        /// <remarks>
        ///     型引数を持てないコンストラクタ注入のための内部専用の入口である。
        ///     未初期化と破棄済みUnity Objectの扱いは <see cref="GetInstance{T}" /> と同じにする。
        ///     公開APIへは広げない。
        /// </remarks>
        internal static bool TryGetInstance(Type serviceType, out object instance)
        {
            instance = null;

            // 未初期化時は登録が存在しないものとして扱う。
            if (!IsInitialized || serviceType == null) { return false; }

            // 通常のnullと破棄済みUnity Objectを、どちらも未取得として返す。
            if (!_query.TryGetInstance(serviceType, out object registeredInstance)) { return false; }
            if (!IsAvailableInstance(registeredInstance)) { return false; }

            instance = registeredInstance;
            return true;
        }

        /// <summary>
        ///     Compositionが生成した所有先を使用してLocator状態を初期化する。
        /// </summary>
        /// <param name="host"> Singleton Componentの所有と解放を行うHost。 </param>
        internal static void Initialize(ServiceHostComponent host)
        {
            // 不完全なCompositionを作らないよう、所有先の欠落を初期化前に拒否する。
            if (host == null) { throw new ArgumentNullException(nameof(host)); }

            // Domain Reloadなしでも前回状態を残さないよう、既存状態を破棄してから再構築する。
            ResetRuntimeState();
            _host = host;
            _registry = new ServiceLocateRegistry();
            _service = new ServiceLocateService(_registry, host);
            _query = new ServiceLocateQuery(_registry);
            _viewModel = new ServiceLocateViewModel(_query, _service);
        }

        /// <summary>
        ///     登録状態を消去してLocatorを未初期化状態へ戻す。
        /// </summary>
        internal static void ResetRuntimeState()
        {
            // 通知経路、登録状態、Unity上の所有先の順で外部との接続を閉じる。
            _viewModel?.Dispose();
            _registry?.Clear();
            _host?.DisposeHost();

            // Domain Reloadなしの次回初期化が古い参照を再利用しないよう、Composition参照をすべて切る。
            _viewModel = null;
            _query = null;
            _service = null;
            _registry = null;
            _host = null;
        }

        /// <summary>
        ///     Service Locatorが利用可能な状態か検証する。
        /// </summary>
        private static void EnsureInitialized()
        {
            // Compositionの一部でも欠けている場合は、部分初期化状態で処理を続けない。
            if (!IsInitialized) { throw new SymphonyNotInitializedException(typeof(ServiceLocator)); }
        }

        /// <summary>
        ///     登録payloadが利用可能か判定する。
        /// </summary>
        /// <param name="instance"> 判定する登録payload。 </param>
        /// <returns> 現在利用可能な参照の場合はtrue。 </returns>
        private static bool IsAvailableInstance(object instance)
        {
            // 通常のnullは登録payloadとして利用できない。
            if (instance == null) { return false; }

            // Unity Objectだけは独自のnull演算子を通し、破棄済み参照も取得不能とする。
            return instance is not UnityEngine.Object unityObject
                || unityObject != null;
        }

        /// <summary>
        ///     登録入力を検証してServiceへ転送する。
        /// </summary>
        private static bool RegisterInstance(
            Type type,
            object instance,
            LocateTypeEnum locateType,
            bool disposeOnFailure)
        {
            // 登録状態を保持できるCompositionが構築済みであることを保証する。
            EnsureInitialized();

            // 型の検証結果を一貫させるため、payloadより先に登録キーの欠落を拒否する。
            if (type == null) { throw new ArgumentNullException(nameof(type)); }

            // 登録キーとpayloadの型対応が壊れた状態をRegistryへ保存させない。
            if (instance != null && !type.IsInstanceOfType(instance))
            {
                throw new ArgumentException(
                    $"{type.FullName} として登録できるインスタンスを指定してください。",
                    nameof(instance));
            }

            // enumの未定義値ではHostの所有方針を決められないため、登録前に拒否する。
            if (locateType != LocateTypeEnum.Locator
                && locateType != LocateTypeEnum.Singleton)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(locateType),
                    locateType,
                    "有効な登録方式を指定してください。");
            }

            // 入力検証後に、失敗時の所有権方針を含めてServiceへ登録を委ねる。
            bool registered = _service.Register(
                type,
                instance,
                locateType,
                disposeOnFailure);

#if UNITY_EDITOR
            // Editor向け登録ログが有効な場合だけ、payload名と登録方式を記録する。
            if (registered && ServiceLocateLogOption.IsSetInstanceLogEnabled)
            {
                string instanceName = instance is Component component
                    ? component.name
                    : instance.GetType().Name;
                string locateTypeName = locateType switch
                {
                    LocateTypeEnum.Locator => "ロケート",
                    LocateTypeEnum.Singleton => "シングルトン",
                    _ => string.Empty
                };
                SymphonyDebugLogger.LogDirect($"{type.Name}クラスの{instanceName}が{locateTypeName}登録されました");
            }
#endif
            return registered;
        }

        /// <summary>
        ///     登録されるまで待機してインスタンスを取得する。
        /// </summary>
        /// <typeparam name="T"> 取得したいインスタンスの型。 </typeparam>
        /// <param name="grace"> 最大待機時間（秒）。 </param>
        /// <param name="token"> キャンセルトークン。 </param>
        /// <returns> 指定した型のインスタンス。 </returns>
        /// <remarks>
        ///     待機とキャンセルはTaskで管理し、公開面だけAwaitableへ変換する。
        /// </remarks>
        private static async Task<T> GetInstanceInternalAsync<T>(
            byte grace,
            CancellationToken token) where T : class
        {
            // 既に登録されている場合は、待機callbackを作らず即座に返す。
            if (TryGetInstance<T>(out T instance)) { return instance; }

            // ResetRuntimeStateでstatic参照が変わっても、同じServiceから待機解除できるよう固定する。
            ServiceLocateService service = _service;
            TaskCompletionSource<T> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Action<T> locatedHandler = value => completionSource.TrySetResult(value);

            // 登録通知を受け取る経路を、タイムアウトとキャンセルの設定前に確保する。
            service.RegisterWaitingAction(locatedHandler);

            // タイムアウトと呼び出し側キャンセルを、同じ完了ソースへ競合可能な形で接続する。
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

            // 完了理由にかかわらず、残った待機callbackを必ず解除する。
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
        ///     制限時間内にインスタンスを取得する。
        /// </summary>
        /// <typeparam name="T"> 取得したいインスタンスの型。 </typeparam>
        /// <param name="grace"> 最大待機時間（秒）。 </param>
        /// <param name="token"> キャンセルトークン。 </param>
        /// <returns> 取得の成否と結果。 </returns>
        /// <remarks> 期限超過だけを失敗へ変換し、呼び出し側のキャンセルは伝播する。 </remarks>
        private static async Task<(bool success, T result)> TryGetInstanceInternalAsync<T>(
            byte grace,
            CancellationToken token)
            where T : class
        {
            // 登録待機が完了した場合は、取得結果と参照の有無を返す。
            try
            {
                T result = await GetInstanceInternalAsync<T>(grace, token);
                return (result != null, result);
            }
            catch (TimeoutException)
            {
                // 公開Try APIでは期限超過だけを例外ではなく取得失敗へ変換する。
                return (false, null);
            }
        }

        private const LocateTypeEnum DEFAULT_LOCATE_TYPE = LocateTypeEnum.Locator;

        // 初期化済みのGetRegistrationInfosはServiceLocateQueryがArray.AsReadOnlyで返すため、
        // 未初期化の空一覧も同じ具象型に揃える。状態によって戻り値の型が変わると、
        // Countプロパティの有無のように利用側から観測できる差になる。
        private static readonly IReadOnlyList<ServiceRegistrationInfo> _emptyRegistrationInfos =
            Array.AsReadOnly(Array.Empty<ServiceRegistrationInfo>());

        private static ServiceHostComponent _host;
        private static ServiceLocateRegistry _registry;
        private static ServiceLocateService _service;
        private static ServiceLocateQuery _query;
        private static ServiceLocateViewModel _viewModel;

        #endregion
    }
}
