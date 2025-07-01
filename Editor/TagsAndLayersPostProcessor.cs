using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     タグやレイヤーの変更を監視する
    /// </summary>
    public class TagsAndLayersPostProcessor : AssetPostprocessor
    {
        public static TagsAndLayersSettingData SceneList = new();
        public static TagsAndLayersSettingData Tags = new("ProjectSettings/TagManager.asset");
        public static TagsAndLayersSettingData Layers = new("ProjectSettings/TagManager.asset");

        public class TagsAndLayersSettingData : IDisposable
        {
            private string _managerPath = string.Empty;
            public string TagManagerPath { get => _managerPath; }

            private string _lastManagerContent = string.Empty;
            public string LastManagerContent { get => _lastManagerContent; }

            public event Action OnSettingChanged;

            public TagsAndLayersSettingData() => _managerPath = string.Empty;
            public TagsAndLayersSettingData(string tagManagerPath)
            {
                if (File.Exists(tagManagerPath))
                {
                    _managerPath = tagManagerPath;
                    _lastManagerContent = File.ReadAllText(tagManagerPath);
                }
                else
                {
                    Debug.LogWarning($"{tagManagerPath}にアセットがありません。");
                }
            }

            public void SetLastManagerContent(string content) => _lastManagerContent = content;

            public void EventInvoke() => OnSettingChanged?.Invoke();
            public void Dispose() => OnSettingChanged = null;
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorBuildSettings.sceneListChanged -= SceneList.EventInvoke;
            EditorBuildSettings.sceneListChanged += SceneList.EventInvoke;
        }

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var data in new TagsAndLayersSettingData[] { Tags, Layers })
            {
                foreach (string asset in importedAssets)
                {
                    if (asset == data.TagManagerPath)
                    {
                        string newContent = File.ReadAllText(data.TagManagerPath);
                        if (newContent != data.LastManagerContent)
                        {
                            data.SetLastManagerContent(newContent);
                            data.EventInvoke();
                        }
                        break;
                    }
                }
            }
        }
    }
}
