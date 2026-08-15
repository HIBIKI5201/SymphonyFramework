using System.Collections.Generic;
using System.Text.RegularExpressions;

using SymphonyFrameWork.Editor;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary> EnumGeneratorが、候補文字列をそのままコンパイルできる列挙子名へ解決する契約を検証する。 </summary>
    public sealed class EnumGeneratorMemberNameTests
    {
        /// <summary> 常に先頭へNoneを置く。 </summary>
        [Test]
        public void ResolveEnumMemberNames_AnyCandidates_StartsWithNone()
        {
            IReadOnlyList<string> members = EnumGenerator.ResolveEnumMemberNames(new[] { "Title", "Game" });

            Assert.That(members, Is.EqualTo(new[] { "None", "Title", "Game" }));
        }

        /// <summary> 候補が無くてもNoneだけを返す。 </summary>
        [Test]
        public void ResolveEnumMemberNames_EmptyCandidates_ReturnsOnlyNone()
        {
            Assert.That(EnumGenerator.ResolveEnumMemberNames(new string[0]), Is.EqualTo(new[] { "None" }));
        }

        /// <summary> 予約語は除外せず、@を前置した識別子として残す。 </summary>
        [Test]
        public void ResolveEnumMemberNames_ReservedWord_EscapesWithAtSign()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"'class'はC#の予約語のため、'@class'として生成します"));

            IReadOnlyList<string> members = EnumGenerator.ResolveEnumMemberNames(new[] { "class" });

            Assert.That(members, Is.EqualTo(new[] { "None", "@class" }));
        }

        /// <summary> 予約語でない候補は警告を出さずそのまま残す。 </summary>
        [Test]
        public void ResolveEnumMemberNames_ContextualKeyword_IsKeptAsIs()
        {
            IReadOnlyList<string> members = EnumGenerator.ResolveEnumMemberNames(new[] { "var", "value", "record" });

            Assert.That(members, Is.EqualTo(new[] { "None", "var", "value", "record" }));

            LogAssert.NoUnexpectedReceived();
        }

        /// <summary> 既に@付きの候補は、二重に前置せず1つの列挙子へ解決する。 </summary>
        [Test]
        public void ResolveEnumMemberNames_AlreadyEscapedReservedWord_DoesNotDoubleEscape()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"'int'はC#の予約語のため"));

            IReadOnlyList<string> members = EnumGenerator.ResolveEnumMemberNames(new[] { "@int" });

            Assert.That(members, Is.EqualTo(new[] { "None", "@int" }));
        }

        /// <summary> エスケープ後に同じ名前になる候補は、重複した列挙子を作らない。 </summary>
        [Test]
        public void ResolveEnumMemberNames_ReservedWordWithAndWithoutAtSign_ProducesSingleMember()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"'base'はC#の予約語のため"));
            LogAssert.Expect(LogType.Warning, new Regex(@"'base'はC#の予約語のため"));

            IReadOnlyList<string> members = EnumGenerator.ResolveEnumMemberNames(new[] { "base", "@base" });

            Assert.That(members, Is.EqualTo(new[] { "None", "@base" }));
        }

        /// <summary> 識別子として使えない候補は除外する。 </summary>
        [Test]
        public void ResolveEnumMemberNames_InvalidIdentifier_IsExcluded()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"'1st Scene'を除外しました"));

            IReadOnlyList<string> members = EnumGenerator.ResolveEnumMemberNames(new[] { "1st Scene", "Title" });

            Assert.That(members, Is.EqualTo(new[] { "None", "Title" }));
        }

        /// <summary> nullと空文字は識別子にできないため除外する。 </summary>
        [Test]
        public void ResolveEnumMemberNames_NullOrEmpty_IsExcluded()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"''を除外しました"));
            LogAssert.Expect(LogType.Warning, new Regex(@"''を除外しました"));

            IReadOnlyList<string> members = EnumGenerator.ResolveEnumMemberNames(new[] { null, string.Empty, "Title" });

            Assert.That(members, Is.EqualTo(new[] { "None", "Title" }));
        }

        /// <summary> enumが値の格納に使うvalue__は、@を付けても列挙子にできないため除外する。 </summary>
        [Test]
        public void ResolveEnumMemberNames_ReservedEnumMemberName_IsExcluded()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"'value__'はenumの列挙子名として予約されているため除外しました"));

            IReadOnlyList<string> members = EnumGenerator.ResolveEnumMemberNames(new[] { "value__", "Title" });

            Assert.That(members, Is.EqualTo(new[] { "None", "Title" }));
        }

        /// <summary> 同名の候補は1つの列挙子にまとめ、最初の並び順を保つ。 </summary>
        [Test]
        public void ResolveEnumMemberNames_DuplicateCandidates_KeepsFirstOrder()
        {
            IReadOnlyList<string> members =
                EnumGenerator.ResolveEnumMemberNames(new[] { "Title", "Game", "Title", "None" });

            Assert.That(members, Is.EqualTo(new[] { "None", "Title", "Game" }));
        }
    }
}
