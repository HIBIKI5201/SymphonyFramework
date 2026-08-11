using SymphonyFrameWork.Attribute;
using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     stringフィールドをプロジェクトのタグ選択欄として描画する。
    /// </summary>
    [CustomPropertyDrawer(typeof(TagSelectorAttribute))]
    public sealed class TagSelectorDrawer : PropertyDrawer
    {
        #region 外部向けAPI

        /// <summary>
        ///     登録済みタグを選択するポップアップを描画する。
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 属性の契約外であるstring以外のフィールドでは、値を変更せずエラーを表示する。
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "TagSelectorはstring型にのみ使用できます。");
                return;
            }

            // 保存済みの値がタグ一覧から外れている場合は、先頭のタグを既定値とする。
            int index = Array.IndexOf(Tags, property.stringValue);
            if (index < 0) { index = 0; }

            // Popupの選択結果をシリアライズ対象のタグ名へ反映する。
            int selectedIndex = EditorGUI.Popup(position, label.text, index, Tags);
            property.stringValue = Tags[selectedIndex];
        }

        #endregion

        #region 内部処理

        private string[] _tags;

        private string[] Tags
        {
            get
            {
                // 更新判定は登録数だけに限定し、同数のまま名前を変えた場合はDrawer再生成までキャッシュを維持する。
                if (_tags == null || _tags.Length != InternalEditorUtility.tags.Length)
                {
                    _tags = InternalEditorUtility.tags;
                }

                return _tags;
            }
        }

        #endregion
    }
}
