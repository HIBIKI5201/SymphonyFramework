using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Attribute
{
    /// <summary>
    ///     プロパティを変更不可の状態で描画する。
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public sealed class ReadOnlyDrawer : PropertyDrawer
    {
        #region 外部向けAPI

        /// <summary>
        ///     対象プロパティを無効化した状態で描画する。
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true)) { EditorGUI.PropertyField(position, property, label, true); }
        }

        #endregion
    }
}
