using UnityEngine;

namespace SymphonyFrameWork.Attribute
{
    /// <summary>
    ///     インスペクターに文字を表示する
    /// </summary>
    public class DisplayTextAttribute : PropertyAttribute
    {
        public DisplayTextAttribute(string text)
        {
            Text = text;
        }

        public string Text { get; private set; }
    }
}