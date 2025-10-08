using SymphonyFrameWork.Core;
using UnityEditor;
using UnityEngine;
using SymphonyFrameWork.Config;

namespace SymphonyFrameWork.Editor
{
    [CustomEditor(typeof(AudioManagerConfig))]
    public class AudioManagerConfigDrawer : UnityEditor.Editor
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
