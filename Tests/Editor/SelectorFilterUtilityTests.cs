using System;

using NUnit.Framework;

using SymphonyFrameWork.Editor;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     セレクター属性のフィルターメソッド解決と候補の絞り込みを検証する。
    /// </summary>
    public sealed class SelectorFilterUtilityTests
    {
        /// <summary>
        ///     フィルター名が無い場合は全候補を残す。
        /// </summary>
        [Test]
        public void TryCreateMask_NoFilterName_KeepsAllCandidates()
        {
            string[] candidates = { "Title", "UI_Menu" };

            bool nullResult = SelectorFilterUtility.TryCreateMask(
                candidates, null, new FilterHost(), out bool[] nullMask, out string nullError);
            bool emptyResult = SelectorFilterUtility.TryCreateMask(
                candidates, string.Empty, new FilterHost(), out bool[] emptyMask, out string emptyError);

            Assert.That(nullResult, Is.True);
            Assert.That(nullMask, Is.EqualTo(new[] { true, true }));
            Assert.That(nullError, Is.Null);
            Assert.That(emptyResult, Is.True);
            Assert.That(emptyMask, Is.EqualTo(new[] { true, true }));
            Assert.That(emptyError, Is.Null);
        }

        /// <summary>
        ///     staticなprivateメソッドを名前で解決して候補を絞る。
        /// </summary>
        [Test]
        public void TryCreateMask_StaticFilter_KeepsMatchingCandidates()
        {
            string[] candidates = { "UI_Menu", "Title", "UI_Result" };

            bool result = SelectorFilterUtility.TryCreateMask(
                candidates, "KeepUiPrefixed", new FilterHost(), out bool[] mask, out string errorMessage);

            Assert.That(result, Is.True);
            Assert.That(mask, Is.EqualTo(new[] { true, false, true }));
            Assert.That(errorMessage, Is.Null);
        }

        /// <summary>
        ///     instanceメソッドは対象の状態を反映して判定する。
        /// </summary>
        [Test]
        public void TryCreateMask_InstanceFilter_UsesTargetState()
        {
            string[] candidates = { "UI_Menu", "Battle_Boss" };
            FilterHost host = new() { Prefix = "Battle_" };

            bool result = SelectorFilterUtility.TryCreateMask(
                candidates, "KeepConfiguredPrefix", host, out bool[] mask, out string errorMessage);

            Assert.That(result, Is.True);
            Assert.That(mask, Is.EqualTo(new[] { false, true }));
            Assert.That(errorMessage, Is.Null);
        }

        /// <summary>
        ///     基底型が宣言するprivateメソッドも解決できる。
        /// </summary>
        [Test]
        public void TryCreateMask_BaseTypeFilter_IsFound()
        {
            string[] candidates = { "UI_Menu", "Title" };

            bool result = SelectorFilterUtility.TryCreateMask(
                candidates, "KeepUiPrefixed", new DerivedFilterHost(), out bool[] mask, out string errorMessage);

            Assert.That(result, Is.True);
            Assert.That(mask, Is.EqualTo(new[] { true, false }));
            Assert.That(errorMessage, Is.Null);
        }

        /// <summary>
        ///     nullの候補はフィルターへ渡さず常に残す。
        /// </summary>
        [Test]
        public void TryCreateMask_NullCandidate_IsKeptWithoutInvoking()
        {
            Type[] candidates = { null, typeof(int), typeof(string) };
            FilterHost host = new();

            bool result = SelectorFilterUtility.TryCreateMask(
                candidates, "KeepValueTypes", host, out bool[] mask, out string errorMessage);

            Assert.That(result, Is.True);
            Assert.That(mask, Is.EqualTo(new[] { true, true, false }));
            Assert.That(errorMessage, Is.Null);

            // nullを渡していれば呼び出し回数は3件になるため、件数で迂回を確認する。
            Assert.That(host.TypeFilterCallCount, Is.EqualTo(2));
        }

        /// <summary>
        ///     候補の型に合う引数を持つメソッドだけを採用する。
        /// </summary>
        [Test]
        public void TryCreateMask_TypeCandidates_MatchesTypeParameter()
        {
            Type[] candidates = { typeof(int), typeof(string) };

            bool typeResult = SelectorFilterUtility.TryCreateMask(
                candidates, "KeepValueTypes", new FilterHost(), out bool[] typeMask, out string typeError);
            bool stringResult = SelectorFilterUtility.TryCreateMask(
                new[] { "UI_Menu" }, "KeepValueTypes", new FilterHost(), out bool[] stringMask, out string stringError);

            Assert.That(typeResult, Is.True);
            Assert.That(typeMask, Is.EqualTo(new[] { true, false }));
            Assert.That(typeError, Is.Null);

            // string候補にはbool(Type)のメソッドを使えないため、見つからない扱いにする。
            Assert.That(stringResult, Is.False);
            Assert.That(stringMask, Is.Null);
            Assert.That(stringError, Does.Contain("KeepValueTypes"));
        }

        /// <summary>
        ///     存在しないメソッド名は期待するシグネチャを添えて失敗させる。
        /// </summary>
        [Test]
        public void TryCreateMask_MissingMethod_ReturnsFalseWithMessage()
        {
            bool result = SelectorFilterUtility.TryCreateMask(
                new[] { "Title" }, "NotDeclared", new FilterHost(), out bool[] mask, out string errorMessage);

            Assert.That(result, Is.False);
            Assert.That(mask, Is.Null);
            Assert.That(errorMessage, Does.Contain("NotDeclared"));
            Assert.That(errorMessage, Does.Contain("bool NotDeclared(string)"));
            Assert.That(errorMessage, Does.Contain(nameof(FilterHost)));
        }

        /// <summary>
        ///     boolを返さない同名メソッドは採用しない。
        /// </summary>
        [Test]
        public void TryCreateMask_WrongSignature_ReturnsFalseWithMessage()
        {
            bool result = SelectorFilterUtility.TryCreateMask(
                new[] { "Title" }, "ReturnsNothing", new FilterHost(), out bool[] mask, out string errorMessage);

            Assert.That(result, Is.False);
            Assert.That(mask, Is.Null);
            Assert.That(errorMessage, Does.Contain("ReturnsNothing"));
        }

        /// <summary>
        ///     対象が無い場合は絞り込みを行わず失敗させる。
        /// </summary>
        [Test]
        public void TryCreateMask_NullTarget_ReturnsFalseWithMessage()
        {
            bool result = SelectorFilterUtility.TryCreateMask(
                new[] { "Title" }, "KeepUiPrefixed", null, out bool[] mask, out string errorMessage);

            Assert.That(result, Is.False);
            Assert.That(mask, Is.Null);
            Assert.That(errorMessage, Is.Not.Null.And.Not.Empty);
        }

        /// <summary>
        ///     フィルターの例外は描画を止めずメッセージへ変換する。
        /// </summary>
        [Test]
        public void TryCreateMask_FilterThrows_ReturnsFalseWithMessage()
        {
            bool result = SelectorFilterUtility.TryCreateMask(
                new[] { "Title" }, "ThrowsAlways", new FilterHost(), out bool[] mask, out string errorMessage);

            Assert.That(result, Is.False);
            Assert.That(mask, Is.Null);
            Assert.That(errorMessage, Does.Contain(FilterHost.THROWN_MESSAGE));
        }

        /// <summary>
        ///     マスクがtrueの要素だけを順序を保って残す。
        /// </summary>
        [Test]
        public void ApplyMask_KeepsOnlyMaskedElements()
        {
            string[] source = { "A", "B", "C" };

            string[] filtered = SelectorFilterUtility.ApplyMask(source, new[] { true, false, true });

            Assert.That(filtered, Is.EqualTo(new[] { "A", "C" }));
        }

        /// <summary>
        ///     長さの違うマスクでは絞り込まず元の配列を返す。
        /// </summary>
        [Test]
        public void ApplyMask_LengthMismatch_ReturnsSource()
        {
            string[] source = { "A", "B", "C" };

            Assert.That(SelectorFilterUtility.ApplyMask(source, new[] { true }), Is.SameAs(source));
            Assert.That(SelectorFilterUtility.ApplyMask(source, null), Is.SameAs(source));
        }

        /// <summary> フィルターメソッドの宣言先となる検証用の型。 </summary>
        private class FilterHost
        {
            /// <summary> 例外を投げるフィルターが使うメッセージ。 </summary>
            internal const string THROWN_MESSAGE = "フィルターの検証用例外";

            /// <summary> instanceフィルターが判定に使う接頭辞。 </summary>
            internal string Prefix = "UI_";

            /// <summary> Type候補のフィルターが呼ばれた回数。 </summary>
            internal int TypeFilterCallCount;

            /// <summary>
            ///     UI_で始まる候補だけを残す。
            /// </summary>
            /// <param name="candidate"> 判定する候補。 </param>
            /// <returns> 残す場合はtrue。 </returns>
            private static bool KeepUiPrefixed(string candidate)
                => candidate.StartsWith("UI_", StringComparison.Ordinal);

            /// <summary>
            ///     boolを返さない同名メソッドとして、採用されないことを確認する。
            /// </summary>
            /// <param name="candidate"> 判定する候補。 </param>
            private static void ReturnsNothing(string candidate)
            {
            }

            /// <summary>
            ///     常に例外を投げる。
            /// </summary>
            /// <param name="candidate"> 判定する候補。 </param>
            /// <returns> 返さない。 </returns>
            private static bool ThrowsAlways(string candidate)
                => throw new InvalidOperationException(THROWN_MESSAGE);

            /// <summary>
            ///     対象の状態が示す接頭辞で始まる候補だけを残す。
            /// </summary>
            /// <param name="candidate"> 判定する候補。 </param>
            /// <returns> 残す場合はtrue。 </returns>
            private bool KeepConfiguredPrefix(string candidate)
                => candidate.StartsWith(Prefix, StringComparison.Ordinal);

            /// <summary>
            ///     値型の候補だけを残す。
            /// </summary>
            /// <param name="candidate"> 判定する候補。 </param>
            /// <returns> 残す場合はtrue。 </returns>
            private bool KeepValueTypes(Type candidate)
            {
                TypeFilterCallCount++;

                return candidate.IsValueType;
            }
        }

        /// <summary> 基底型のprivateメソッドを解決できるか確認する派生型。 </summary>
        private sealed class DerivedFilterHost : FilterHost
        {
        }
    }
}
