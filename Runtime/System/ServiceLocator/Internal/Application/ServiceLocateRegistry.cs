using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary> 型をキーにService Registration Entityと登録待機callbackを所有する。 </summary>
    internal sealed class ServiceLocateRegistry
    {
        /// <summary> Queryが読み取る登録Entity一覧。 </summary>
        internal IReadOnlyDictionary<Type, ServiceRegistrationEntity> Entities =>
            _entities;

        /// <summary> 型とpayloadを重複なしで登録する。 </summary>
        /// <param name="serviceType"> 登録キーとして使用する型。 </param>
        /// <param name="instance"> 登録するpayload。 </param>
        /// <param name="locateType"> 登録方式。 </param>
        /// <param name="entity"> 登録できたEntity。 </param>
        /// <returns> 登録できた場合はtrue。 </returns>
        internal bool TryRegister(
            Type serviceType,
            object instance,
            LocateTypeEnum locateType,
            out ServiceRegistrationEntity entity)
        {
            if (_entities.ContainsKey(serviceType))
            {
                entity = null;
                return false;
            }

            entity = new ServiceRegistrationEntity(
                serviceType,
                instance,
                locateType);
            _entities.Add(serviceType, entity);
            return true;
        }

        /// <summary> 指定型のEntityを取得する。 </summary>
        /// <param name="serviceType"> 検索する登録キー。 </param>
        /// <param name="entity"> 取得できたEntity。 </param>
        /// <returns> 登録中のEntityを取得できた場合はtrue。 </returns>
        internal bool TryGet(
            Type serviceType,
            out ServiceRegistrationEntity entity)
        {
            return _entities.TryGetValue(serviceType, out entity)
                && entity.IsRegistered;
        }

        /// <summary> 指定型が登録済みか判定する。 </summary>
        /// <param name="serviceType"> 検索する登録キー。 </param>
        /// <returns> 登録済みの場合はtrue。 </returns>
        internal bool Contains(Type serviceType) =>
            TryGet(serviceType, out _);

        /// <summary> 指定型のEntityを登録解除してRegistryから除去する。 </summary>
        /// <param name="serviceType"> 除去する登録キー。 </param>
        /// <param name="entity"> 除去できたEntity。 </param>
        /// <returns> 除去できた場合はtrue。 </returns>
        internal bool TryRemove(
            Type serviceType,
            out ServiceRegistrationEntity entity)
        {
            if (!_entities.Remove(serviceType, out entity))
            {
                return false;
            }

            entity.Unregister();
            return true;
        }

        /// <summary> 指定型の登録後に引数なしで実行するcallbackを追加する。 </summary>
        /// <typeparam name="T"> 登録を待つサービス型。 </typeparam>
        /// <param name="action"> 登録成功時に実行するcallback。 </param>
        internal void RegisterWaitingAction<T>(Action action)
        {
            Type serviceType = typeof(T);
            if (!_waitingActions.TryAdd(serviceType, action))
            {
                _waitingActions[serviceType] += action;
            }
        }

        /// <summary> 指定した引数なし登録待機callbackを解除する。 </summary>
        /// <typeparam name="T"> 登録を待っていたサービス型。 </typeparam>
        /// <param name="action"> 解除するcallback。 </param>
        internal void UnregisterWaitingAction<T>(Action action)
        {
            Type serviceType = typeof(T);
            if (!_waitingActions.TryGetValue(
                serviceType,
                out Action existing))
            {
                return;
            }

            Action remaining = existing - action;
            if (remaining == null)
            {
                _waitingActions.Remove(serviceType);
                return;
            }

            _waitingActions[serviceType] = remaining;
        }

        /// <summary> 指定型のpayloadを受け取る登録待機callbackを追加する。 </summary>
        /// <typeparam name="T"> 登録を待つサービス型。 </typeparam>
        /// <param name="action"> 登録payloadを受け取るcallback。 </param>
        internal void RegisterWaitingAction<T>(Action<T> action)
        {
            Type serviceType = typeof(T);
            if (_waitingActionsWithInstance.TryGetValue(
                serviceType,
                out Delegate existing))
            {
                _waitingActionsWithInstance[serviceType] =
                    Delegate.Combine(existing, action);
                return;
            }

            _waitingActionsWithInstance.Add(serviceType, action);
        }

        /// <summary> 指定したpayload付き登録待機callbackを解除する。 </summary>
        /// <typeparam name="T"> 登録を待っていたサービス型。 </typeparam>
        /// <param name="action"> 解除するcallback。 </param>
        internal void UnregisterWaitingAction<T>(Action<T> action)
        {
            Type serviceType = typeof(T);
            if (!_waitingActionsWithInstance.TryGetValue(
                serviceType,
                out Delegate existing))
            {
                return;
            }

            Delegate remaining = Delegate.Remove(existing, action);
            if (remaining == null)
            {
                _waitingActionsWithInstance.Remove(serviceType);
                return;
            }

            _waitingActionsWithInstance[serviceType] = remaining;
        }

        /// <summary> 指定型の登録待機callbackを一覧から除去して1回だけ実行する。 </summary>
        /// <param name="serviceType"> 登録されたサービス型。 </param>
        /// <param name="instance"> 登録されたpayload。 </param>
        internal void InvokeWaitingActions(Type serviceType, object instance)
        {
            _waitingActions.Remove(serviceType, out Action waitingAction);
            _waitingActionsWithInstance.Remove(
                serviceType,
                out Delegate waitingActionWithInstance);

            try
            {
                waitingAction?.Invoke();
            }
            finally
            {
                waitingActionWithInstance?.DynamicInvoke(instance);
            }
        }

        /// <summary> 登録Entityと未実行の待機callbackをすべて消去する。 </summary>
        internal void Clear()
        {
            foreach (ServiceRegistrationEntity entity in _entities.Values)
            {
                entity.Unregister();
            }

            _entities.Clear();
            _waitingActions.Clear();
            _waitingActionsWithInstance.Clear();
        }

        private readonly Dictionary<Type, ServiceRegistrationEntity> _entities = new();
        private readonly Dictionary<Type, Action> _waitingActions = new();
        private readonly Dictionary<Type, Delegate> _waitingActionsWithInstance = new();
    }
}
