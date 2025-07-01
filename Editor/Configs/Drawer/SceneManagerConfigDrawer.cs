using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SymphonyFrameWork.Config;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    [CustomEditor(typeof(SceneManagerConfig))]
    public class SceneManagerConfigDrawer: UnityEditor.Editor
    {
        private string[] _sceneNames;
        
        private void OnEnable()
        {
            _sceneNames = EditorBuildSettings.scenes
                .Select(s => Path.GetFileNameWithoutExtension(s.path))
                .ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var isActiveLoadScene = serializedObject.FindProperty("_isActiveLoadScene");
            var loadScene = serializedObject.FindProperty("_loadScene");
            var initializeSceneList = serializedObject.FindProperty("_initializeSceneList");

            EditorGUILayout.PropertyField(isActiveLoadScene);

            #region isActiveLoadScene
            
            List<string> sceneNamesWithEmpty = _sceneNames.ToList();
            const string emptyName = "None";
            sceneNamesWithEmpty.Insert(0, emptyName);
            
            int selectedIndex = Mathf.Max(0, Array.IndexOf(sceneNamesWithEmpty.ToArray(), loadScene.stringValue));
            selectedIndex = EditorGUILayout.Popup("ロード中に表示されるシーン", selectedIndex, sceneNamesWithEmpty.ToArray());
            loadScene.stringValue = selectedIndex != 0 ? _sceneNames[selectedIndex - 1] : string.Empty;
            
            #endregion
            
            #region InitializeSceneList
            
            EditorGUILayout.LabelField("初期化時にロードするシーン（複数）", EditorStyles.boldLabel);
            
            //ポップアップを表示
            for (int i = 0; i < initializeSceneList.arraySize; i++)
            {
                var element = initializeSceneList.GetArrayElementAtIndex(i);
                int idx = Mathf.Max(0, Array.IndexOf(_sceneNames, element.stringValue));
                idx = EditorGUILayout.Popup((i + 1).ToString("00"), idx, _sceneNames);
                element.stringValue = _sceneNames[idx];
            }

            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("＋ シーンを追加"))
            {
                initializeSceneList.InsertArrayElementAtIndex(initializeSceneList.arraySize);
                initializeSceneList.GetArrayElementAtIndex(initializeSceneList.arraySize - 1).stringValue = _sceneNames.FirstOrDefault();
            }

            if (initializeSceneList.arraySize > 0 && GUILayout.Button("－ 最後のシーンを削除"))
            {
                initializeSceneList.DeleteArrayElementAtIndex(initializeSceneList.arraySize - 1);
            }
            
            GUILayout.EndHorizontal();
            
            #endregion

            serializedObject.ApplyModifiedProperties();
        }
    }
}