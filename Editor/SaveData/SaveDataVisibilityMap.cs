using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     セーブデータ型の既定値と上書き値から管理対象かを解決する。
    /// </summary>
    internal sealed class SaveDataVisibilityMap
    {
        #region 外部向けAPI

        /// <summary>
        ///     型カタログと保存済み上書き値から解決表を構築する。
        /// </summary>
        /// <param name="catalogEntries"> 型の完全名とテストアセンブリ所属かの対応。 </param>
        /// <param name="overrides"> 型の完全名と明示的な管理対象値の対応。 </param>
        internal SaveDataVisibilityMap(
            IEnumerable<KeyValuePair<string, bool>> catalogEntries,
            IReadOnlyDictionary<string, bool> overrides)
        {
            // カタログに存在する型だけを解決対象として保持する。
            if (catalogEntries == null) { return; }

            foreach (KeyValuePair<string, bool> entry in catalogEntries)
            {
                if (string.IsNullOrEmpty(entry.Key)) { continue; }

                // 明示値を既定値より優先し、未指定ならテスト型だけを除外する。
                bool isManaged = overrides != null
                    && overrides.TryGetValue(entry.Key, out bool overridden)
                        ? overridden
                        : !entry.Value;
                _managedTypes[entry.Key] = isManaged;
            }
        }

        /// <summary>
        ///     指定した型が管理対象かを返す。
        /// </summary>
        /// <param name="typeFullName"> 判定する型の完全名。 </param>
        /// <returns> カタログに存在し、解決結果が管理対象ならtrue。 </returns>
        internal bool IsManaged(string typeFullName)
        {
            // カタログ外の型名は、保存済み上書きが残っていても表示しない。
            return !string.IsNullOrEmpty(typeFullName)
                && _managedTypes.TryGetValue(typeFullName, out bool isManaged)
                && isManaged;
        }

        #endregion

        #region 内部処理

        private readonly Dictionary<string, bool> _managedTypes = new(StringComparer.Ordinal);

        #endregion
    }
}
