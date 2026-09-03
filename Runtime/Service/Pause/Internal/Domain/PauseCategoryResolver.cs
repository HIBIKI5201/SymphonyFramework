using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ対象が属するカテゴリーを、実装しているinterfaceから解決する。
    /// </summary>
    /// <remarks>
    ///     カテゴリーは <see cref="PauseManager.IPausable" /> を継承した空のinterfaceで表す。
    ///     Unity APIへ触れないため、EditModeテストから直接検証できる。
    /// </remarks>
    internal static class PauseCategoryResolver
    {
        #region 外部向けAPI

        /// <summary> カテゴリーを1つも実装していない対象が属するカテゴリー。 </summary>
        /// <remarks>
        ///     **カテゴリー未指定の対象を「どこにも属さない」にはしない。**
        ///     どのカテゴリーを止めても動き続ける対象が黙って生まれると、
        ///     ポーズしても止まらない原因を追えない。
        /// </remarks>
        internal static Type DefaultCategory { get; } = typeof(PauseManager.IPausable);

        /// <summary>
        ///     対象の型が属するカテゴリーを列挙する。
        /// </summary>
        /// <param name="pausableType"> 判定するポーズ対象の型。 </param>
        /// <returns>
        ///     実装しているカテゴリー。1つも実装していない場合は
        ///     <see cref="DefaultCategory" /> だけを含む一覧。
        /// </returns>
        /// <exception cref="ArgumentNullException"> pausableTypeがnullの場合。 </exception>
        internal static IReadOnlyList<Type> Resolve(Type pausableType)
        {
            if (pausableType == null) { throw new ArgumentNullException(nameof(pausableType)); }

            List<Type> categories = new();
            foreach (Type interfaceType in pausableType.GetInterfaces())
            {
                if (!IsCategory(interfaceType)) { continue; }

                categories.Add(interfaceType);
            }

            // 派生カテゴリーを実装した対象は基底カテゴリーにも属する。GetInterfacesが両方返すため、
            // どちらを止めても対象へ届く。ここで基底を落とすと、基底を止めても止まらなくなる。
            if (categories.Count == 0) { categories.Add(DefaultCategory); }

            return categories;
        }

        /// <summary>
        ///     カテゴリーとして使える型かを判定する。
        /// </summary>
        /// <param name="type"> 判定する型。 </param>
        /// <returns> カテゴリーとして使える場合はtrue。 </returns>
        /// <remarks>
        ///     <see cref="PauseManager.IPausable" /> 自身はカテゴリーではなく、
        ///     カテゴリー未指定の対象を表す <see cref="DefaultCategory" /> として扱う。
        /// </remarks>
        internal static bool IsCategory(Type type)
        {
            if (type == null) { return false; }
            if (!type.IsInterface) { return false; }

            // IPausable自身は「カテゴリーを明示していない」ことを表すため、カテゴリーには数えない。
            if (type == DefaultCategory) { return false; }

            return DefaultCategory.IsAssignableFrom(type);
        }

        /// <summary>
        ///     ポーズ状態の操作対象として指定できる型かを判定する。
        /// </summary>
        /// <param name="type"> 判定する型。 </param>
        /// <returns> 操作対象にできる場合はtrue。 </returns>
        /// <remarks>
        ///     <see cref="IsCategory" /> と違い、<see cref="DefaultCategory" /> も含む。
        ///     既定カテゴリーは「カテゴリーを明示していない対象」を指す操作対象として使える。
        /// </remarks>
        internal static bool IsOperableCategory(Type type)
        {
            if (type == null) { return false; }
            if (!type.IsInterface) { return false; }

            return DefaultCategory.IsAssignableFrom(type);
        }

        /// <summary>
        ///     ポーズ状態の操作対象として指定できる型かを検証する。
        /// </summary>
        /// <param name="type"> 検証する型。 </param>
        /// <param name="parameterName"> 公開APIで使用されている型引数名。 </param>
        /// <exception cref="ArgumentNullException"> typeがnullの場合。 </exception>
        /// <exception cref="ArgumentException"> カテゴリーとして使えない型の場合。 </exception>
        /// <remarks>
        ///     **型制約だけでは表現できない。** <c>where TCategory : IPausable</c> は
        ///     <c>IPausable</c> を実装した具象クラスも通してしまうため、実行時に弾く。
        /// </remarks>
        internal static void ValidateOperableCategory(Type type, string parameterName)
        {
            if (type == null) { throw new ArgumentNullException(parameterName); }

            if (!IsOperableCategory(type))
            {
                throw new ArgumentException(
                    $"{type.Name} はカテゴリーとして使えません。"
                    + $" カテゴリーは {DefaultCategory.Name} を継承したinterfaceで指定してください。",
                    parameterName);
            }
        }

        #endregion
    }
}
