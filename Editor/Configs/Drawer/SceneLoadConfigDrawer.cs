using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SymphonyFrameWork.Config;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     SceneLoadConfigのシーン一覧をBuild Settingsから選択可能にする。
    /// </summary>
    [CustomEditor(typeof(SceneLoadConfig))]
    public sealed class SceneLoadConfigDrawer: UnityEditor.Editor
    {
        #region 外部向けAPI

        /// <summary>
        ///     起動時シーン設定の専用Inspectorを描画する。
        /// </summary>
        public override void OnInspectorGUI()
        {
            // SerializedProperty経由の変更を正しく追跡するため、描画前に最新の値を同期する。
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            SerializedProperty isResetAndLoadOnPlay = serializedObject.FindProperty("_isResetAndLoadOnPlay");
            SerializedProperty initializeSceneList = serializedObject.FindProperty("_initializeSceneList");

            // 設定側の表示名とTooltipを維持したまま、Play開始時の挙動を編集する。
            GUIContent isResetAndLoadOnPlayLabel = new(isResetAndLoadOnPlay.displayName, isResetAndLoadOnPlay.tooltip);
            isResetAndLoadOnPlay.boolValue = EditorGUILayout.Toggle(
                isResetAndLoadOnPlayLabel,
                isResetAndLoadOnPlay.boolValue);

            // Build Settingsのシーン名だけを選べるPopupとして、起動時のロード順を描画する。
            EditorGUILayout.LabelField("初期化時にロードするシーン（複数）", EditorStyles.boldLabel);
            for (int i = 0; i < initializeSceneList.arraySize; i++)
            {
                SerializedProperty element = initializeSceneList.GetArrayElementAtIndex(i);
                int idx = Mathf.Max(0, Array.IndexOf(_sceneNames, element.stringValue));
                idx = EditorGUILayout.Popup((i + 1).ToString("00"), idx, _sceneNames);
                element.stringValue = _sceneNames[idx];
            }

            // 追加は末尾へ既定のシーンを設定し、削除も末尾だけに限定して順序を保つ。
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("＋ シーンを追加"))
            {
                initializeSceneList.InsertArrayElementAtIndex(initializeSceneList.arraySize);
                initializeSceneList.GetArrayElementAtIndex(
                    initializeSceneList.arraySize - 1).stringValue = _sceneNames.FirstOrDefault();
            }

            if (initializeSceneList.arraySize > 0 && GUILayout.Button("－ 最後のシーンを削除"))
            {
                initializeSceneList.DeleteArrayElementAtIndex(initializeSceneList.arraySize - 1);
            }
            GUILayout.EndHorizontal();

            // Undoとアセット保存へ反映できるよう、描画中の変更を確定する。
            bool changed = EditorGUI.EndChangeCheck();
            bool applied = serializedObject.ApplyModifiedProperties();
#if UNITY_6000_3_OR_NEWER
            // Inspectorから同じ設定を変更した場合も、開いたままのツールバーへ現在値を反映する。
            if (changed && applied)
            {
                SymphonyMainToolbar.RefreshSceneInitializationToggle();
            }
#endif
        }

        #endregion

        #region 内部処理

        private string[] _sceneNames;

        /// <summary>
        ///     Inspector表示時にBuild Settingsのシーン名一覧をキャッシュする。
        /// </summary>
        private void OnEnable()
        {
            // Inspectorを有効化した時点のBuild Settingsを、SceneLoadConfigが保持するシーン名の一覧へ変換する。
            _sceneNames = EditorBuildSettings.scenes
                .Select(s => Path.GetFileNameWithoutExtension(s.path))
                .ToArray();
        }

        #endregion
    }
}
