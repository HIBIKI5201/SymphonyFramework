using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SymphonyFrameWork.System.SceneBlock;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Scene Blockアセットのシーン一覧、依存辺、検証結果をInspectorへ表示する。
    /// </summary>
    [CustomEditor(typeof(SceneBlockAsset))]
    public sealed class SceneBlockAssetDrawer: UnityEditor.Editor
    {
        #region 外部向けAPI

        /// <summary>
        ///     Scene Blockアセットの専用Inspectorを描画する。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty sceneIds = serializedObject.FindProperty("_sceneIds");
            SerializedProperty edges = serializedObject.FindProperty("_edges");

            DrawSceneIds(sceneIds);
            EditorGUILayout.Space();
            DrawEdges(sceneIds, edges);

            serializedObject.ApplyModifiedProperties();
            DrawPlanResult(((SceneBlockAsset)target).Plan());
        }

        #endregion

        #region 内部処理

        private const string UNSELECTED_LABEL = "未選択";

        private string[] _sceneNames;

        /// <summary>
        ///     Inspector表示時にBuild Settingsのシーン名一覧をキャッシュする。
        /// </summary>
        private void OnEnable()
        {
            _sceneNames = EditorBuildSettings.scenes
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                .ToArray();
        }

        /// <summary> Build Settingsから選択するシーン識別子一覧を描画する。 </summary>
        /// <param name="sceneIds"> シーン識別子一覧のSerializedProperty。 </param>
        private void DrawSceneIds(SerializedProperty sceneIds)
        {
            EditorGUILayout.LabelField("シーン", EditorStyles.boldLabel);
            for (int index = 0; index < sceneIds.arraySize; index++)
            {
                SerializedProperty sceneId = sceneIds.GetArrayElementAtIndex(index);
                if (_sceneNames.Length == 0)
                {
                    EditorGUILayout.PropertyField(sceneId, new GUIContent((index + 1).ToString("00")));
                    continue;
                }

                int selectedIndex = Mathf.Max(0, Array.IndexOf(_sceneNames, sceneId.stringValue));
                selectedIndex = EditorGUILayout.Popup(
                    (index + 1).ToString("00"),
                    selectedIndex,
                    _sceneNames);
                sceneId.stringValue = _sceneNames[selectedIndex];
            }

            GUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_sceneNames.Length == 0))
            {
                if (GUILayout.Button("＋ シーンを追加"))
                {
                    sceneIds.InsertArrayElementAtIndex(sceneIds.arraySize);
                    sceneIds.GetArrayElementAtIndex(sceneIds.arraySize - 1).stringValue = _sceneNames[0];
                }
            }

            if (sceneIds.arraySize > 0 && GUILayout.Button("－ 最後のシーンを削除"))
            {
                sceneIds.DeleteArrayElementAtIndex(sceneIds.arraySize - 1);
            }
            GUILayout.EndHorizontal();
        }

        /// <summary> 現在のシーン識別子から選択する依存辺一覧を描画する。 </summary>
        /// <param name="sceneIds"> シーン識別子一覧のSerializedProperty。 </param>
        /// <param name="edges"> 依存辺一覧のSerializedProperty。 </param>
        private static void DrawEdges(
            SerializedProperty sceneIds,
            SerializedProperty edges)
        {
            EditorGUILayout.LabelField("依存辺", EditorStyles.boldLabel);
            if (sceneIds.arraySize == 0)
            {
                EditorGUILayout.HelpBox("先にシーンを追加してください", MessageType.Info);
            }

            string[] sceneIdOptions = BuildSceneIdOptions(sceneIds);
            for (int index = 0; index < edges.arraySize; index++)
            {
                SerializedProperty edge = edges.GetArrayElementAtIndex(index);
                SerializedProperty from = edge.FindPropertyRelative("_from");
                SerializedProperty to = edge.FindPropertyRelative("_to");

                EditorGUILayout.LabelField((index + 1).ToString("00"));
                EditorGUI.indentLevel++;
                DrawSceneIdPopup("From", from, sceneIdOptions);
                DrawSceneIdPopup("To", to, sceneIdOptions);
                EditorGUI.indentLevel--;
            }

            GUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(sceneIds.arraySize == 0))
            {
                if (GUILayout.Button("＋ 依存辺を追加"))
                {
                    edges.InsertArrayElementAtIndex(edges.arraySize);
                    SerializedProperty addedEdge = edges.GetArrayElementAtIndex(edges.arraySize - 1);
                    addedEdge.FindPropertyRelative("_from").stringValue = string.Empty;
                    addedEdge.FindPropertyRelative("_to").stringValue = string.Empty;
                }
            }

            if (edges.arraySize > 0 && GUILayout.Button("－ 最後の依存辺を削除"))
            {
                edges.DeleteArrayElementAtIndex(edges.arraySize - 1);
            }
            GUILayout.EndHorizontal();
        }

        /// <summary> シーン識別子一覧から未選択を含むPopup候補を作成する。 </summary>
        /// <param name="sceneIds"> シーン識別子一覧のSerializedProperty。 </param>
        /// <returns> 未選択を先頭に置いたPopup候補。 </returns>
        private static string[] BuildSceneIdOptions(SerializedProperty sceneIds)
        {
            List<string> options = new() { UNSELECTED_LABEL };
            for (int index = 0; index < sceneIds.arraySize; index++)
            {
                string sceneId = sceneIds.GetArrayElementAtIndex(index).stringValue;
                if (!string.IsNullOrWhiteSpace(sceneId) && !options.Contains(sceneId))
                {
                    options.Add(sceneId);
                }
            }

            return options.ToArray();
        }

        /// <summary> 依存辺の片端をシーン識別子一覧から選択するPopupとして描画する。 </summary>
        /// <param name="label"> Popupのラベル。 </param>
        /// <param name="sceneId"> 編集するシーン識別子のSerializedProperty。 </param>
        /// <param name="options"> 未選択を含むシーン識別子候補。 </param>
        private static void DrawSceneIdPopup(
            string label,
            SerializedProperty sceneId,
            string[] options)
        {
            int optionIndex = Array.IndexOf(options, sceneId.stringValue);
            optionIndex = Mathf.Max(0, optionIndex);
            optionIndex = EditorGUILayout.Popup(label, optionIndex, options);
            sceneId.stringValue = optionIndex == 0 ? string.Empty : options[optionIndex];
        }

        /// <summary> 計画の検証エラーまたは実行層の概要を表示する。 </summary>
        /// <param name="result"> 表示するScene Blockの計画結果。 </param>
        private static void DrawPlanResult(SceneBlockPlanResult result)
        {
            if (!result.IsSuccess)
            {
                foreach (IGrouping<SceneBlockPlanErrorEnum, SceneBlockPlanError> errorGroup in
                         result.Errors.GroupBy(error => error.Kind))
                {
                    IEnumerable<string> nodeIds = errorGroup
                        .SelectMany(error => error.NodeIds)
                        .Distinct(StringComparer.Ordinal);
                    EditorGUILayout.HelpBox(
                        $"{errorGroup.Key}: {string.Join(", ", nodeIds)}",
                        MessageType.Error);
                }

                return;
            }

            string nodeCounts = string.Join(
                ", ",
                result.Layers.Select(layer => layer.Count.ToString()));
            EditorGUILayout.HelpBox(
                $"層数: {result.Layers.Count} / 各層のノード数: {nodeCounts}",
                MessageType.Info);
        }

        #endregion
    }
}
