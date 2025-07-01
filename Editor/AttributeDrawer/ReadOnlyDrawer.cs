using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Attribute
{
    /// <summary>
    ///     プロパティを変更不可にする
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    /// <summary>
    ///     ReadOnryが表示されない場合に警告を出す
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class ReadOnlyInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var targetType = target.GetType();
            var fields = targetType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
                if (field.IsDefined(typeof(ReadOnlyAttribute), true))
                {
                    var property = serializedObject.FindProperty(field.Name);
                    if (property == null)
                        Debug.LogWarning(
                            $"フィールド '{field.Name}' は [ReadOnly] 属性が付与されていますが、[SerializeField] 属性が付与されていないため、インスペクターに表示されません。");
                }

            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
}