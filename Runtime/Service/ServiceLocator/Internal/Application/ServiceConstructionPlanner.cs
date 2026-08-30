using System;
using System.Reflection;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     コンストラクタ注入で使うコンストラクタを選び、引数を解決する。
    /// </summary>
    /// <remarks>
    ///     **Service LocatorもUnity APIも参照しない。** 引数の解決手段を委譲で受け取るため、
    ///     Service Locatorを初期化せずに単体テストできる。
    /// </remarks>
    internal static class ServiceConstructionPlanner
    {
        #region 外部向けAPI

        /// <summary>
        ///     コンストラクタ注入に使うコンストラクタを選ぶ。
        /// </summary>
        /// <param name="type"> 生成する型。 </param>
        /// <returns> 選ばれたコンストラクタ。 </returns>
        /// <exception cref="ArgumentNullException"> typeがnullの場合。 </exception>
        /// <exception cref="InvalidOperationException">
        ///     publicなコンストラクタが無い場合、または引数が最多のものを1つに決められない場合。
        /// </exception>
        internal static ConstructorInfo SelectConstructor(Type type)
        {
            if (type == null) { throw new ArgumentNullException(nameof(type)); }

            // 利用側が「生成してよい」と表明した形だけを候補にする。
            ConstructorInfo[] constructors = type.GetConstructors(
                BindingFlags.Instance | BindingFlags.Public);

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{LOG_PREFIX} {type.FullName} にpublicなコンストラクタがありません。"
                    + " コンストラクタ注入で生成する型には、publicなコンストラクタが必要です。");
            }

            // 依存を最も多く受け取る形を、注入を意図した形とみなす。
            int maxParameterCount = -1;
            int tiedCount = 0;
            ConstructorInfo selected = null;
            foreach (ConstructorInfo constructor in constructors)
            {
                int parameterCount = constructor.GetParameters().Length;
                if (parameterCount > maxParameterCount)
                {
                    maxParameterCount = parameterCount;
                    tiedCount = 1;
                    selected = constructor;
                    continue;
                }

                if (parameterCount == maxParameterCount) { tiedCount++; }
            }

            // 同数で並んだ場合はどれを使うか決められないため、推測せず利用側へ返す。
            if (tiedCount > 1)
            {
                throw new InvalidOperationException(
                    $"{LOG_PREFIX} {type.FullName} には引数{maxParameterCount}件のpublicなコンストラクタが"
                    + $"{tiedCount}個あり、どれを使うか決められません。"
                    + " publicなコンストラクタを1つに絞るか、引数の数を変えてください。");
            }

            return selected;
        }

        /// <summary>
        ///     コンストラクタの引数を解決する。
        /// </summary>
        /// <param name="constructor"> 引数を解決するコンストラクタ。 </param>
        /// <param name="resolve"> 型から登録済みインスタンスを返す処理。未登録の場合はnullを返す。 </param>
        /// <returns> 宣言順に並んだ引数。 </returns>
        /// <exception cref="ArgumentNullException"> constructorまたはresolveがnullの場合。 </exception>
        /// <exception cref="ServiceNotRegisteredException"> 解決できず既定値も無い引数がある場合。 </exception>
        /// <remarks>
        ///     **1つでも解決できない場合は例外にし、配列を返さない。** 半端に構築された対象を残さないためである。
        /// </remarks>
        internal static object[] ResolveArguments(
            ConstructorInfo constructor,
            Func<Type, object> resolve)
        {
            if (constructor == null) { throw new ArgumentNullException(nameof(constructor)); }
            if (resolve == null) { throw new ArgumentNullException(nameof(resolve)); }

            ParameterInfo[] parameters = constructor.GetParameters();
            if (parameters.Length == 0) { return Array.Empty<object>(); }

            object[] arguments = new object[parameters.Length];
            for (int index = 0; index < parameters.Length; index++)
            {
                ParameterInfo parameter = parameters[index];

                // 登録があれば、それをそのまま渡す。
                object instance = resolve(parameter.ParameterType);
                if (instance != null)
                {
                    arguments[index] = instance;
                    continue;
                }

                // 登録が無くても既定値があるなら、設定値としてコンストラクタへ書けたものとして扱う。
                if (parameter.HasDefaultValue)
                {
                    arguments[index] = parameter.DefaultValue;
                    continue;
                }

                throw new ServiceNotRegisteredException(parameter.ParameterType);
            }

            return arguments;
        }

        /// <summary>
        ///     コンストラクタ注入で生成できる型か検証する。
        /// </summary>
        /// <param name="type"> 検証する型。 </param>
        /// <param name="parameterName"> 例外へ載せる引数名。 </param>
        /// <exception cref="ArgumentNullException"> typeがnullの場合。 </exception>
        /// <exception cref="ArgumentException"> インスタンスを生成できない型の場合。 </exception>
        internal static void ValidateCreatableType(Type type, string parameterName)
        {
            if (type == null) { throw new ArgumentNullException(parameterName); }

            // 生成できない型は、依存の解決を始める前に呼び出し側の誤りとして返す。
            if (type.IsAbstract || type.IsInterface)
            {
                throw new ArgumentException(
                    $"{LOG_PREFIX} {type.FullName} は抽象型のためインスタンスを生成できません。",
                    parameterName);
            }

            if (type.IsValueType)
            {
                throw new ArgumentException(
                    $"{LOG_PREFIX} {type.FullName} は値型のためコンストラクタ注入の対象になりません。",
                    parameterName);
            }

            if (type.ContainsGenericParameters)
            {
                throw new ArgumentException(
                    $"{LOG_PREFIX} {type.FullName} は型引数が未確定のためインスタンスを生成できません。",
                    parameterName);
            }
        }

        #endregion

        #region 内部処理

        /// <summary> Consoleでサブシステムを判別するためのメッセージ接頭辞。 </summary>
        private const string LOG_PREFIX = "[" + nameof(ServiceInjector) + "]";

        #endregion
    }
}
