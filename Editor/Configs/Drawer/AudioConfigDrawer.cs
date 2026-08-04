using SymphonyFrameWork.Core;
using UnityEditor;
using UnityEngine;
using SymphonyFrameWork.Config;

namespace SymphonyFrameWork.Editor
{
    /// <summary> AudioConfigとオーディオグループenumの再生成操作を描画する。 </summary>
    [CustomEditor(typeof(AudioConfig))]
    public sealed class AudioConfigDrawer : UnityEditor.Editor
    {
        /// <summary>
        /// InspectorのGUIを上書きします。
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // AudioGroupTypeEnumを再生成するボタン。
            if (GUILayout.Button($"{EditorSymphonyConstant.AudioGroupTypeEnumName}Enumを再生成"))
            {
                AutoEnumGenerator.AudioEnumGenerate();
            }
        }

    }
}
