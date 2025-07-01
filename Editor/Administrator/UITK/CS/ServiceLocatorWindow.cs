using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.System;
using SymphonyFrameWork.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    [UxmlElement]
    public partial class ServiceLocatorWindow : SymphonyVisualElement
    {
        private Dictionary<Type, Component> _locateDict;
        private FieldInfo _locateInfo;
        private ListView _locateList;

        public ServiceLocatorWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "ServiceLocatorWindow.uxml",
            InitializeType.None,
            LoadType.AssetDataBase)
        { }

        protected override Task Initialize_S(TemplateContainer container)
        {
            _locateInfo =
                typeof(ServiceLocator).GetField("_singletonObjects", BindingFlags.Static | BindingFlags.NonPublic);

            if (_locateInfo != null) _locateDict = (Dictionary<Type, Component>)_locateInfo.GetValue(null);

            _locateList = container.Q<ListView>("locate-list");

            _locateList.makeItem = () => new Label();

            // 項目のバインド（データを UI に反映）
            _locateList.bindItem = (element, index) =>
            {
                var kvp = GetLocateList()[index];
                (element as Label).text = $"type : {kvp.Key.Name}\nobj : {kvp.Value.name}";
            };
            
            // データのセット
            _locateList.itemsSource = GetLocateList();

            // 選択タイプの設定
            _locateList.selectionType = SelectionType.None;

            //ログのコンフィグを初期化
            var setInstanceLogActive = container.Q<Toggle>("set_instance-log-active");
            InitializeToggle(setInstanceLogActive,
                EditorSymphonyConstant.ServiceLocatorSetInstanceKey,
                EditorSymphonyConstant.ServiceLocatorSetInstanceDefault);

            var getInstanceLogActive = container.Q<Toggle>("get_instance-log-active");
            InitializeToggle(getInstanceLogActive,
                EditorSymphonyConstant.ServiceLocatorGetInstanceKey,
                EditorSymphonyConstant.ServiceLocatorGetInstanceDefault);

            var destroyInstanceLogActive = container.Q<Toggle>("destroy_instance-log-active");
            InitializeToggle(destroyInstanceLogActive,
                EditorSymphonyConstant.ServiceLocatorDestroyInstanceKey,
                EditorSymphonyConstant.ServiceLocatorDestroyInstanceDefault);

            return Task.CompletedTask;
        }

        private List<KeyValuePair<Type, Component>> GetLocateList()
        {
            if (_locateDict != null) _locateDict = (Dictionary<Type, Component>)_locateInfo.GetValue(null);
            return _locateDict != null
                ? new List<KeyValuePair<Type, Component>>(_locateDict)
                : new List<KeyValuePair<Type, Component>>();
        }

        public void Update()
        {
            if (_locateList != null)
            {
                _locateList.itemsSource = GetLocateList();
                _locateList.Rebuild();
            }
        }

        private void InitializeToggle(Toggle toggle, string key, bool defaultValue)
        {
            if (toggle != null)
            {
                toggle.value = EditorPrefs.GetBool(key, defaultValue);
                toggle.RegisterValueChangedCallback(e => EditorPrefs.SetBool(key, e.newValue));
            }
        }
    }
}