using SymphonyFrameWork.Attribute;
using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    [CustomPropertyDrawer(typeof(TagSelectorAttribute))]
    public class TagSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // string型のみ対応。
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "TagSelectorはstring型にのみ使用できます。");
                return;
            }

            // 現在の値のインデックスを探す。
            int index = Array.IndexOf(Tags, property.stringValue);
            if (index < 0) index = 0; // 見つからない場合は最初のタグを選択。

            // プルダウンを表示。
            int selectedIndex = EditorGUI.Popup(position, label.text, index, Tags);

            // 選択されたタグを文字列として保存。
            property.stringValue = Tags[selectedIndex];
        }

        private string[] _tags;

        private string[] Tags
        {
            get
            {
                if (_tags == null || _tags.Length != InternalEditorUtility.tags.Length)
                {
                    _tags = InternalEditorUtility.tags;
                }
                return _tags;
            }
        }
    }
}
