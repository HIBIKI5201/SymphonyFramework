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
    /// <summary> SceneLoaderが追跡するシーン状態を一覧表示する管理パネル。 </summary>
    [UxmlElement]
    public sealed partial class SceneLoaderWindow : SymphonyVisualElement
    {
        private Dictionary<string, SceneLoadEntity> _sceneDict;
        private FieldInfo _registryField;
        private ListView _sceneList;

        /// <summary> 管理パネル用UXMLの非同期初期化を開始する。 </summary>
        public SceneLoaderWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "SceneLoaderWindow.uxml",
            InitializeType.None,
            LoadType.AssetDataBase)
        { }

        /// <summary> SceneLoaderの追跡データを参照するシーン一覧を構成する。 </summary>
        protected override ValueTask Initialize_S(VisualElement container)
        {
            _registryField = typeof(SceneLoader).GetField(
                "_registry",
                BindingFlags.Static | BindingFlags.NonPublic);

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

        /// <summary> SceneLoader内部の最新シーン辞書参照を取得する。 </summary>
        private void UpdateSceneDict()
        {
            if (_registryField == null) return;

            var sceneLoadRegistryInstance = _registryField.GetValue(null);

            if (sceneLoadRegistryInstance == null)
            {
                _sceneDict = new Dictionary<string, SceneLoadEntity>();
                return;
            }

            var sceneDictField =
                sceneLoadRegistryInstance.GetType()
                .GetField("_entities",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            if (sceneDictField != null)
            {
                _sceneDict =
                    (Dictionary<string, SceneLoadEntity>)sceneDictField.GetValue(
                        sceneLoadRegistryInstance);
            }
            else
            {
                _sceneDict = new Dictionary<string, SceneLoadEntity>();
            }
        }

        /// <summary> 表示時の列挙変更を避けるため、シーン辞書のスナップショットを生成する。 </summary>
        private List<KeyValuePair<string, SceneLoadEntity>> GetSceneList()
        {
            UpdateSceneDict();
            return _sceneDict != null
                ? new List<KeyValuePair<string, SceneLoadEntity>>(_sceneDict)
                : new List<KeyValuePair<string, SceneLoadEntity>>();
        }

        /// <summary> シーン一覧を最新の追跡状態で再構築する。 </summary>
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
