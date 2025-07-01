using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Attribute
{
    /// <summary>
    ///     文字の描画を行う
    /// </summary>
    [CustomPropertyDrawer(typeof(DisplayTextAttribute))]
    public class DisplayTextDecoratorDrawer : DecoratorDrawer
    {
        private readonly GUIStyle Style = new(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Normal,
            fontSize = 12
        };

        private DisplayTextAttribute DisplayTextAttribute => (DisplayTextAttribute)attribute;

        public override float GetHeight()
        {
            //改行の数だけ高くする
            return Style.lineHeight * DisplayTextAttribute.Text.Split('\n').Length;
        }

        public override void OnGUI(Rect position)
        {
            // 指定した領域にテキストを表示する
            EditorGUI.LabelField(position, DisplayTextAttribute.Text, Style);
        }
    }
}