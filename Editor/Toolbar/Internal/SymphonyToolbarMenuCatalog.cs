#if UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;

using SymphonyFrameWork.Debugger.Logger;

using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     ツールバープルダウンへ登録された項目を探索して並べる。
    /// </summary>
    internal static class SymphonyToolbarMenuCatalog
    {
        #region 外部向けAPI

        /// <summary>
        ///     登録属性を持つ項目を生成し、表示順に並べる。
        /// </summary>
        /// <returns> メニューへ追加する項目。 </returns>
        internal static IReadOnlyList<ISymphonyToolbarMenuItem> CreateItems()
        {
            return CreateItems(
                TypeCache.GetTypesWithAttribute<SymphonyToolbarMenuItemAttribute>());
        }

        /// <summary>
        ///     候補型から有効な項目を生成し、表示順に並べる。
        /// </summary>
        /// <param name="candidateTypes"> 登録候補の型。 </param>
        /// <returns> 生成できたメニュー項目。 </returns>
        internal static IReadOnlyList<ISymphonyToolbarMenuItem> CreateItems(
            IEnumerable<Type> candidateTypes)
        {
            List<ISymphonyToolbarMenuItem> items = new();
            foreach (Type candidateType in candidateTypes)
            {
                if (candidateType.IsAbstract
                    || !typeof(ISymphonyToolbarMenuItem).IsAssignableFrom(
                        candidateType))
                {
                    SymphonyDebugLogger.LogDirect(
                        $"ツールバーメニュー項目を生成できません: {candidateType.FullName}",
                        LogKindEnum.Error);
                    continue;
                }

                try
                {
                    object instance = Activator.CreateInstance(
                        candidateType,
                        nonPublic: true);
                    if (instance is ISymphonyToolbarMenuItem item)
                    {
                        items.Add(item);
                    }
                }
                catch (Exception exception)
                {
                    SymphonyDebugLogger.LogException(exception);
                }
            }

            return OrderItems(items);
        }

        /// <summary>
        ///     項目を優先度、同値の場合は表示パスの順に並べる。
        /// </summary>
        /// <param name="items"> 並べ替える項目。 </param>
        /// <returns> 表示順に並んだ新しい配列。 </returns>
        internal static IReadOnlyList<ISymphonyToolbarMenuItem> OrderItems(
            IEnumerable<ISymphonyToolbarMenuItem> items)
        {
            return items
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.Path, StringComparer.Ordinal)
                .ToArray();
        }

        #endregion
    }
}
#endif
