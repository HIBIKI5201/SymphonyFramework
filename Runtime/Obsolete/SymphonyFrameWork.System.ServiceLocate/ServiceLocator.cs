using System.Collections.Generic;
using System.Threading;

using SymphonyFramework.Services;

using UnityEngine;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     6.x の <c>ServiceLocator</c> を <see cref="Partitura" /> へ転送する。
    /// </summary>
    /// <remarks>
    ///     7.0.0 の移行期間だけ残す。8.0.0 で削除する。
    ///     名前空間 <c>SymphonyFrameWork.System</c> が <c>System</c> を隠すため、BCL の型は <c>global::System</c> で書く。
    /// </remarks>
    [global::System.Obsolete("ServiceLocator は Partitura（名前空間 SymphonyFramework.Services）へ改名しました。Partitura を使用してください。8.0.0 で削除します。")]
    public static class ServiceLocator
    {
        #region 外部向けAPI

        /// <summary> 旧 <c>RegisterInstance</c>。<see cref="Partitura.Register{T}" /> へ転送する。 </summary>
        public static bool RegisterInstance<T>(T instance, LocateTypeEnum type = LocateTypeEnum.Singleton) where T : class =>
            Partitura.Register(instance, ToLifetime(type));

        /// <summary> 旧 <c>RegisterInstance</c>。<see cref="Partitura.Register(global::System.Type, object, ServiceLifetimeEnum)" /> へ転送する。 </summary>
        public static bool RegisterInstance(
            global::System.Type type,
            object instance,
            LocateTypeEnum locateType = LocateTypeEnum.Singleton) =>
            Partitura.Register(type, instance, ToLifetime(locateType));

        /// <summary> 旧 <c>RegisterInstanceWithAutoDispose</c>。<see cref="Partitura.RegisterWithAutoDispose{T}" /> へ転送する。 </summary>
        public static bool RegisterInstanceWithAutoDispose<T>(T instance, LocateTypeEnum type = LocateTypeEnum.Singleton)
            where T : class =>
            Partitura.RegisterWithAutoDispose(instance, ToLifetime(type));

        /// <summary> 旧 <c>RegisterInstanceWithAutoDispose</c>。Partitura へ転送する。 </summary>
        public static bool RegisterInstanceWithAutoDispose(
            global::System.Type type,
            object instance,
            LocateTypeEnum locateType = LocateTypeEnum.Singleton) =>
            Partitura.RegisterWithAutoDispose(type, instance, ToLifetime(locateType));

        /// <summary> 旧 <c>UnregisterInstance</c>。<see cref="Partitura.Unregister{T}(T)" /> へ転送する。 </summary>
        public static bool UnregisterInstance<T>(T instance) where T : class => Partitura.Unregister(instance);

        /// <summary> 旧 <c>UnregisterInstance</c>。<see cref="Partitura.Unregister(global::System.Type)" /> へ転送する。 </summary>
        public static bool UnregisterInstance(global::System.Type type) => Partitura.Unregister(type);

        /// <summary> 旧 <c>UnregisterInstance</c>。<see cref="Partitura.Unregister{T}()" /> へ転送する。 </summary>
        public static bool UnregisterInstance<T>() where T : class => Partitura.Unregister<T>();

        /// <summary> 旧 <c>DestroyInstance</c>。 </summary>
        /// <remarks> 6.x と同じく、渡したインスタンスの同一性は見ずに型の登録を破棄する。 </remarks>
        public static bool DestroyInstance<T>(T instance) where T : class
        {
            // 6.x は未初期化と null 候補を、破棄対象なしとして警告なしで返していた。
            if (!Partitura.IsInitialized) { return false; }
            if (instance == null) { return false; }

            // 6.x は型の破棄の成否にかかわらず true を返していたため、戻り値を合わせる。
            Partitura.Destroy<T>();
            return true;
        }

        /// <summary> 旧 <c>DestroyInstance</c>。<see cref="Partitura.Destroy{T}()" /> へ転送する。 </summary>
        public static bool DestroyInstance<T>() where T : class => Partitura.Destroy<T>();

        /// <summary> 旧 <c>IsExistInstance</c>。<see cref="Partitura.IsRegistered{T}()" /> へ転送する。 </summary>
        public static bool IsExistInstance<T>() where T : class => Partitura.IsRegistered<T>();

        /// <summary> 旧 <c>IsExistInstance</c>。 </summary>
        /// <remarks> 6.x と同じく、渡したインスタンスは見ずに型の登録だけを確かめる。 </remarks>
        public static bool IsExistInstance<T>(T instance) where T : class => Partitura.IsRegistered<T>();

        /// <summary> 旧 <c>IsExistInstance</c>。<see cref="Partitura.IsRegistered(global::System.Type)" /> へ転送する。 </summary>
        public static bool IsExistInstance(global::System.Type type) => Partitura.IsRegistered(type);

        /// <summary> <see cref="Partitura.GetRegistrationInfos" /> へ転送する。 </summary>
        public static IReadOnlyList<ServiceRegistrationInfo> GetRegistrationInfos() => Partitura.GetRegistrationInfos();

        /// <summary> <see cref="Partitura.TryGetRegistrationInfo" /> へ転送する。 </summary>
        public static bool TryGetRegistrationInfo(
            global::System.Type serviceType,
            out ServiceRegistrationInfo registrationInfo) =>
            Partitura.TryGetRegistrationInfo(serviceType, out registrationInfo);

        /// <summary> 旧 <c>GetInstance</c>。 </summary>
        /// <remarks> 6.x と同じく、未初期化・未登録・破棄済みのときは null を返す。 </remarks>
        public static T GetInstance<T>() where T : class => Partitura.TryGet(out T result) ? result : null;

        /// <summary> 旧 <c>GetRequiredInstance</c>。<see cref="Partitura.Get{T}" /> へ転送する。 </summary>
        public static T GetRequiredInstance<T>() where T : class => Partitura.Get<T>();

        /// <summary> 旧 <c>TryGetInstance</c>。<see cref="Partitura.TryGet{T}" /> へ転送する。 </summary>
        public static bool TryGetInstance<T>(out T result) where T : class => Partitura.TryGet(out result);

        /// <summary> 旧 <c>GetInstanceAsync</c>。<see cref="Partitura.GetAsync{T}" /> へ転送する。 </summary>
        public static Awaitable<T> GetInstanceAsync<T>(byte grace = 120, CancellationToken token = default)
            where T : class =>
            Partitura.GetAsync<T>(grace, token);

        /// <summary> 旧 <c>TryGetInstanceAsync</c>。<see cref="Partitura.TryGetAsync{T}" /> へ転送する。 </summary>
        public static Awaitable<(bool success, T result)> TryGetInstanceAsync<T>(
            byte grace = 120,
            CancellationToken token = default)
            where T : class =>
            Partitura.TryGetAsync<T>(grace, token);

        /// <summary> 旧 <c>RegisterAfterLocate</c>。<see cref="Partitura.WhenRegistered{T}" /> へ転送する。 </summary>
        public static void RegisterAfterLocate<T>(global::System.Action action) where T : class =>
            // null の検証は転送先に任せ、6.x と同じ順序（未初期化 → null）で例外を出す。
            Partitura.WhenRegistered<T>(action == null ? null : _ => action());

        /// <summary> 旧 <c>RegisterAfterLocate</c>。<see cref="Partitura.WhenRegistered{T}" /> へ転送する。 </summary>
        public static void RegisterAfterLocate<T>(global::System.Action<T> action) where T : class =>
            Partitura.WhenRegistered(action);

        #endregion

        #region 内部処理

        /// <summary>
        ///     6.x の登録方式を 7.0.0 の寿命へ変換する。
        /// </summary>
        /// <param name="type"> 6.x の登録方式。 </param>
        /// <returns> 対応する寿命。 </returns>
        /// <remarks> 値の順序（Singleton = Persistent、Locator = Scene）は同じにしてある。 </remarks>
        internal static ServiceLifetimeEnum ToLifetime(LocateTypeEnum type) => (ServiceLifetimeEnum)(byte)type;

        #endregion
    }
}
