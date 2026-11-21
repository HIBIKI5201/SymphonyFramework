using SymphonyFramework.Core;
using UnityEditor;
using UnityEngine;
using SymphonyFramework.Config;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     AudioConfigとオーディオグループenumの再生成操作を描画する。
    /// </summary>
    [CustomEditor(typeof(AudioConfig))]
    // 6.x の名前（SymphonyFrameWork.Editor）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Editor",
        sourceAssembly: "SymphonyFrameWork.Editor")]
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
