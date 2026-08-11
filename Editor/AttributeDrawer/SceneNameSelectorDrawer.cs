using SymphonyFrameWork.Attribute;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     stringフィールドをBuild Settingsのシーン名選択欄として描画する。
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneNameSelectorAttribute))]
    public sealed class SceneNameSelectorDrawer : PropertyDrawer
    {
        #region 外部向けAPI

        /// <summary>
        ///     Build Settingsのシーン名を選択するポップアップを描画する。
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 属性の契約外であるstring以外のフィールドでは、値を変更せずエラーを表示する。
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "SceneNameSelectorはstring型にのみ使用できます。");
                return;
            }

            // 選択肢が無い場合はPopupの範囲外参照を避け、Build Settingsの不足を表示する。
            if (SceneList.Length == 0)
            {
                EditorGUI.LabelField(position, label.text, "ビルド設定にシーンが追加されていません。");
                return;
            }

            // 保存済みの値がBuild Settingsから外れている場合は、先頭のシーンを既定値とする。
            int index = Array.IndexOf(SceneList, property.stringValue);
            if (index < 0) { index = 0; }

            // Popupの選択結果をシリアライズ対象のシーン名へ反映する。
            int selectedIndex = EditorGUI.Popup(position, label.text, index, SceneList);
            property.stringValue = SceneList[selectedIndex];
        }

        #endregion

        #region 内部処理

        private string[] _sceneList;

        private string[] SceneList
        {
            get
            {
                // 更新判定は登録数だけに限定し、同数のまま名前を変えた場合はDrawer再生成までキャッシュを維持する。
                if (_sceneList == null || _sceneList.Length != EditorBuildSettings.scenes.Length)
                {
                    _sceneList = EditorBuildSettings.scenes
                        .Select(s => Path.GetFileNameWithoutExtension(s.path))
                        .ToArray();
                }

                return _sceneList;
            }
        }

        #endregion
    }
}
