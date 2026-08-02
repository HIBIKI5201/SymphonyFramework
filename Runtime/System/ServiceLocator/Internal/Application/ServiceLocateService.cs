using System;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary> Service Locatorの登録、解除、破棄の処理順を管理する。 </summary>
    internal sealed class ServiceLocateService
    {
        /// <summary> RegistryとUnity所有境界を注入してServiceを生成する。 </summary>
        /// <param name="registry"> 登録状態と待機callbackの所有者。 </param>
        /// <param name="host"> Unity上の所有処理を行う境界。 </param>
        internal ServiceLocateService(
            ServiceLocateRegistry registry,
            IServiceHost host)
        {
            _registry = registry
                ?? throw new ArgumentNullException(nameof(registry));
            _host = host
                ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary> 登録状態が確定したときに通知する。 </summary>
        internal event Action OnStateChanged;

        /// <summary> 指定payloadを登録する。 </summary>
        /// <param name="serviceType"> 登録キーとして使用する型。 </param>
        /// <param name="instance"> 登録するpayload。 </param>
        /// <param name="locateType"> 登録方式。 </param>
        /// <param name="disposeOnFailure"> 登録失敗時に候補を解放する場合はtrue。 </param>
        /// <returns> 登録できた場合はtrue。 </returns>
        internal bool Register(
            Type serviceType,
            object instance,
            LocateType locateType,
            bool disposeOnFailure)
        {
            if (instance == null)
            {
                return false;
            }

            if (!_registry.TryRegister(
                serviceType,
                instance,
                locateType,
                out _))
            {
                if (disposeOnFailure)
                {
                    _host.DisposeInstance(instance);
                }

                return false;
            }

            try
            {
                if (locateType == LocateType.Singleton)
                {
                    _host.Attach(instance);
                }
            }
            catch
            {
                _registry.TryRemove(serviceType, out _);
                if (disposeOnFailure)
                {
                    _host.DisposeInstance(instance);
                }

                throw;
            }

            OnStateChanged?.Invoke();
            _registry.InvokeWaitingActions(serviceType, instance);
            return true;
        }

        /// <summary> 指定型の登録を解除する。 </summary>
        /// <param name="serviceType"> 登録解除する型。 </param>
        /// <returns> 登録解除できた場合はtrue。 </returns>
        internal bool Unregister(Type serviceType)
        {
            if (!_registry.TryGet(
                serviceType,
                out ServiceRegistrationEntity entity))
            {
                return false;
            }

            _host.Detach(entity.Instance);
            _registry.TryRemove(serviceType, out _);
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary> 指定型のpayloadを解放して登録を除去する。 </summary>
        /// <param name="serviceType"> 破棄する登録型。 </param>
        /// <returns> 登録を除去できた場合はtrue。 </returns>
        internal bool Destroy(Type serviceType)
        {
            if (!_registry.TryGet(
                serviceType,
                out ServiceRegistrationEntity entity))
            {
                return false;
            }

            _host.DisposeInstance(entity.Instance);
            _host.Detach(entity.Instance);
            _registry.TryRemove(serviceType, out _);
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary> 引数なしの登録待機callbackを追加する。 </summary>
        /// <typeparam name="T"> 登録を待つサービス型。 </typeparam>
        /// <param name="action"> 登録後に実行するcallback。 </param>
        internal void RegisterWaitingAction<T>(Action action) =>
            _registry.RegisterWaitingAction<T>(action);

        /// <summary> payload付き登録待機callbackを追加する。 </summary>
        /// <typeparam name="T"> 登録を待つサービス型。 </typeparam>
        /// <param name="action"> 登録後に実行するcallback。 </param>
        internal void RegisterWaitingAction<T>(Action<T> action) =>
            _registry.RegisterWaitingAction(action);

        /// <summary> payload付き登録待機callbackを解除する。 </summary>
        /// <typeparam name="T"> 登録を待っていたサービス型。 </typeparam>
        /// <param name="action"> 解除するcallback。 </param>
        internal void UnregisterWaitingAction<T>(Action<T> action) =>
            _registry.UnregisterWaitingAction(action);

        private readonly ServiceLocateRegistry _registry;
        private readonly IServiceHost _host;
    }
}
