using System.Text.RegularExpressions;
using UnityEngine;

namespace SymphonyFrameWork
{
    /// <summary>
    ///     文字列のユーティリティクラス。
    /// </summary>
    public static class SymphonyStringUtil
    {
        #region 外部向けAPI

        /// <summary>
        ///     リッチテキストにカラータグを挿入する。
        /// </summary>
        /// <param name="text"> 装飾する文字列。 </param>
        /// <param name="color"> 適用する文字色。 </param>
        /// <returns> colorタグで囲んだ文字列。 </returns>
        public static string AddRichTextColor(this string text, Color color)
        {
            // alphaを含めない既存契約に合わせ、RGBだけをRich Textの色指定へ埋め込む。
            string htmlColor = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{htmlColor}>{text}</color>";
        }

        /// <summary>
        ///     最外層の&lt;color&gt;タグを削除する。
        /// </summary>
        public static string RemoveRichTextColor(this string text)
        {
            // 文字列全体を囲むタグだけを対象とし、内側の入れ子は保持する。
            Regex outerColorRegex = new(@"^<color=[^>]+>(.*)</color>$", RegexOptions.Singleline);
            Match match = outerColorRegex.Match(text);

            // 最外層が一致した場合だけ、キャプチャした内側の文字列へ置き換える。
            if (match.Success) { return match.Groups[1].Value; }

            // 文字列全体がcolorタグで囲まれていない場合は入力を維持する。
            return text;
        }

        /// <summary>
        ///     リッチテキストに太文字タグを挿入する。
        /// </summary>
        /// <param name="text"> 装飾する文字列。 </param>
        /// <returns> bタグで囲んだ文字列。 </returns>
        public static string AddRichTextBold(this string text) => $"<b>{text}</b>";

        /// <summary>
        ///     最外層の&lt;b&gt;タグを削除する。
        /// </summary>
        /// <param name="text"> 最外層のbタグを除去する文字列。 </param>
        /// <returns> 最外層のbタグを除去した文字列。 </returns>
        public static string RemoveRichTextBold(this string text)
        {
            // 文字列全体を囲むタグだけを対象とし、内側の入れ子は保持する。
            Regex outerBoldRegex = new(@"^<b>(.*)</b>$", RegexOptions.Singleline);
            Match match = outerBoldRegex.Match(text);

            // 最外層が一致した場合だけ、キャプチャした内側の文字列へ置き換える。
            if (match.Success) { return match.Groups[1].Value; }

            // 文字列全体がbタグで囲まれていない場合は入力を維持する。
            return text;
        }

        /// <summary>
        ///     リッチテキストに下線タグを挿入する。
        /// </summary>
        /// <param name="text"> 装飾する文字列。 </param>
        /// <returns> uタグで囲んだ文字列。 </returns>
        public static string AddRichTextUnderline(this string text) => $"<u>{text}</u>";

        /// <summary>
        ///     最外層の&lt;u&gt;タグを削除する。
        /// </summary>
        /// <param name="text"> 最外層のuタグを除去する文字列。 </param>
        /// <returns> 最外層のuタグを除去した文字列。 </returns>
        public static string RemoveRichTextUnderline(this string text)
        {
            // 文字列全体を囲むタグだけを対象とし、内側の入れ子は保持する。
            Regex outerUnderlineRegex = new(@"^<u>(.*)</u>$", RegexOptions.Singleline);
            Match match = outerUnderlineRegex.Match(text);

            // 最外層が一致した場合だけ、キャプチャした内側の文字列へ置き換える。
            if (match.Success) { return match.Groups[1].Value; }

            // 文字列全体がuタグで囲まれていない場合は入力を維持する。
            return text;
        }

        #endregion
    }
}
