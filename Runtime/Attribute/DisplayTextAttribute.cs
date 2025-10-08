using UnityEngine;

namespace SymphonyFrameWork.Attribute
{
    /// <summary>
    ///     インスペクターに文字を表示する。
    /// </summary>
    public class DisplayTextAttribute : PropertyAttribute
    {
        public DisplayTextAttribute(string text)
        {
            _text = text;
        }

        public string Text => _text;

        private readonly string _text;
    }
}