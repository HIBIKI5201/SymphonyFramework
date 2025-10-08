using SymphonyFrameWork.Core;
using UnityEditor;
using UnityEngine;
using SymphonyFrameWork.Config;

namespace SymphonyFrameWork.Editor
{
    [CustomEditor(typeof(AudioManagerConfig))]
    public class AudioManagerConfigDrawer : UnityEditor.Editor
    {
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
