using UnityEngine;

namespace SymphonyFrameWork
{
    /// <summary>
    ///     文字列のユーティリティクラス。
    /// </summary>
    public static class SymphonyStringUtil
    {
        /// <summary>
        ///     リッチテキストにカラータグを挿入する。
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string AddRichTextColor(this string text, Color color)
        {
            // ColorをRGBに変換する。
            string htmlColor = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{htmlColor}>{text}</color>"; // タグに入れて返す。
        }

        /// <summary>
        ///     リッチテキストに太文字タグを挿入する。
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string AddRichTextBold(this string text) => $"<b>{text}</b>";

        /// <summary>
        ///     リッチテキストに下線タグを挿入する。
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string AddRichTextUnderline(this string text) => $"<u>{text}</u>";
    }
}