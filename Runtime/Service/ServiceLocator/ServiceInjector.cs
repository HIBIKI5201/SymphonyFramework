using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     IInjectableへ登録済みサービスを注入する。
    /// </summary>
    /// <remarks> SceneLoaderはシーンロード時にこのクラスを介して自動注入する。 </remarks>
    public static class ServiceInjector
    {
        #region 外部向けAPI

        /// <summary>
        ///     1件の依存関係を解決して対象へ注入する。
        /// </summary>
        /// <exception cref="ArgumentNullException"> 注入対象がnullの場合。 </exception>
        /// <exception cref="ServiceNotRegisteredException"> 必須サービスが未登録の場合。 </exception>
        public static void Inject<T0>(IInjectable<T0> target)
            where T0 : class
        {
            // 注入処理の途中で失敗させず、対象の指定漏れを先に通知する。
            if (target == null) { throw new ArgumentNullException(nameof(target)); }

            // 必須サービスを解決してから対象固有の注入処理へ渡す。
            target.Inject(ServiceLocator.GetRequiredInstance<T0>());
        }

        /// <summary>
        ///     2件の依存関係を解決して対象へ注入する。
        /// </summary>
        /// <exception cref="ArgumentNullException"> 注入対象がnullの場合。 </exception>
        /// <exception cref="ServiceNotRegisteredException"> 必須サービスが未登録の場合。 </exception>
        public static void Inject<T0, T1>(IInjectable<T0, T1> target)
            where T0 : class
            where T1 : class
        {
            // 注入処理の途中で失敗させず、対象の指定漏れを先に通知する。
            if (target == null) { throw new ArgumentNullException(nameof(target)); }

            // すべての必須サービスを解決してから対象固有の注入処理へ渡す。
            target.Inject(
                ServiceLocator.GetRequiredInstance<T0>(),
                ServiceLocator.GetRequiredInstance<T1>());
        }

        /// <summary>
        ///     3件の依存関係を解決して対象へ注入する。
        /// </summary>
        /// <exception cref="ArgumentNullException"> 注入対象がnullの場合。 </exception>
        /// <exception cref="ServiceNotRegisteredException"> 必須サービスが未登録の場合。 </exception>
        public static void Inject<T0, T1, T2>(IInjectable<T0, T1, T2> target)
            where T0 : class
            where T1 : class
            where T2 : class
        {
            // 注入処理の途中で失敗させず、対象の指定漏れを先に通知する。
            if (target == null) { throw new ArgumentNullException(nameof(target)); }

            // すべての必須サービスを解決してから対象固有の注入処理へ渡す。
            target.Inject(
                ServiceLocator.GetRequiredInstance<T0>(),
                ServiceLocator.GetRequiredInstance<T1>(),
                ServiceLocator.GetRequiredInstance<T2>());
        }

        /// <summary>
        ///     4件の依存関係を解決して対象へ注入する。
        /// </summary>
        /// <exception cref="ArgumentNullException"> 注入対象がnullの場合。 </exception>
        /// <exception cref="ServiceNotRegisteredException"> 必須サービスが未登録の場合。 </exception>
        public static void Inject<T0, T1, T2, T3>(IInjectable<T0, T1, T2, T3> target)
            where T0 : class
            where T1 : class
            where T2 : class
            where T3 : class
        {
            // 注入処理の途中で失敗させず、対象の指定漏れを先に通知する。
            if (target == null) { throw new ArgumentNullException(nameof(target)); }

            // すべての必須サービスを解決してから対象固有の注入処理へ渡す。
            target.Inject(
                ServiceLocator.GetRequiredInstance<T0>(),
                ServiceLocator.GetRequiredInstance<T1>(),
                ServiceLocator.GetRequiredInstance<T2>(),
                ServiceLocator.GetRequiredInstance<T3>());
        }

        /// <summary>
        ///     依存をService Locatorから解決してインスタンスを生成する。
        /// </summary>
        /// <typeparam name="T"> 生成する型。 </typeparam>
        /// <returns> 生成したインスタンス。 </returns>
        /// <exception cref="ArgumentException"> Tがインスタンスを生成できない型の場合。 </exception>
        /// <exception cref="InvalidOperationException">
        ///     publicなコンストラクタが無い場合、または引数が最多のものを1つに決められない場合。
        /// </exception>
        /// <exception cref="ServiceNotRegisteredException"> 解決できず既定値も無い引数がある場合。 </exception>
        public static T CreateInstance<T>() where T : class =>
            (T)CreateInstance(typeof(T));

        /// <summary>
        ///     依存をService Locatorから解決してインスタンスを生成する。
        /// </summary>
        /// <param name="type"> 生成する型。 </param>
        /// <returns> 生成したインスタンス。 </returns>
        /// <exception cref="ArgumentNullException"> typeがnullの場合。 </exception>
        /// <exception cref="ArgumentException"> typeがインスタンスを生成できない型の場合。 </exception>
        /// <exception cref="InvalidOperationException">
        ///     publicなコンストラクタが無い場合、または引数が最多のものを1つに決められない場合。
        /// </exception>
        /// <exception cref="ServiceNotRegisteredException"> 解決できず既定値も無い引数がある場合。 </exception>
        /// <remarks>
        ///     **publicなコンストラクタのうち、引数が最も多いものを使う。**
        ///     引数はService Locatorの登録から解決し、登録が無い引数は既定値があればそれを使う。
        ///     生成したインスタンスはService Locatorへ登録しない。
        /// </remarks>
        public static object CreateInstance(Type type)
        {
            // 生成できない型は、依存の解決を始める前に拒否する。
            ServiceConstructionPlanner.ValidateCreatableType(type, nameof(type));

            ConstructorInfo constructor = ServiceConstructionPlanner.SelectConstructor(type);

            // 全引数を解決してから1回だけ呼び、半端に構築された対象を残さない。
            object[] arguments = ServiceConstructionPlanner.ResolveArguments(
                constructor,
                ResolveService);

            // Reflectionのラッパー例外を除き、コンストラクタの失敗を元のスタックトレースで伝播する。
            try
            {
                return constructor.Invoke(arguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        #endregion

        #region 内部処理

        private static readonly Dictionary<Type, MethodInfo> _injectMethods = BuildInjectMethodMap();

        /// <summary>
        ///     コンストラクタ引数をService Locatorの登録から解決する。
        /// </summary>
        /// <param name="serviceType"> 解決する引数の型。 </param>
        /// <returns> 登録済みインスタンス。登録が無い場合はnull。 </returns>
        private static object ResolveService(Type serviceType) =>
            ServiceLocator.TryGetInstance(serviceType, out object instance) ? instance : null;

        /// <summary>
        ///     IInjectable&lt;...&gt;の各アリティと、対応するInjectメソッドの対応表を構築する。
        /// </summary>
        private static Dictionary<Type, MethodInfo> BuildInjectMethodMap()
        {
            Dictionary<Type, MethodInfo> map = new();

            // 公開InjectオーバーロードをアリティごとのIInjectable定義へ対応付ける。
            foreach (MethodInfo method in typeof(ServiceInjector).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                // Inject以外の公開staticメソッドは自動注入の候補に含めない。
                if (method.Name != nameof(Inject)) { continue; }

                Type parameterType = method.GetParameters()[0].ParameterType;
                map[parameterType.GetGenericTypeDefinition()] = method;
            }

            return map;
        }

        /// <summary>
        ///     対象が実装するIInjectableに対応したInjectを呼び出す。
        /// </summary>
        /// <param name="target"> 注入対象。 </param>
        /// <returns> 注入を実行できた場合はtrue。targetがどのIInjectable系interfaceも実装していない場合はfalse。 </returns>
        internal static bool TryAutoInject(IInjectable target)
        {
            // 注入対象が無い場合は、対応interfaceの探索を行わない。
            if (target == null) { return false; }

            // 対象が実装するinterfaceから、対応する最初のInjectオーバーロードを探す。
            foreach (Type interfaceType in target.GetType().GetInterfaces())
            {
                // 非ジェネリックinterfaceはIInjectable<T...>のいずれにも該当しない。
                if (!interfaceType.IsGenericType) { continue; }

                // 対応する公開Injectが無いジェネリックinterfaceは自動注入の対象外とする。
                if (!_injectMethods.TryGetValue(
                    interfaceType.GetGenericTypeDefinition(),
                    out MethodInfo method))
                {
                    continue;
                }

                // Reflectionのラッパー例外を除き、実際の注入失敗を元のスタックトレースで伝播する。
                try
                {
                    method.MakeGenericMethod(interfaceType.GetGenericArguments()).Invoke(null, new object[] { target });
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    throw;
                }

                return true;
            }

            return false;
        }

        #endregion
    }
}
