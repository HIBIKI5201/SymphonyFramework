using System;
using System.Collections.Generic;
using System.Linq;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     <see cref="LegacyNameRewriter.Rewrite" /> の結果。
    /// </summary>
    internal sealed class LegacyNameRewriteResult
    {
        #region 外部向けAPI

        /// <summary>
        ///     結果を指定して生成する。
        /// </summary>
        /// <param name="text"> 書き換えた本文。 </param>
        /// <param name="isChanged"> 本文が変わったか。 </param>
        /// <param name="replacements"> 置き換えた旧名（「旧 → 新」の形）。 </param>
        /// <param name="addedUsings"> 足した using の名前空間。 </param>
        /// <param name="removedUsings"> 消した旧 using の名前空間。 </param>
        /// <param name="remainingForwarders"> 書き換えずに残した転送の型名。 </param>
        /// <param name="unresolved"> 自動では書き換えられなかった内容。 </param>
        internal LegacyNameRewriteResult(
            string text,
            bool isChanged,
            IEnumerable<string> replacements,
            IEnumerable<string> addedUsings,
            IEnumerable<string> removedUsings,
            IEnumerable<string> remainingForwarders,
            IEnumerable<string> unresolved)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            IsChanged = isChanged;
            Replacements = (replacements ?? Array.Empty<string>()).ToArray();
            AddedUsings = (addedUsings ?? Array.Empty<string>()).ToArray();
            RemovedUsings = (removedUsings ?? Array.Empty<string>()).ToArray();
            RemainingForwarders = (remainingForwarders ?? Array.Empty<string>()).ToArray();
            Unresolved = (unresolved ?? Array.Empty<string>()).ToArray();
        }

        /// <summary> 書き換えた本文。変わらなければ元の本文。 </summary>
        internal string Text { get; }

        /// <summary> 本文が変わったか。 </summary>
        internal bool IsChanged { get; }

        /// <summary> 置き換えた旧名（「旧 → 新」の形）。 </summary>
        internal IReadOnlyList<string> Replacements { get; }

        /// <summary> 足した using の名前空間。 </summary>
        internal IReadOnlyList<string> AddedUsings { get; }

        /// <summary> 消した旧 using の名前空間。 </summary>
        internal IReadOnlyList<string> RemovedUsings { get; }

        /// <summary> 書き換えずに残した転送の型名。<c>[Obsolete]</c> の警告が残る。 </summary>
        internal IReadOnlyList<string> RemainingForwarders { get; }

        /// <summary> 自動では書き換えられなかった内容。 </summary>
        internal IReadOnlyList<string> Unresolved { get; }

        /// <summary>
        ///     本文を変えなかった結果を作る。
        /// </summary>
        /// <param name="source"> 元の本文。 </param>
        /// <returns> 結果。 </returns>
        internal static LegacyNameRewriteResult Unchanged(string source)
        {
            return new LegacyNameRewriteResult(source, false, null, null, null, null, null);
        }

        /// <summary>
        ///     本文を変えなかったが、残した転送や書き換えられなかった内容がある結果を作る。
        /// </summary>
        /// <param name="source"> 元の本文。 </param>
        /// <param name="usedForwarders"> 旧名前空間ごとの、使われている転送の型名。 </param>
        /// <param name="unresolved"> 自動では書き換えられなかった内容。 </param>
        /// <returns> 結果。 </returns>
        internal static LegacyNameRewriteResult Unchanged(
            string source,
            IReadOnlyDictionary<string, HashSet<string>> usedForwarders,
            IEnumerable<string> unresolved)
        {
            IEnumerable<string> forwarders = usedForwarders?.Values
                .SelectMany(names => names)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal);
            return new LegacyNameRewriteResult(source, false, null, null, null, forwarders, unresolved);
        }

        #endregion
    }
}
