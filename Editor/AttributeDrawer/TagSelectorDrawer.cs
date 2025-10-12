using SymphonyFrameWork.Attribute;
using System;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    [CustomPropertyDrawer(typeof(TagSelectorAttribute))]
    public class TagSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // string型のみ対応
            if (property.propertyType == SerializedPropertyType.String)
            {
                // Unityの全タグを取得
                string[] tags = UnityEditorInternal.InternalEditorUtility.tags;

                // 現在の値のインデックスを探す
                int index = Array.IndexOf(tags, property.stringValue);
                if (index < 0) index = 0;

                // プルダウンを表示
                int selectedIndex = EditorGUI.Popup(position, label.text, index, tags);

                // 選択されたタグを文字列として保存
                property.stringValue = tags[selectedIndex];
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "TagSelectorはstring型にのみ使用できます。");
            }
        }
    }
}
