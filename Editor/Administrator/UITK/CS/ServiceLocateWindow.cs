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
    /// <summary>
    ///     Service Locatorの登録状態とデバッグログ設定を表示する。
    /// </summary>
    [UxmlElement]
    public sealed partial class ServiceLocateWindow :
        SymphonyVisualElement,
        IDisposable
    {
        #region 外部向けAPI

        /// <summary>
        ///     管理パネル用UXMLの非同期初期化を開始する。
        /// </summary>
        /// <remarks>
        ///     UXMLの基準パスはパッケージ導入とAssets直置きの双方を解決する。
        /// </remarks>
        public ServiceLocateWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "ServiceLocateWindow.uxml",
            InitializeTypeEnum.None,
            LoadTypeEnum.AssetDataBase)
        { }

        /// <summary>
        ///     ViewModelとEditor callbackの購読を解除する。
        /// </summary>
        public void Dispose()
        {
            // 破棄済みの場合は、同じ購読を重複して解除しない。
            if (_isDisposed) { return; }

            // Windowより長生きするEditor callbackとReactivePropertyの購読をすべて解除する。
            _isDisposed = true;
            EditorApplication.playModeStateChanged -= PlayModeStateChangedHandler;
            EditorApplication.delayCall -= DelayedBindViewModel;
            UnbindViewModel();
        }

        #endregion

        #region 内部処理

        private IDisposable _registrationSubscription;
        private List<ServiceLocateDto> _registrationItems = new();
        private ListView _locateList;
        private bool _isDisposed;

        /// <summary>
        ///     Play Mode遷移に合わせて現在のViewModelへ接続し直す。
        /// </summary>
        /// <param name="state"> 遷移後のPlay Mode状態。 </param>
        private void PlayModeStateChangedHandler(PlayModeStateChange state)
        {
            // Edit ModeにはViewModelが存在しないため未接続表示とし、Play Mode開始後に取得し直す。
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

        /// <summary>
        ///     登録一覧とService Locatorのログ設定Toggleを構成する。
        /// </summary>
        /// <param name="container"> UXMLから生成されたルート要素。 </param>
        /// <returns> 同期的に完了する初期化処理。 </returns>
        protected override Awaitable Initialize_S(VisualElement container)
        {
            // 非同期UXML初期化より先にWindowが閉じられた場合は、購読を開始しない。
            if (_isDisposed) { return SymphonyAwaitable.Completed(); }

            // RuntimeのDtoとUser Settingsを表示するVisualElementをUXMLへ接続する。
            SymphonyDocumentationGUI.BindOpenButton(container, SymphonyDocumentPageEnum.ServiceLocator);

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
            // ListViewの匿名ラムダは要素と同時に破棄されるが、static eventは明示解除できる形で購読する。
            EditorApplication.playModeStateChanged += PlayModeStateChangedHandler;
            BindViewModel();
            return SymphonyAwaitable.Completed();
        }

        /// <summary>
        ///     Service Locator初期化後のEditor callbackでViewModelへ接続する。
        /// </summary>
        private void DelayedBindViewModel()
        {
            // 一度だけ遅延実行し、Compositionによる初期化が完了した時点のViewModelを取得する。
            EditorApplication.delayCall -= DelayedBindViewModel;
            BindViewModel();
        }

        /// <summary>
        ///     現在のService Locate ViewModelへ接続する。
        /// </summary>
        private void BindViewModel()
        {
            // ViewModelはCompositionが所有するため、Windowは既存の購読ハンドルだけを置き換える。
            UnbindViewModel();

            // Window破棄後またはEdit ModeではViewModelへ接続せず、空の一覧を維持する。
            if (_isDisposed
                || !EditorApplication.isPlaying
                || !ServiceLocator.IsInitialized)
            {
                ApplyRegistrations(Array.Empty<ServiceLocateDto>());
                return;
            }

            ServiceLocateViewModel viewModel = ServiceLocator.CurrentViewModel;
            // CompositionがまだViewModelを公開していない場合は、未接続の空一覧にする。
            if (viewModel == null)
            {
                ApplyRegistrations(Array.Empty<ServiceLocateDto>());
                return;
            }

            // RuntimeのReactivePropertyはWindowより長生きするため、解除可能な購読ハンドルを保持する。
            _registrationSubscription =
                viewModel.Registrations.Subscribe(ApplyRegistrations);
        }

        /// <summary>
        ///     現在のViewModel購読を解除する。
        /// </summary>
        private void UnbindViewModel()
        {
            // 初期化時の購読とDisposeを対にし、古いCompositionへの参照を残さない。
            _registrationSubscription?.Dispose();
            _registrationSubscription = null;
        }

        /// <summary>
        ///     最新Dto一覧をListViewへ反映する。
        /// </summary>
        /// <param name="serviceDtos"> ViewModelが公開した変更不能なDto一覧。 </param>
        private void ApplyRegistrations(
            IReadOnlyList<ServiceLocateDto> serviceDtos)
        {
            // 通知後に元の一覧が変化しても表示内容が揺れないよう、Window専用のスナップショットを作る。
            _registrationItems = serviceDtos == null
                ? new List<ServiceLocateDto>()
                : new List<ServiceLocateDto>(serviceDtos);

            // UXML構築前に初期値が通知された場合は、保持だけ行い表示更新を待つ。
            if (_locateList == null) { return; }

            // ListViewの参照先を最新スナップショットへ差し替えて再構築する。
            _locateList.itemsSource = _registrationItems;
            _locateList.Rebuild();
        }

        /// <summary>
        ///     User Settingsへ保存されるログ設定Toggleを初期化する。
        /// </summary>
        /// <param name="container"> Toggleを所有するルート要素。 </param>
        private static void InitializeLogToggles(VisualElement container)
        {
            // 三種類のログ設定を同じUser Settingsアセットへ接続する。
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

        /// <summary>
        ///     User Settingsへ保存されるログ設定Toggleを初期化する。
        /// </summary>
        /// <param name="toggle"> 初期化するToggle。 </param>
        /// <param name="currentValue"> 現在の設定値。 </param>
        /// <param name="valueSetter"> 変更後の値を保存する処理。 </param>
        private static void InitializeToggle(
            Toggle toggle,
            bool currentValue,
            Action<bool> valueSetter)
        {
            // 対応するUXML要素が無い場合は、残りのパネル初期化を妨げない。
            if (toggle == null) { return; }

            // 現在値を表示した後、以降の変更だけを設定とRuntimeのログ動作へ反映する。
            toggle.value = currentValue;
            toggle.RegisterValueChangedCallback(changeEvent =>
            {
                valueSetter(changeEvent.newValue);
                PackageInitializer.ApplyServiceLocateLogOptions();
            });
        }

        #endregion
    }
}
