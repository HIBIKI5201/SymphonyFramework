using SymphonyFrameWork.Core;
using UnityEditor;
using UnityEngine;
using SymphonyFrameWork.Config;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     AudioConfigとオーディオグループenumの再生成操作を描画する。
    /// </summary>
    [CustomEditor(typeof(AudioConfig))]
    public sealed class AudioConfigDrawer : UnityEditor.Editor
    {
        #region 外部向けAPI

        /// <summary>
        ///     AudioConfigのInspectorを描画する。
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // AudioMixerGroupの構成変更を利用者が明示的にenumへ反映できるようにする。
            if (GUILayout.Button($"{EditorSymphonyConstant.AudioGroupTypeEnumName}Enumを再生成"))
            {
                AutoEnumGenerator.AudioEnumGenerate();
            }
        }

        #endregion
    }
}
