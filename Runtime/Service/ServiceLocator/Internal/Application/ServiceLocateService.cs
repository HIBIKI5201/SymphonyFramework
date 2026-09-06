using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     Service Locatorの登録、解除、破棄の処理順を管理する。
    /// </summary>
    internal sealed class ServiceLocateService
    {
        #region 外部向けAPI

        /// <summary>
        ///     RegistryとUnity所有境界を注入してServiceを生成する。
        /// </summary>
        /// <param name="registry"> 登録状態と待機callbackの所有者。 </param>
        /// <param name="host"> Unity上の所有処理を行う境界。 </param>
        internal ServiceLocateService(
            ServiceLocateRegistry registry,
            IServiceHost host)
        {
            // 登録状態とUnity所有処理を同じServiceの生存期間へ固定する。
            _registry = registry
                ?? throw new ArgumentNullException(nameof(registry));
            _host = host
                ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary> 登録状態が確定したときに通知する。 </summary>
        internal event Action OnStateChanged;

        /// <summary>
        ///     指定payloadを登録する。
        /// </summary>
        /// <param name="serviceType"> 登録キーとして使用する型。 </param>
        /// <param name="instance"> 登録するpayload。 </param>
        /// <param name="locateType"> 登録方式。 </param>
        /// <param name="disposeOnFailure"> 登録失敗時に候補を解放する場合はtrue。 </param>
        /// <returns> 登録できた場合はtrue。 </returns>
        internal bool Register(
            Type serviceType,
            object instance,
            LocateTypeEnum locateType,
            bool disposeOnFailure)
        {
            // nullの候補は所有も解放もできないため、登録処理へ渡さない。
            if (instance == null) { return false; }

            // 型が既に登録済みなら、指定された所有権方針に従って候補を解放する。
            if (!_registry.TryRegister(
                serviceType,
                instance,
                locateType,
                out _))
            {
                if (disposeOnFailure) { _host.DisposeInstance(instance); }

                return false;
            }

            // SingletonだけをHostの所有階層へ接続し、Locatorは元の階層を維持する。
            try
            {
                if (locateType == LocateTypeEnum.Singleton) { _host.Attach(instance); }
            }
            catch
            {
                // 接続に失敗した登録を残さず、候補の所有権も指定された方針で処理する。
                _registry.TryRemove(serviceType, out _);
                if (disposeOnFailure) { _host.DisposeInstance(instance); }

                throw;
            }

            // 登録状態を表示側へ通知してから、今回の登録を待つcallbackを解放する。
            OnStateChanged?.Invoke();
            _registry.InvokeWaitingActions(serviceType, instance);
            return true;
        }

        /// <summary>
        ///     指定型の登録を解除する。
        /// </summary>
        /// <param name="serviceType"> 登録解除する型。 </param>
        /// <returns> 登録解除できた場合はtrue。 </returns>
        internal bool Unregister(Type serviceType)
        {
            // 登録中のEntityが無い場合は、所有階層と状態を変更しない。
            if (!_registry.TryGet(serviceType, out ServiceRegistrationEntity entity)) { return false; }

            // payloadを破棄せず所有階層から外し、登録状態だけを除去する。
            _host.Detach(entity.Instance);
            _registry.TryRemove(serviceType, out _);
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        ///     指定型のpayloadを解放して登録を除去する。
        /// </summary>
        /// <param name="serviceType"> 破棄する登録型。 </param>
        /// <returns> 登録を除去できた場合はtrue。 </returns>
        internal bool Destroy(Type serviceType)
        {
            // 登録中のEntityが無い場合は、所有物と状態を変更しない。
            if (!_registry.TryGet(serviceType, out ServiceRegistrationEntity entity)) { return false; }

            // 所有階層から外してpayloadを解放した後、Registryと表示状態を更新する。
            _host.Detach(entity.Instance);
            _host.DisposeInstance(entity.Instance);
            _registry.TryRemove(serviceType, out _);
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        ///     引数なしの登録待機callbackを追加する。
        /// </summary>
        /// <typeparam name="T"> 登録を待つサービス型。 </typeparam>
        /// <param name="action"> 登録後に実行するcallback。 </param>
        internal void RegisterWaitingAction<T>(Action action) =>
            _registry.RegisterWaitingAction<T>(action);

        /// <summary>
        ///     引数なしの登録待機callbackを解除する。
        /// </summary>
        /// <typeparam name="T"> 登録を待っていたサービス型。 </typeparam>
        /// <param name="action"> 解除するcallback。 </param>
        internal void UnregisterWaitingAction<T>(Action action) =>
            _registry.UnregisterWaitingAction<T>(action);

        /// <summary>
        ///     payload付き登録待機callbackを追加する。
        /// </summary>
        /// <typeparam name="T"> 登録を待つサービス型。 </typeparam>
        /// <param name="action"> 登録後に実行するcallback。 </param>
        internal void RegisterWaitingAction<T>(Action<T> action) =>
            _registry.RegisterWaitingAction(action);

        /// <summary>
        ///     payload付き登録待機callbackを解除する。
        /// </summary>
        /// <typeparam name="T"> 登録を待っていたサービス型。 </typeparam>
        /// <param name="action"> 解除するcallback。 </param>
        internal void UnregisterWaitingAction<T>(Action<T> action) =>
            _registry.UnregisterWaitingAction(action);

        /// <summary>
        ///     フレームワークの同期フェーズでの登録を保留する。
        /// </summary>
        /// <param name="serviceType"> 登録キーとして使用する型。 </param>
        /// <param name="instance"> 登録するpayload。 </param>
        /// <param name="locateType"> 登録方式。 </param>
        /// <remarks> 実際の登録は<see cref="FlushPendingRegistrations"/>が行う。 </remarks>
        internal void EnqueuePendingRegistration(
            Type serviceType,
            object instance,
            LocateTypeEnum locateType)
        {
            _pendingRegistrations.Add((serviceType, instance, locateType));
        }

        /// <summary>
        ///     未反映の保留登録を取り消す。
        /// </summary>
        /// <param name="serviceType"> 取り消す登録キー。 </param>
        /// <param name="instance"> 取り消す候補と同一か確認するインスタンス。 </param>
        /// <returns> 取り消せた場合はtrue。既に反映済みまたは該当が無い場合はfalse。 </returns>
        internal bool CancelPendingRegistration(Type serviceType, object instance)
        {
            for (int i = _pendingRegistrations.Count - 1; i >= 0; i--)
            {
                (Type type, object candidate, LocateTypeEnum _) = _pendingRegistrations[i];
                if (type != serviceType || !ReferenceEquals(candidate, instance)) { continue; }

                _pendingRegistrations.RemoveAt(i);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     保留中の登録をすべて反映する。
        /// </summary>
        /// <remarks>
        ///     <see cref="SymphonyFrameWork.Orchestrator.SymphonyOrchestrator"/>がOnEnableとStartの間の同期フェーズから呼ぶ。
        ///     反映中に追加された保留は同じ回では処理せず、次回のFlushへ回す。
        /// </remarks>
        internal void FlushPendingRegistrations()
        {
            if (_pendingRegistrations.Count == 0) { return; }

            (Type type, object instance, LocateTypeEnum locateType)[] pending = _pendingRegistrations.ToArray();
            _pendingRegistrations.Clear();

            foreach ((Type type, object instance, LocateTypeEnum locateType) in pending)
            {
                // Flush前にUnityオブジェクトとして破棄済みになった候補は登録しない。
                if (instance is UnityEngine.Object unityObject && unityObject == null) { continue; }

                Register(type, instance, locateType, disposeOnFailure: false);
            }
        }

        #endregion

        #region 内部処理

        private readonly ServiceLocateRegistry _registry;
        private readonly IServiceHost _host;
        private readonly List<(Type type, object instance, LocateTypeEnum locateType)> _pendingRegistrations = new();

        #endregion
    }
}
