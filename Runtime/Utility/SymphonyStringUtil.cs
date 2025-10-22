using UnityEngine;

namespace SymphonyFrameWork
{
    /// <summary>
    ///     文字列のユーティリティクラス。
    /// </summary>
    public static class SymphonyStringUtil
    {
        /// <summary>
        ///     リッチテキストの文字にカラータグを挿入する。
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string AddRichTextColor(this string text, Color color)
        {
            // ColorをRGBに変換する。
            string htmlColor = ColorUtility.ToHtmlStringRGB(color);
            return $"<color={htmlColor}>{text}</color>"; // タグに入れて返す。
        }
    }
}