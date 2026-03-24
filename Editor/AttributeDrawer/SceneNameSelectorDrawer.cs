using SymphonyFrameWork.Attribute;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    [CustomPropertyDrawer(typeof(SceneNameSelectorAttribute))]
    public class SceneNameSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // string型のみ対応。
            if (property.propertyType == SerializedPropertyType.String)
            {
                int index = Array.IndexOf(SceneList, property.stringValue);
                if (index < 0) index = 0;

                int selectedIndex = EditorGUI.Popup(position, label.text, index, SceneList);

                property.stringValue = SceneList[selectedIndex];
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "SceneNameSelectorはstring型にのみ使用できます。");
            }
        }

        private string[] _sceneList;

        private string[] SceneList
        {
            get
            {
                if (_sceneList == null || _sceneList.Length != EditorBuildSettings.scenes.Length)
                {
                    _sceneList = EditorBuildSettings.scenes
                        .Select(s => Path.GetFileNameWithoutExtension(s.path))
                        .ToArray();
                }
                return _sceneList;
            }
        }
    }
}
