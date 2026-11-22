using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     利用側スクリプトの本文にある 6.x の名前（<c>SymphonyFrameWork.*</c>）を、7.0.0 の名前へ書き換える。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unity の API Updater は、Assets 内のソースとしてコンパイルされるパッケージの <c>[MovedFrom]</c> では
    ///         利用側を書き換えなかった（6000.3.10f1 で3回確認）。そのため、パッケージ自身が本文を書き換える。
    ///     </para>
    ///     <para>
    ///         Unity API に触れない純粋な関数にし、文字列の入出力だけでテストする。
    ///         コメントと文字列は書き換えず、メンバーアクセス（<c>.</c> の直後）の識別子も置き換えない。
    ///     </para>
    ///     <para>
    ///         **転送（<c>ServiceLocator</c> など）は書き換えない。** 名前もメンバーも変わるため、
    ///         機械的に置き換えると別の API を呼ぶことになる。転送を使い続ける間は旧 <c>using</c> を残し、
    ///         同じ名前の型を持つ新しい名前空間は <c>using</c> せずに <c>global::</c> 付きで書く（CS0104 を避ける）。
    ///     </para>
    /// </remarks>
    internal static class LegacyNameRewriter
    {
        #region 外部向けAPI

        /// <summary>
        ///     本文の旧名を書き換える。
        /// </summary>
        /// <param name="source"> スクリプトの本文。BOM を含めない。 </param>
        /// <param name="map"> 対応表。 </param>
        /// <returns> 書き換えた結果。旧名が無ければ本文をそのまま返す。 </returns>
        internal static LegacyNameRewriteResult Rewrite(string source, LegacyNameMap map)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (map == null) { throw new ArgumentNullException(nameof(map)); }

            if (source.IndexOf(LegacyNameMap.LEGACY_ROOT, StringComparison.Ordinal) < 0)
            {
                return LegacyNameRewriteResult.Unchanged(source);
            }

            return new Context(source, map).Run();
        }

        #endregion

        #region 内部処理

        /// <summary> 直後の識別子が宣言の名前になるキーワード。宣言は利用側の型なので書き換えない。 </summary>
        private static readonly HashSet<string> DeclarationKeywords = new(StringComparer.Ordinal)
        {
            "class", "struct", "interface", "enum", "record", "delegate", "namespace",
        };

        /// <summary>
        ///     本文の字句。識別子と記号だけを持ち、コメント・文字列・プリプロセッサ行は含めない。
        /// </summary>
        private readonly struct Token
        {
            internal Token(bool isIdentifier, string text, int start, int end)
            {
                IsIdentifier = isIdentifier;
                Text = text;
                Start = start;
                End = end;
            }

            internal bool IsIdentifier { get; }
            internal string Text { get; }
            internal int Start { get; }
            internal int End { get; }
        }

        /// <summary>
        ///     <c>using</c> ディレクティブ。
        /// </summary>
        private readonly struct UsingDirective
        {
            internal UsingDirective(string name, int firstToken, int lastToken)
            {
                Name = name;
                FirstToken = firstToken;
                LastToken = lastToken;
            }

            /// <summary> 取り込む名前空間。 </summary>
            internal string Name { get; }

            /// <summary> 先頭の字句（<c>global</c> または <c>using</c>）。 </summary>
            internal int FirstToken { get; }

            /// <summary> 末尾の字句（<c>;</c>）。 </summary>
            internal int LastToken { get; }
        }

        /// <summary>
        ///     本文の置き換え1件。
        /// </summary>
        private readonly struct Edit
        {
            internal Edit(int start, int end, string text)
            {
                Start = start;
                End = end;
                Text = text;
            }

            internal int Start { get; }
            internal int End { get; }
            internal string Text { get; }
        }

        /// <summary>
        ///     型名で書かれた旧名1件と、その書き換え先。
        /// </summary>
        private readonly struct SimpleNameUse
        {
            internal SimpleNameUse(int token, LegacyNameMap.Entry entry)
            {
                Token = token;
                Entry = entry;
            }

            internal int Token { get; }
            internal LegacyNameMap.Entry Entry { get; }
        }

        /// <summary>
        ///     1ファイル分の書き換えの状態。
        /// </summary>
        private sealed class Context
        {
            internal Context(string source, LegacyNameMap map)
            {
                _source = source;
                _map = map;
                _tokens = Tokenize(source);
                _newLine = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            }

            internal LegacyNameRewriteResult Run()
            {
                List<UsingDirective> allUsings = FindUsingDirectives();
                List<UsingDirective> legacyUsings = allUsings
                    .Where(directive => LegacyNameMap.IsLegacyNamespace(directive.Name))
                    .ToList();

                // using で取り込んだ旧名前空間に加え、旧名前空間の中で書かれたコードは外側の名前空間も見える。
                List<string> imported = legacyUsings.Select(directive => directive.Name).ToList();
                AddEnclosingLegacyNamespaces(imported);

                ScanIdentifiers(imported);

                Dictionary<string, List<SimpleNameUse>> usesByNamespace = _simpleUses
                    .GroupBy(use => use.Entry.NewNamespace, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
                HashSet<string> extensionNamespaces = FindExtensionNamespaces(imported);
                HashSet<string> existingUsings = new(allUsings.Select(directive => directive.Name), StringComparer.Ordinal);
                HashSet<string> usedForwarderNames = new(_usedForwarders.Values.SelectMany(names => names), StringComparer.Ordinal);

                List<string> addedUsings = new();
                foreach (string newNamespace in usesByNamespace.Keys.Union(extensionNamespaces).OrderBy(name => name, StringComparer.Ordinal))
                {
                    usesByNamespace.TryGetValue(newNamespace, out List<SimpleNameUse> uses);
                    uses ??= new List<SimpleNameUse>();

                    // 残す転送と同じ名前の型がある名前空間を using すると、転送の名前が曖昧になる。
                    string[] conflicts = usedForwarderNames.Where(name => _map.HasType(newNamespace, name)).ToArray();
                    if (conflicts.Length > 0)
                    {
                        foreach (SimpleNameUse use in uses)
                        {
                            ReplaceSimpleName(use, $"global::{use.Entry.NewNamespace}.{use.Entry.NewName}");
                        }

                        if (extensionNamespaces.Contains(newNamespace))
                        {
                            _unresolved.Add(
                                $"拡張メソッドの名前空間 {newNamespace} は、使い続ける転送（{string.Join(", ", conflicts)}）と名前がぶつかるため追加しませんでした。");
                        }

                        continue;
                    }

                    foreach (SimpleNameUse use in uses)
                    {
                        ReplaceSimpleName(use, use.Entry.NewName);
                    }

                    if (!existingUsings.Contains(newNamespace)) { addedUsings.Add(newNamespace); }
                }

                List<UsingDirective> removedUsings = legacyUsings
                    .Where(directive => !_usedForwarders.ContainsKey(directive.Name))
                    .ToList();
                EditUsingDirectives(allUsings, removedUsings, addedUsings);

                string text = ApplyEdits();
                if (text == _source) { return LegacyNameRewriteResult.Unchanged(_source, _usedForwarders, _unresolved); }

                return new LegacyNameRewriteResult(
                    text,
                    true,
                    _replacements,
                    addedUsings,
                    removedUsings.Select(directive => directive.Name).ToArray(),
                    _usedForwarders.Values.SelectMany(names => names).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray(),
                    _unresolved);
            }

            private readonly string _source;
            private readonly LegacyNameMap _map;
            private readonly List<Token> _tokens;
            private readonly string _newLine;
            private readonly HashSet<int> _directiveTokens = new();
            private readonly List<Edit> _edits = new();
            private readonly List<SimpleNameUse> _simpleUses = new();
            private readonly Dictionary<string, HashSet<string>> _usedForwarders = new(StringComparer.Ordinal);
            private readonly List<string> _replacements = new();
            private readonly List<string> _unresolved = new();

            /// <summary>
            ///     <c>using A.B;</c> と <c>global using A.B;</c> を探す。別名と <c>using static</c> は対象外。
            /// </summary>
            private List<UsingDirective> FindUsingDirectives()
            {
                List<UsingDirective> directives = new();
                for (int i = 0; i < _tokens.Count; i++)
                {
                    if (!IsIdentifier(i, "using")) { continue; }

                    int first = i > 0 && IsIdentifier(i - 1, "global") ? i - 1 : i;
                    string before = first > 0 ? _tokens[first - 1].Text : ";";
                    if (before != ";" && before != "{" && before != "}" && before != "]") { continue; }

                    if (!TryReadDottedName(i + 1, out string name, out int next)) { continue; }
                    if (next >= _tokens.Count || _tokens[next].Text != ";") { continue; }

                    directives.Add(new UsingDirective(name, first, next));
                    for (int j = first; j <= next; j++) { _directiveTokens.Add(j); }
                }

                return directives;
            }

            /// <summary>
            ///     <c>namespace SymphonyFrameWork.X</c> の中のコードは、外側の旧名前空間も見えるため取り込み済みに加える。
            /// </summary>
            private void AddEnclosingLegacyNamespaces(List<string> imported)
            {
                for (int i = 0; i < _tokens.Count; i++)
                {
                    if (!IsIdentifier(i, "namespace")) { continue; }
                    if (!TryReadDottedName(i + 1, out string name, out int next)) { continue; }

                    for (int j = i; j < next; j++) { _directiveTokens.Add(j); }
                    if (!LegacyNameMap.IsLegacyNamespace(name)) { continue; }

                    string[] parts = name.Split('.');
                    for (int length = 1; length <= parts.Length; length++)
                    {
                        string enclosing = string.Join(".", parts, 0, length);
                        if (!imported.Contains(enclosing)) { imported.Add(enclosing); }
                    }
                }
            }

            /// <summary>
            ///     識別子を走査し、完全修飾の旧名を置き換え、型名で書かれた旧名と転送の利用を集める。
            /// </summary>
            private void ScanIdentifiers(List<string> imported)
            {
                for (int i = 0; i < _tokens.Count; i++)
                {
                    Token token = _tokens[i];
                    if (!token.IsIdentifier || _directiveTokens.Contains(i)) { continue; }

                    string previous = i > 0 ? _tokens[i - 1].Text : string.Empty;
                    if (token.Text == LegacyNameMap.LEGACY_ROOT && previous != ".")
                    {
                        i = RewriteQualifiedName(i);
                        continue;
                    }

                    if (previous == "." || previous == "::" || DeclarationKeywords.Contains(previous)) { continue; }

                    CollectSimpleName(i, imported);
                }
            }

            /// <summary>
            ///     <c>SymphonyFrameWork.X.Y.Name</c> を新しい完全修飾名へ置き換える。
            /// </summary>
            /// <returns> 読み終えた最後の字句の位置。 </returns>
            private int RewriteQualifiedName(int first)
            {
                List<int> identifiers = new() { first };
                int i = first + 1;
                while (i + 1 < _tokens.Count && _tokens[i].Text == "." && _tokens[i + 1].IsIdentifier)
                {
                    identifiers.Add(i + 1);
                    i += 2;
                }

                // 最も長い名前空間から順に、続く識別子が旧型名か調べる。
                for (int cut = identifiers.Count - 1; cut >= 1; cut--)
                {
                    string oldNamespace = string.Join(".", identifiers.Take(cut).Select(index => _tokens[index].Text));
                    string name = _tokens[identifiers[cut]].Text;

                    if (_map.IsForwarder(oldNamespace, name)) { break; }
                    if (!_map.TryGetEntry(oldNamespace, name, out LegacyNameMap.Entry entry)) { continue; }

                    string replacement = $"{entry.NewNamespace}.{entry.NewName}";
                    _edits.Add(new Edit(_tokens[first].Start, _tokens[identifiers[cut]].End, replacement));
                    _replacements.Add($"{oldNamespace}.{name} → {replacement}");
                    break;
                }

                return identifiers[identifiers.Count - 1];
            }

            /// <summary>
            ///     型名だけで書かれた識別子が、取り込んだ旧名前空間の転送か旧型名かを調べる。
            /// </summary>
            private void CollectSimpleName(int index, List<string> imported)
            {
                string name = _tokens[index].Text;
                List<LegacyNameMap.Entry> candidates = new();
                bool isForwarder = false;

                foreach (string oldNamespace in imported)
                {
                    if (_map.IsForwarder(oldNamespace, name))
                    {
                        if (!_usedForwarders.TryGetValue(oldNamespace, out HashSet<string> names))
                        {
                            names = new HashSet<string>(StringComparer.Ordinal);
                            _usedForwarders.Add(oldNamespace, names);
                        }

                        names.Add(name);
                        isForwarder = true;
                    }
                    else if (_map.TryGetEntry(oldNamespace, name, out LegacyNameMap.Entry entry))
                    {
                        candidates.Add(entry);
                    }
                }

                // 転送が見つかった名前は書き換えない。転送を残す前提で旧 using も残す。
                if (isForwarder) { return; }

                List<LegacyNameMap.Entry> targets = candidates
                    .GroupBy(entry => (entry.NewNamespace, entry.NewName))
                    .Select(group => group.First())
                    .ToList();
                if (targets.Count == 1)
                {
                    _simpleUses.Add(new SimpleNameUse(index, targets[0]));
                }
                else if (targets.Count > 1)
                {
                    _unresolved.Add($"{name} は複数の旧名前空間に対応があるため書き換えませんでした。");
                }
            }

            /// <summary>
            ///     取り込んだ旧名前空間から移った拡張メソッドが呼ばれていれば、その新しい名前空間を返す。
            /// </summary>
            private HashSet<string> FindExtensionNamespaces(List<string> imported)
            {
                HashSet<string> memberNames = new(StringComparer.Ordinal);
                for (int i = 1; i < _tokens.Count; i++)
                {
                    if (_tokens[i].IsIdentifier && _tokens[i - 1].Text == ".") { memberNames.Add(_tokens[i].Text); }
                }

                HashSet<string> namespaces = new(StringComparer.Ordinal);
                foreach (string oldNamespace in imported)
                {
                    foreach (LegacyNameMap.ExtensionEntry extension in _map.GetExtensions(oldNamespace))
                    {
                        if (extension.MethodNames.Any(memberNames.Contains)) { namespaces.Add(extension.NewNamespace); }
                    }
                }

                return namespaces;
            }

            private void ReplaceSimpleName(SimpleNameUse use, string replacement)
            {
                Token token = _tokens[use.Token];
                if (replacement != token.Text) { _edits.Add(new Edit(token.Start, token.End, replacement)); }

                _replacements.Add($"{token.Text} → {use.Entry.NewNamespace}.{use.Entry.NewName}");
            }

            /// <summary>
            ///     使わなくなった旧 using を消し、新しい using を足す。
            /// </summary>
            /// <remarks>
            ///     新しい using は、消した最初の旧 using の位置へ書く。消すものが無ければ最後の using の次の行へ、
            ///     using が1つも無ければ本文の先頭へ書く。
            /// </remarks>
            private void EditUsingDirectives(
                List<UsingDirective> allUsings,
                List<UsingDirective> removedUsings,
                List<string> addedUsings)
            {
                string addedLines = string.Concat(addedUsings.Select(name => $"using {name};{_newLine}"));
                bool isAddedWritten = addedUsings.Count == 0;

                foreach (UsingDirective directive in removedUsings)
                {
                    (int start, int end, bool isWholeLine) = GetDirectiveSpan(directive);
                    string replacement = string.Empty;
                    if (!isAddedWritten)
                    {
                        replacement = isWholeLine
                            ? addedLines
                            : string.Join(" ", addedUsings.Select(name => $"using {name};"));
                        isAddedWritten = true;
                    }

                    _edits.Add(new Edit(start, end, replacement));
                }

                if (isAddedWritten) { return; }

                if (allUsings.Count == 0)
                {
                    _edits.Add(new Edit(0, 0, addedLines));
                    return;
                }

                int lastEnd = _tokens[allUsings.Max(directive => directive.LastToken)].End;
                int lineEnd = _source.IndexOf('\n', lastEnd);
                int position = lineEnd < 0 ? _source.Length : lineEnd + 1;
                string prefix = lineEnd < 0 ? _newLine : string.Empty;
                _edits.Add(new Edit(position, position, prefix + addedLines));
            }

            /// <summary>
            ///     using ディレクティブが1行を占めていれば行全体（改行を含む）を、そうでなければディレクティブだけを返す。
            /// </summary>
            private (int Start, int End, bool IsWholeLine) GetDirectiveSpan(UsingDirective directive)
            {
                int start = _tokens[directive.FirstToken].Start;
                int end = _tokens[directive.LastToken].End;
                int lineStart = _source.LastIndexOf('\n', Math.Max(start - 1, 0)) + 1;
                if (start == 0) { lineStart = 0; }

                int lineEnd = _source.IndexOf('\n', end);
                int lineEndInclusive = lineEnd < 0 ? _source.Length : lineEnd + 1;

                bool isWholeLine = string.IsNullOrWhiteSpace(_source.Substring(lineStart, start - lineStart))
                    && string.IsNullOrWhiteSpace(_source.Substring(end, lineEndInclusive - end));
                return isWholeLine ? (lineStart, lineEndInclusive, true) : (start, end, false);
            }

            private string ApplyEdits()
            {
                StringBuilder builder = new(_source);
                foreach (Edit edit in _edits.OrderByDescending(edit => edit.Start).ThenByDescending(edit => edit.End))
                {
                    builder.Remove(edit.Start, edit.End - edit.Start);
                    builder.Insert(edit.Start, edit.Text);
                }

                return builder.ToString();
            }

            private bool IsIdentifier(int index, string text)
            {
                return index >= 0 && index < _tokens.Count && _tokens[index].IsIdentifier && _tokens[index].Text == text;
            }

            /// <summary>
            ///     <c>A.B.C</c> の形の名前を読む。
            /// </summary>
            /// <param name="first"> 最初の識別子の位置。 </param>
            /// <param name="name"> 読んだ名前。 </param>
            /// <param name="next"> 名前の次の字句の位置。 </param>
            private bool TryReadDottedName(int first, out string name, out int next)
            {
                name = null;
                next = first;
                if (first >= _tokens.Count || !_tokens[first].IsIdentifier) { return false; }

                List<string> parts = new() { _tokens[first].Text };
                next = first + 1;
                while (next + 1 < _tokens.Count && _tokens[next].Text == "." && _tokens[next + 1].IsIdentifier)
                {
                    parts.Add(_tokens[next + 1].Text);
                    next += 2;
                }

                name = string.Join(".", parts);
                return true;
            }
        }

        /// <summary>
        ///     本文を字句に分ける。コメント、文字列、文字リテラル、プリプロセッサ行は字句にしない。
        /// </summary>
        /// <param name="source"> 本文。 </param>
        /// <returns> 識別子と記号の字句。 </returns>
        private static List<Token> Tokenize(string source)
        {
            List<Token> tokens = new();
            int length = source.Length;
            int i = 0;
            bool isLineStart = true;

            while (i < length)
            {
                char c = source[i];
                if (c == '\n')
                {
                    isLineStart = true;
                    i++;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (isLineStart && c == '#')
                {
                    i = SkipToLineEnd(source, i);
                    continue;
                }

                isLineStart = false;

                if (c == '/' && i + 1 < length && source[i + 1] == '/')
                {
                    i = SkipToLineEnd(source, i);
                    continue;
                }

                if (c == '/' && i + 1 < length && source[i + 1] == '*')
                {
                    int close = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = close < 0 ? length : close + 2;
                    continue;
                }

                if (TrySkipStringLiteral(source, i, out int stringEnd))
                {
                    i = stringEnd;
                    continue;
                }

                if (c == '\'')
                {
                    i = SkipQuoted(source, i + 1, '\'');
                    continue;
                }

                if (IsIdentifierStart(c) || (c == '@' && i + 1 < length && IsIdentifierStart(source[i + 1])))
                {
                    int start = i;
                    int nameStart = c == '@' ? i + 1 : i;
                    i = nameStart + 1;
                    while (i < length && IsIdentifierPart(source[i])) { i++; }

                    tokens.Add(new Token(true, source.Substring(nameStart, i - nameStart), start, i));
                    continue;
                }

                if (c == ':' && i + 1 < length && source[i + 1] == ':')
                {
                    tokens.Add(new Token(false, "::", i, i + 2));
                    i += 2;
                    continue;
                }

                tokens.Add(new Token(false, c.ToString(), i, i + 1));
                i++;
            }

            return tokens;
        }

        /// <summary>
        ///     文字列リテラル（通常、逐語的、補間、生文字列）を読み飛ばす。
        /// </summary>
        /// <remarks>
        ///     補間の中の式は書き換えの対象にしない。式の中にさらに文字列がある場合は正しく読めないことがあるが、
        ///     通常の文字列は改行で打ち切るため、影響はその行に留まる。
        /// </remarks>
        private static bool TrySkipStringLiteral(string source, int start, out int end)
        {
            end = start;
            int i = start;
            bool isVerbatim = false;
            while (i < source.Length && (source[i] == '$' || source[i] == '@'))
            {
                isVerbatim |= source[i] == '@';
                i++;
            }

            if (i >= source.Length || source[i] != '"') { return false; }

            int quoteCount = 0;
            while (i + quoteCount < source.Length && source[i + quoteCount] == '"') { quoteCount++; }

            if (quoteCount >= 3)
            {
                string delimiter = new('"', quoteCount);
                int close = source.IndexOf(delimiter, i + quoteCount, StringComparison.Ordinal);
                end = close < 0 ? source.Length : close + quoteCount;
                return true;
            }

            if (quoteCount == 2 && !isVerbatim)
            {
                end = i + 2;
                return true;
            }

            if (isVerbatim)
            {
                int j = i + 1;
                while (j < source.Length)
                {
                    if (source[j] == '"')
                    {
                        if (j + 1 < source.Length && source[j + 1] == '"')
                        {
                            j += 2;
                            continue;
                        }

                        break;
                    }

                    j++;
                }

                end = Math.Min(j + 1, source.Length);
                return true;
            }

            end = SkipQuoted(source, i + 1, '"');
            return true;
        }

        /// <summary>
        ///     エスケープを考慮して閉じ記号まで読み飛ばす。改行で打ち切る。
        /// </summary>
        private static int SkipQuoted(string source, int start, char quote)
        {
            int i = start;
            while (i < source.Length && source[i] != quote && source[i] != '\n')
            {
                i += source[i] == '\\' ? 2 : 1;
            }

            return Math.Min(i + 1, source.Length);
        }

        private static int SkipToLineEnd(string source, int start)
        {
            int lineEnd = source.IndexOf('\n', start);
            return lineEnd < 0 ? source.Length : lineEnd;
        }

        private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

        private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';

        #endregion
    }
}
