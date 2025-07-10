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
        private Dictionary<Type, object> _locateDict; // Componentからobjectに変更
        private FieldInfo _lazyDataField; // _locateInfoから_lazyDataFieldに変更
        private ListView _locateList;

        public ServiceLocatorWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "ServiceLocatorWindow.uxml",
            InitializeType.None,
            LoadType.AssetDataBase)
        { }

        protected override Task Initialize_S(TemplateContainer container)
        {
            _lazyDataField = typeof(ServiceLocator).GetField("_data", BindingFlags.Static | BindingFlags.NonPublic);

            if (_lazyDataField != null)
            {
                // Lazy<ServiceLocatorData> のインスタンスを取得
                var lazyData = (Lazy<ServiceLocator.ServiceLocatorData>)_lazyDataField.GetValue(null);
                if (lazyData != null && lazyData.IsValueCreated)
                {
                    // ServiceLocatorData のインスタンスから _singletonObjects を取得
                    var singletonObjectsField = typeof(ServiceLocator.ServiceLocatorData).GetField("_singletonObjects", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (singletonObjectsField != null)
                    {
                        _locateDict = (Dictionary<Type, object>)singletonObjectsField.GetValue(lazyData.Value);
                    }
                }
            }

            _locateList = container.Q<ListView>("locate-list");

            _locateList.makeItem = () => new Label();

            // 項目のバインド（データを UI に反映）
            _locateList.bindItem = (element, index) =>
            {
                var kvp = GetLocateList()[index];
                // Componentの場合のみnameプロパティにアクセス
                string objName = (kvp.Value is Component component) ? component.name : kvp.Value.GetType().Name;
                (element as Label).text = $"type : {kvp.Key.Name}\nobj : {objName}";
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

        private List<KeyValuePair<Type, object>> GetLocateList() // Componentからobjectに変更
        {
            // _lazyDataFieldからLazy<ServiceLocatorData>のインスタンスを取得
            var lazyData = (Lazy<ServiceLocator.ServiceLocatorData>)_lazyDataField.GetValue(null);
            if (lazyData != null && lazyData.IsValueCreated)
            {
                // ServiceLocatorDataのインスタンスから_singletonObjectsを取得
                var singletonObjectsField = typeof(ServiceLocator.ServiceLocatorData).GetField("_singletonObjects", BindingFlags.Instance | BindingFlags.NonPublic);
                if (singletonObjectsField != null)
                {
                    _locateDict = (Dictionary<Type, object>)singletonObjectsField.GetValue(lazyData.Value);
                }
            }

            return _locateDict != null
                ? new List<KeyValuePair<Type, object>>(_locateDict) // Componentからobjectに変更
                : new List<KeyValuePair<Type, object>>(); // Componentからobjectに変更
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