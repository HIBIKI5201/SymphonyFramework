using SymphonyFrameWork.Core;
using SymphonyFrameWork.System.SceneLoad;
using SymphonyFrameWork.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    [UxmlElement]
    public partial class SceneLoaderWindow : SymphonyVisualElement
    {
        private Dictionary<string, SceneLoadData.SceneInfo> _sceneDict;
        private FieldInfo _dataField;
        private ListView _sceneList;

        public SceneLoaderWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "SceneLoaderWindow.uxml",
            InitializeType.None,
            LoadType.AssetDataBase)
        { }

        protected override ValueTask Initialize_S(VisualElement container)
        {
            _dataField = typeof(SceneLoader).GetField("_data", BindingFlags.Static | BindingFlags.NonPublic);

            UpdateSceneDict();

            _sceneList = container.Q<ListView>("scene-list");

            _sceneList.makeItem = () => new Label();

            _sceneList.bindItem = (element, index) =>
            {
                var kvp = GetSceneList()[index];
                (element as Label).text = $"Name : {kvp.Key}\nState : {kvp.Value.State}, Priority : {kvp.Value.Priority}";
            };

            _sceneList.itemsSource = GetSceneList();
            _sceneList.selectionType = SelectionType.None;

            return default;
        }

        private void UpdateSceneDict()
        {
            if (_dataField == null) return;

            var sceneLoadDataInstance = _dataField.GetValue(null);

            if (sceneLoadDataInstance == null)
            {
                _sceneDict = new Dictionary<string, SceneLoadData.SceneInfo>();
                return;
            }

            var sceneDictField =
                sceneLoadDataInstance.GetType()
                .GetField("_sceneDict",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            if (sceneDictField != null)
            {
                _sceneDict = (Dictionary<string, SceneLoadData.SceneInfo>)sceneDictField.GetValue(sceneLoadDataInstance);
            }
            else
            {
                _sceneDict = new Dictionary<string, SceneLoadData.SceneInfo>();
            }
        }

        private List<KeyValuePair<string, SceneLoadData.SceneInfo>> GetSceneList()
        {
            UpdateSceneDict();
            return _sceneDict != null
                ? new List<KeyValuePair<string, SceneLoadData.SceneInfo>>(_sceneDict)
                : new List<KeyValuePair<string, SceneLoadData.SceneInfo>>();
        }

        public void Update()
        {
            if (_sceneList != null)
            {
                _sceneList.itemsSource = GetSceneList();
                _sceneList.Rebuild();
            }
        }
    }
}
