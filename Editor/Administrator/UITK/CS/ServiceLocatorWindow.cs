using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SymphonyFrameWork.System.ServiceLocate;
using SymphonyFrameWork.Utility;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary> Service Locatorの登録状態とデバッグログ設定を表示する管理パネル。 </summary>
    [UxmlElement]
    public sealed partial class ServiceLocatorWindow :
        SymphonyVisualElement,
        IDisposable
    {
        /// <summary> 管理パネル用UXMLの非同期初期化を開始する。 </summary>
        public ServiceLocatorWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "ServiceLocatorWindow.uxml",
            InitializeTypeEnum.None,
            LoadTypeEnum.AssetDataBase)
        { }

        private IDisposable _registrationSubscription;
        private List<ServiceLocateDto> _registrationItems = new();
        private ListView _locateList;
        private bool _isDisposed;

        /// <summary> ViewModelとEditor callbackの購読を解除する。 </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            EditorApplication.playModeStateChanged -= PlayModeStateChangedHandler;
            EditorApplication.delayCall -= DelayedBindViewModel;
            UnbindViewModel();
        }

        /// <summary> 登録一覧とService Locatorのログ設定Toggleを構成する。 </summary>
        /// <param name="container"> UXMLから生成されたルート要素。 </param>
        /// <returns> 同期的に完了する初期化処理。 </returns>
        protected override Awaitable Initialize_S(VisualElement container)
        {
            if (_isDisposed)
            {
                return SymphonyAwaitable.Completed();
            }

            _locateList = container.Q<ListView>("locate-list");
            _locateList.makeItem = () => new Label();
            _locateList.bindItem = (element, index) =>
            {
                ServiceLocateDto registration = _registrationItems[index];
                (element as Label).text =
                    $"type : {registration.ServiceTypeName}\n"
                    + $"obj : {registration.InstanceName}\n"
                    + $"locate type : {registration.LocateType}";
            };
            _locateList.itemsSource = _registrationItems;
            _locateList.selectionType = SelectionType.None;

            InitializeLogToggles(container);
            EditorApplication.playModeStateChanged += PlayModeStateChangedHandler;
            BindViewModel();
            return SymphonyAwaitable.Completed();
        }

        /// <summary> Play Mode遷移に合わせて現在のViewModelへ接続し直す。 </summary>
        /// <param name="state"> 遷移後のPlay Mode状態。 </param>
        private void PlayModeStateChangedHandler(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.delayCall -= DelayedBindViewModel;
                    EditorApplication.delayCall += DelayedBindViewModel;
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.delayCall -= DelayedBindViewModel;
                    UnbindViewModel();
                    ApplyRegistrations(Array.Empty<ServiceLocateDto>());
                    break;
            }
        }

        /// <summary> Service Locator初期化後のEditor callbackでViewModelへ接続する。 </summary>
        private void DelayedBindViewModel()
        {
            EditorApplication.delayCall -= DelayedBindViewModel;
            BindViewModel();
        }

        /// <summary> 現在のService Locate ViewModelへ接続する。 </summary>
        private void BindViewModel()
        {
            UnbindViewModel();

            if (_isDisposed
                || !EditorApplication.isPlaying
                || !ServiceLocator.IsInitialized)
            {
                ApplyRegistrations(Array.Empty<ServiceLocateDto>());
                return;
            }

            ServiceLocateViewModel viewModel = ServiceLocator.CurrentViewModel;
            if (viewModel == null)
            {
                ApplyRegistrations(Array.Empty<ServiceLocateDto>());
                return;
            }

            _registrationSubscription =
                viewModel.Registrations.Subscribe(ApplyRegistrations);
        }

        /// <summary> 現在のViewModel購読を解除する。 </summary>
        private void UnbindViewModel()
        {
            _registrationSubscription?.Dispose();
            _registrationSubscription = null;
        }

        /// <summary> 最新Dto一覧をListViewへ反映する。 </summary>
        /// <param name="serviceDtos"> ViewModelが公開した変更不能なDto一覧。 </param>
        private void ApplyRegistrations(
            IReadOnlyList<ServiceLocateDto> serviceDtos)
        {
            _registrationItems = serviceDtos == null
                ? new List<ServiceLocateDto>()
                : new List<ServiceLocateDto>(serviceDtos);

            if (_locateList == null)
            {
                return;
            }

            _locateList.itemsSource = _registrationItems;
            _locateList.Rebuild();
        }

        /// <summary> User Settingsへ保存されるログ設定Toggleを初期化する。 </summary>
        /// <param name="container"> Toggleを所有するルート要素。 </param>
        private static void InitializeLogToggles(VisualElement container)
        {
            SymphonyUserSettingConfig config =
                SymphonyEditorConfigLocator.GetConfig<SymphonyUserSettingConfig>();

            Toggle setInstanceLogActive =
                container.Q<Toggle>("set_instance-log-active");
            InitializeToggle(
                setInstanceLogActive,
                config.IsServiceLocatorSetInstanceLogEnabled,
                value => config.IsServiceLocatorSetInstanceLogEnabled = value);

            Toggle getInstanceLogActive =
                container.Q<Toggle>("get_instance-log-active");
            InitializeToggle(
                getInstanceLogActive,
                config.IsServiceLocatorGetInstanceLogEnabled,
                value => config.IsServiceLocatorGetInstanceLogEnabled = value);

            Toggle destroyInstanceLogActive =
                container.Q<Toggle>("destroy_instance-log-active");
            InitializeToggle(
                destroyInstanceLogActive,
                config.IsServiceLocatorDestroyInstanceLogEnabled,
                value => config.IsServiceLocatorDestroyInstanceLogEnabled = value);
        }

        /// <summary> User Settingsへ保存されるログ設定Toggleを初期化する。 </summary>
        /// <param name="toggle"> 初期化するToggle。 </param>
        /// <param name="currentValue"> 現在の設定値。 </param>
        /// <param name="valueSetter"> 変更後の値を保存する処理。 </param>
        private static void InitializeToggle(
            Toggle toggle,
            bool currentValue,
            Action<bool> valueSetter)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.value = currentValue;
            toggle.RegisterValueChangedCallback(changeEvent =>
            {
                valueSetter(changeEvent.newValue);
                PackageInitializer.ApplyServiceLocateLogOptions();
            });
        }
    }
}
