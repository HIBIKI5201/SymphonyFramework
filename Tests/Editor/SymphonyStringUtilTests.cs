using NUnit.Framework;

using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary> SymphonyStringUtilがリッチテキストタグを最外層だけ着脱する契約を検証する。 </summary>
    public sealed class SymphonyStringUtilTests
    {
        /// <summary> 色はalphaを含めないRGB6桁で埋め込む。 </summary>
        [Test]
        public void AddRichTextColor_Color_EmbedsRgbWithoutAlpha()
        {
            Assert.That("文字".AddRichTextColor(Color.red), Is.EqualTo("<color=#FF0000>文字</color>"));
        }

        /// <summary> alphaを指定しても出力へ含めない。 </summary>
        [Test]
        public void AddRichTextColor_TransparentColor_IgnoresAlpha()
        {
            Assert.That(
                "文字".AddRichTextColor(new Color(0f, 1f, 0f, 0.5f)),
                Is.EqualTo("<color=#00FF00>文字</color>"));
        }

        /// <summary> 最外層のcolorタグだけを外す。 </summary>
        [Test]
        public void RemoveRichTextColor_WrappedText_RemovesOuterTagOnly()
        {
            Assert.That(
                "<color=#FF0000>外<color=#00FF00>内</color></color>".RemoveRichTextColor(),
                Is.EqualTo("外<color=#00FF00>内</color>"));
        }

        /// <summary> 全体を囲んでいないタグは外さない。 </summary>
        [Test]
        public void RemoveRichTextColor_PartiallyTaggedText_KeepsInput()
        {
            const string text = "先頭<color=#FF0000>途中</color>末尾";

            Assert.That(text.RemoveRichTextColor(), Is.EqualTo(text));
        }

        /// <summary> タグが無ければそのまま返す。 </summary>
        [Test]
        public void RemoveRichTextColor_NoTag_KeepsInput()
        {
            Assert.That("素の文字".RemoveRichTextColor(), Is.EqualTo("素の文字"));
        }

        /// <summary> 改行をまたぐ場合も最外層として扱う。 </summary>
        [Test]
        public void RemoveRichTextColor_MultilineText_RemovesOuterTag()
        {
            Assert.That(
                "<color=#FF0000>1行目\n2行目</color>".RemoveRichTextColor(),
                Is.EqualTo("1行目\n2行目"));
        }

        /// <summary> 付けてから外すと元に戻る。 </summary>
        [Test]
        public void AddAndRemoveRichTextColor_RoundTrip_RestoresOriginal()
        {
            Assert.That("往復".AddRichTextColor(Color.blue).RemoveRichTextColor(), Is.EqualTo("往復"));
        }

        /// <summary> 太字タグで囲む。 </summary>
        [Test]
        public void AddRichTextBold_Text_WrapsWithBoldTag()
        {
            Assert.That("強調".AddRichTextBold(), Is.EqualTo("<b>強調</b>"));
        }

        /// <summary> 最外層の太字タグだけを外す。 </summary>
        [Test]
        public void RemoveRichTextBold_WrappedText_RemovesOuterTagOnly()
        {
            Assert.That("<b>外<b>内</b></b>".RemoveRichTextBold(), Is.EqualTo("外<b>内</b>"));
        }

        /// <summary> 全体を囲んでいない太字タグは外さない。 </summary>
        [Test]
        public void RemoveRichTextBold_PartiallyTaggedText_KeepsInput()
        {
            Assert.That("先頭<b>途中</b>末尾".RemoveRichTextBold(), Is.EqualTo("先頭<b>途中</b>末尾"));
        }

        /// <summary> 下線タグで囲む。 </summary>
        [Test]
        public void AddRichTextUnderline_Text_WrapsWithUnderlineTag()
        {
            Assert.That("下線".AddRichTextUnderline(), Is.EqualTo("<u>下線</u>"));
        }

        /// <summary> 最外層の下線タグだけを外す。 </summary>
        [Test]
        public void RemoveRichTextUnderline_WrappedText_RemovesOuterTagOnly()
        {
            Assert.That("<u>外<u>内</u></u>".RemoveRichTextUnderline(), Is.EqualTo("外<u>内</u>"));
        }

        /// <summary> 種類の違うタグは外さない。 </summary>
        [Test]
        public void RemoveRichTextUnderline_BoldTag_KeepsInput()
        {
            Assert.That("<b>太字</b>".RemoveRichTextUnderline(), Is.EqualTo("<b>太字</b>"));
        }

        /// <summary> 空文字でもタグを付けられる。 </summary>
        [Test]
        public void AddRichTextBold_EmptyText_WrapsEmptyContent()
        {
            Assert.That(string.Empty.AddRichTextBold(), Is.EqualTo("<b></b>"));
        }

        /// <summary> 空のタグは中身の空文字へ戻る。 </summary>
        [Test]
        public void RemoveRichTextBold_EmptyContent_ReturnsEmpty()
        {
            Assert.That("<b></b>".RemoveRichTextBold(), Is.EqualTo(string.Empty));
        }
    }
}
