using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Attribute
{
    /// <summary>
    ///     文字列をInspectorへ描画する。
    /// </summary>
    [CustomPropertyDrawer(typeof(DisplayTextAttribute))]
    public sealed class DisplayTextDecoratorDrawer : DecoratorDrawer
    {
        #region 外部向けAPI

        /// <summary>
        ///     表示文字列の行数に応じたDecorator領域の高さを返す。
        /// </summary>
        public override float GetHeight()
        {
            // 改行を含む文字列が他のInspector項目へ重ならないよう、行数分の高さを確保する。
            return Style.lineHeight * DisplayTextAttribute.Text.Split('\n').Length;
        }

        /// <summary>
        ///     属性に設定された文字列を中央揃えで描画する。
        /// </summary>
        public override void OnGUI(Rect position)
        {
            EditorGUI.LabelField(position, DisplayTextAttribute.Text, Style);
        }

        #endregion

        #region 内部処理

        private readonly GUIStyle Style = new(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Normal,
            fontSize = 12
        };

        private DisplayTextAttribute DisplayTextAttribute => (DisplayTextAttribute)attribute;

        #endregion
    }
}
