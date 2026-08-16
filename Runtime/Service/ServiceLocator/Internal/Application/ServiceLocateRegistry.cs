using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     型をキーにService Registration Entityと登録待機callbackを所有する。
    /// </summary>
    internal sealed class ServiceLocateRegistry
    {
        #region 外部向けAPI

        /// <summary> Queryが読み取る登録Entity一覧。 </summary>
        internal IReadOnlyDictionary<Type, ServiceRegistrationEntity> Entities =>
            _entities;

        /// <summary>
        ///     型とpayloadを重複なしで登録する。
        /// </summary>
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
            // 同じ型の既存登録を上書きすると所有権が曖昧になるため、新しい候補を拒否する。
            if (_entities.ContainsKey(serviceType))
            {
                entity = null;
                return false;
            }

            // Entityの生成後に一覧へ追加し、成功時だけ呼び出し側へ同じEntityを返す。
            entity = new ServiceRegistrationEntity(
                serviceType,
                instance,
                locateType);
            _entities.Add(serviceType, entity);
            return true;
        }

        /// <summary>
        ///     指定型のEntityを取得する。
        /// </summary>
        /// <param name="serviceType"> 検索する登録キー。 </param>
        /// <param name="entity"> 取得できたEntity。 </param>
        /// <returns> 登録中のEntityを取得できた場合はtrue。 </returns>
        internal bool TryGet(
            Type serviceType,
            out ServiceRegistrationEntity entity)
        {
            // 一覧に残っていても解除済みのEntityは登録中として公開しない。
            return _entities.TryGetValue(serviceType, out entity)
                && entity.IsRegistered;
        }

        /// <summary>
        ///     指定型が登録済みか判定する。
        /// </summary>
        /// <param name="serviceType"> 検索する登録キー。 </param>
        /// <returns> 登録済みの場合はtrue。 </returns>
        internal bool Contains(Type serviceType) =>
            TryGet(serviceType, out _);

        /// <summary>
        ///     指定型のEntityを登録解除してRegistryから除去する。
        /// </summary>
        /// <param name="serviceType"> 除去する登録キー。 </param>
        /// <param name="entity"> 除去できたEntity。 </param>
        /// <returns> 除去できた場合はtrue。 </returns>
        internal bool TryRemove(
            Type serviceType,
            out ServiceRegistrationEntity entity)
        {
            // 登録が無い型ではEntityの状態を変更せず、除去失敗を返す。
            if (!_entities.Remove(serviceType, out entity)) { return false; }

            // 一覧から除外した後にEntityも解除済みへ遷移させる。
            entity.Unregister();
            return true;
        }

        /// <summary>
        ///     指定型の登録後に実行するcallbackを追加する。
        /// </summary>
        /// <typeparam name="T"> 登録を待つサービス型。 </typeparam>
        /// <param name="action"> 登録成功時に実行するcallback。 </param>
        internal void RegisterWaitingAction<T>(Action action)
        {
            Type serviceType = typeof(T);

            // 最初のcallbackは登録し、既存の待機があれば呼び出し順を保って連結する。
            if (!_waitingActions.TryAdd(serviceType, action)) { _waitingActions[serviceType] += action; }
        }

        /// <summary>
        ///     指定した引数なし登録待機callbackを解除する。
        /// </summary>
        /// <typeparam name="T"> 登録を待っていたサービス型。 </typeparam>
        /// <param name="action"> 解除するcallback。 </param>
        internal void UnregisterWaitingAction<T>(Action action)
        {
            Type serviceType = typeof(T);

            // 対象型に待機が無い場合は、解除済みとして何もしない。
            if (!_waitingActions.TryGetValue(serviceType, out Action existing)) { return; }

            Action remaining = existing - action;

            // 最後のcallbackを外した場合だけ、空のdelegateを残さずキーも除去する。
            if (remaining == null)
            {
                _waitingActions.Remove(serviceType);
                return;
            }

            _waitingActions[serviceType] = remaining;
        }

        /// <summary>
        ///     指定型のpayloadを受け取る登録待機callbackを追加する。
        /// </summary>
        /// <typeparam name="T"> 登録を待つサービス型。 </typeparam>
        /// <param name="action"> 登録payloadを受け取るcallback。 </param>
        internal void RegisterWaitingAction<T>(Action<T> action)
        {
            Type serviceType = typeof(T);

            // 既存の待機があれば型を維持したままdelegateへ連結する。
            if (_waitingActionsWithInstance.TryGetValue(
                serviceType,
                out Delegate existing))
            {
                _waitingActionsWithInstance[serviceType] =
                    Delegate.Combine(existing, action);
                return;
            }

            // 対象型で最初のcallbackだけは新しいキーとして登録する。
            _waitingActionsWithInstance.Add(serviceType, action);
        }

        /// <summary>
        ///     指定したpayload付き登録待機callbackを解除する。
        /// </summary>
        /// <typeparam name="T"> 登録を待っていたサービス型。 </typeparam>
        /// <param name="action"> 解除するcallback。 </param>
        internal void UnregisterWaitingAction<T>(Action<T> action)
        {
            Type serviceType = typeof(T);

            // 対象型に待機が無い場合は、解除済みとして何もしない。
            if (!_waitingActionsWithInstance.TryGetValue(serviceType, out Delegate existing)) { return; }

            Delegate remaining = Delegate.Remove(existing, action);

            // 最後のcallbackを外した場合だけ、空のdelegateを残さずキーも除去する。
            if (remaining == null)
            {
                _waitingActionsWithInstance.Remove(serviceType);
                return;
            }

            _waitingActionsWithInstance[serviceType] = remaining;
        }

        /// <summary>
        ///     指定型の登録待機callbackを1回だけ実行する。
        /// </summary>
        /// <param name="serviceType"> 登録されたサービス型。 </param>
        /// <param name="instance"> 登録されたpayload。 </param>
        internal void InvokeWaitingActions(Type serviceType, object instance)
        {
            // 再入や次回登録で同じcallbackを再実行しないよう、呼び出す前に一覧から除去する。
            _waitingActions.Remove(serviceType, out Action waitingAction);
            _waitingActionsWithInstance.Remove(
                serviceType,
                out Delegate waitingActionWithInstance);

            // 引数なしcallbackが失敗しても、payload付きcallbackへ登録結果を通知する。
            try
            {
                waitingAction?.Invoke();
            }
            finally
            {
                waitingActionWithInstance?.DynamicInvoke(instance);
            }
        }

        /// <summary>
        ///     登録Entityと未実行の待機callbackをすべて消去する。
        /// </summary>
        internal void Clear()
        {
            // 外部に残ったEntity参照からも解除済みと分かるよう、一覧を空にする前に状態を遷移させる。
            foreach (ServiceRegistrationEntity entity in _entities.Values) { entity.Unregister(); }

            // ランタイム状態を次回初期化へ持ち越さないよう、登録と待機をまとめて消去する。
            _entities.Clear();
            _waitingActions.Clear();
            _waitingActionsWithInstance.Clear();
        }

        #endregion

        #region 内部処理

        private readonly Dictionary<Type, ServiceRegistrationEntity> _entities = new();
        private readonly Dictionary<Type, Action> _waitingActions = new();
        private readonly Dictionary<Type, Delegate> _waitingActionsWithInstance = new();

        #endregion
    }
}
