using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Attribute
{
    /// <summary>
    ///     プロパティを変更不可の状態で描画する。
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    // 6.x の名前（SymphonyFrameWork.Attribute）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Attribute",
        sourceAssembly: "SymphonyFrameWork.Editor")]
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
