using System;
using Api = SymphonyFrameWork.System.API;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     <see cref="Api.ServiceInjector"/> へ移動しました。
    /// </summary>
    [Obsolete("SymphonyFrameWork.System.API.ServiceInjector に移動しました。今後はそちらを使用してください。", error: false)]
    public static class ServiceInjector
    {
        /// <summary> Service Locatorから1件の依存関係を解決して対象へ注入する。 </summary>
        public static void Inject<T0>(IInjectable<T0> target)
            where T0 : class
            => Api.ServiceInjector.Inject(target);

        /// <summary> Service Locatorから2件の依存関係を解決して対象へ注入する。 </summary>
        public static void Inject<T0, T1>(IInjectable<T0, T1> target)
            where T0 : class
            where T1 : class
            => Api.ServiceInjector.Inject(target);

        /// <summary> Service Locatorから3件の依存関係を解決して対象へ注入する。 </summary>
        public static void Inject<T0, T1, T2>(IInjectable<T0, T1, T2> target)
            where T0 : class
            where T1 : class
            where T2 : class
            => Api.ServiceInjector.Inject(target);

        /// <summary> Service Locatorから4件の依存関係を解決して対象へ注入する。 </summary>
        public static void Inject<T0, T1, T2, T3>(IInjectable<T0, T1, T2, T3> target)
            where T0 : class
            where T1 : class
            where T2 : class
            where T3 : class
            => Api.ServiceInjector.Inject(target);
    }
}
