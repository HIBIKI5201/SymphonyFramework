using SymphonyFramework.Services;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     6.x の <c>ServiceInjector</c> を <see cref="Partitura" /> へ転送する。
    /// </summary>
    /// <remarks> 7.0.0 の移行期間だけ残す。8.0.0 で削除する。 </remarks>
    [global::System.Obsolete("ServiceInjector は Partitura（名前空間 SymphonyFramework.Services）へまとめました。Partitura.Inject / Partitura.CreateInstance を使用してください。8.0.0 で削除します。")]
    public static class ServiceInjector
    {
        #region 外部向けAPI

        /// <summary> <see cref="Partitura.Inject{T0}" /> へ転送する。 </summary>
        public static void Inject<T0>(SymphonyFramework.Services.IInjectable<T0> target)
            where T0 : class =>
            Partitura.Inject(target);

        /// <summary> <see cref="Partitura.Inject{T0, T1}" /> へ転送する。 </summary>
        public static void Inject<T0, T1>(SymphonyFramework.Services.IInjectable<T0, T1> target)
            where T0 : class
            where T1 : class =>
            Partitura.Inject(target);

        /// <summary> <see cref="Partitura.Inject{T0, T1, T2}" /> へ転送する。 </summary>
        public static void Inject<T0, T1, T2>(SymphonyFramework.Services.IInjectable<T0, T1, T2> target)
            where T0 : class
            where T1 : class
            where T2 : class =>
            Partitura.Inject(target);

        /// <summary> <see cref="Partitura.Inject{T0, T1, T2, T3}" /> へ転送する。 </summary>
        public static void Inject<T0, T1, T2, T3>(SymphonyFramework.Services.IInjectable<T0, T1, T2, T3> target)
            where T0 : class
            where T1 : class
            where T2 : class
            where T3 : class =>
            Partitura.Inject(target);

        /// <summary> <see cref="Partitura.CreateInstance{T}" /> へ転送する。 </summary>
        public static T CreateInstance<T>() where T : class => Partitura.CreateInstance<T>();

        /// <summary> <see cref="Partitura.CreateInstance(global::System.Type)" /> へ転送する。 </summary>
        public static object CreateInstance(global::System.Type type) => Partitura.CreateInstance(type);

        #endregion
    }
}
