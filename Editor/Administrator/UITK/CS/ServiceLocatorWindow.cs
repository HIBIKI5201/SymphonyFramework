using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SymphonyFrameWork.System.ServiceLocate;
using SymphonyFrameWork.Utility;

using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary> Service Locatorの登録状態とデバッグログ設定を表示する管理パネル。 </summary>
    [UxmlElement]
    public sealed partial class ServiceLocatorWindow : SymphonyVisualElement
    {
        private ListView _locateList;

        /// <summary> 管理パネル用UXMLの非同期初期化を開始する。 </summary>
        public ServiceLocatorWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "ServiceLocatorWindow.uxml",
            InitializeType.None,
            LoadType.AssetDataBase)
        { }

        /// <summary> 登録一覧とService Locatorのログ設定Toggleを構成する。 </summary>
        protected override ValueTask Initialize_S(VisualElement container)
        {
            _locateList = container.Q<ListView>("locate-list");

            _locateList.makeItem = () => new Label();

            // 項目のバインド（データを UI に反映）
            _locateList.bindItem = (element, index) =>
            {
                var kvp = GetLocateList()[index];
                if (kvp.Value is UnityEngine.Object unityObject && unityObject == null)
                {
                    (element as Label).text = $"type : {kvp.Key.Name}\nobj : (Destroyed)";
                    return;
                }
                // Componentの場合のみnameプロパティにアクセス
                string objName = (kvp.Value is Component component) ? component.name : kvp.Value.GetType().Name;
                (element as Label).text = $"type : {kvp.Key.Name}\nobj : {objName}";
            };
            
            // データのセット
            _locateList.itemsSource = GetLocateList();

            // 選択タイプの設定
            _locateList.selectionType = SelectionType.None;

            //ログのコンフィグを初期化
            SymphonyUserSettingConfig config =
                SymphonyEditorConfigLocator.GetConfig<SymphonyUserSettingConfig>();

            var setInstanceLogActive = container.Q<Toggle>("set_instance-log-active");
            InitializeToggle(setInstanceLogActive,
                config.IsServiceLocatorSetInstanceLogEnabled,
                value => config.IsServiceLocatorSetInstanceLogEnabled = value);

            var getInstanceLogActive = container.Q<Toggle>("get_instance-log-active");
            InitializeToggle(getInstanceLogActive,
                config.IsServiceLocatorGetInstanceLogEnabled,
                value => config.IsServiceLocatorGetInstanceLogEnabled = value);

            var destroyInstanceLogActive = container.Q<Toggle>("destroy_instance-log-active");
            InitializeToggle(destroyInstanceLogActive,
                config.IsServiceLocatorDestroyInstanceLogEnabled,
                value => config.IsServiceLocatorDestroyInstanceLogEnabled = value);

            return default;
        }

        /// <summary> 表示用に登録payloadの変更不能なスナップショットをListへ変換する。 </summary>
        private List<KeyValuePair<Type, object>> GetLocateList()
        {
            IReadOnlyDictionary<Type, object> registeredInstances =
                ServiceLocator.IsInitialized
                    ? ServiceLocator.RegisteredInstances
                    : null;

            return registeredInstances != null
                ? new List<KeyValuePair<Type, object>>(registeredInstances)
                : new List<KeyValuePair<Type, object>>();
        }

        /// <summary> 登録一覧を最新のService Locator状態で再構築する。 </summary>
        public void Update()
        {
            if (_locateList != null)
            {
                _locateList.itemsSource = GetLocateList();
                _locateList.Rebuild();
            }
        }

        /// <summary> UserSettingsへ保存されるログ設定Toggleを初期化する。 </summary>
        private static void InitializeToggle(Toggle toggle, bool currentValue, Action<bool> valueSetter)
        {
            if (toggle != null)
            {
                toggle.value = currentValue;
                toggle.RegisterValueChangedCallback(changeEvent =>
                {
                    valueSetter(changeEvent.newValue);
                    PackageInitializer.ApplyServiceLocateLogOptions();
                });
            }
        }
    }
}
