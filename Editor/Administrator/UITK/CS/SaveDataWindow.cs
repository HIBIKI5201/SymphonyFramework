using SymphonyFrameWork.System.SaveSystem;
using SymphonyFrameWork.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Save Storeのキャッシュ確認、編集、保存操作を提供する。
    /// </summary>
    [UxmlElement]
    public sealed partial class SaveDataWindow : SymphonyVisualElement, IDisposable
    {
        #region 外部向けAPI

        /// <summary>
        ///     管理パネル用UXMLの読み込みを開始する。
        /// </summary>
        /// <remarks>
        ///     UXMLの基準パスはパッケージ導入とAssets直置きの双方を解決する。
        ///     一時編集状態は基底コンストラクタより先に必要なため、ここでは構築しない。
        /// </remarks>
        public SaveDataWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "SaveDataWindow.uxml",
            InitializeTypeEnum.None,
            LoadTypeEnum.AssetDataBase)
        { }

        /// <summary>
        ///     UIコールバックと一時編集用Unityオブジェクトを破棄する。
        /// </summary>
        public void Dispose()
        {
            // 破棄済みの場合は、同じ購読とUnityオブジェクトを重複して解放しない。
            if (_disposed) { return; }

            _disposed = true;

            // Windowより長生きするstatic eventとReactivePropertyの購読を対にして解除する。
            SaveStore.OnCurrentViewModelChanged -= ViewModelChangedHandler;
            EditorApplication.playModeStateChanged -= PlayModeStateChangedHandler;
            UnbindViewModel();

            // 遅延ロードの完了を無効化し、Window専用インスタンスを解放する。
            _activeLoadRequestId++;
            DisposeLocalContent();

            // IMGUIContainerからWindowへの参照を切り、SerializedObjectを先に破棄する。
            if (_editorContainer != null)
            {
                _editorContainer.onGUIHandler = null;
            }

            _debugSerializedObject?.Dispose();
            _debugSerializedObject = null;

            // 永続化対象外の一時ScriptableObjectをEditor上で即時破棄する。
            if (_debugState != null)
            {
                UnityEngine.Object.DestroyImmediate(_debugState);
            }
        }

        #endregion

        #region 内部処理

        private const string SELECTED_TYPE_SESSION_KEY = "SymphonyFrameWork.SaveDataWindow.SelectedTypeName";

        // 基底コンストラクタが Initialize_S を同期的に呼ぶため、コンストラクタ本体では
        // 間に合わない。フィールド初期化子は基底コンストラクタより先に走る。
        private readonly SaveDataDebugState _debugState = CreateDebugState();

        private SerializedObject _debugSerializedObject;
        private List<Type> _saveDataTypes = new();
        private Type _selectedType;
        private Type _localContentType;
        private SaveDataContent _localContent;
        private SaveDataContent _boundRegistryContent;
        private SaveDataBindingSourceEnum _bindingSource = SaveDataBindingSourceEnum.None;
        private string _statusMessage = "初期化中です…";
        private Vector2 _editorScrollPosition;

        private VisualElement _panelRoot;
        private VisualElement _bindingLamp;
        private Label _currentLoaderLabel;
        private Label _loadedEntriesCountLabel;
        private Label _bindingStateLabel;
        private Label _bindingSuffixLabel;
        private Label _statusLabel;
        private Toggle _carryOverToggle;
        private IMGUIContainer _editorContainer;
        private ListView _cacheListView;
        private IDisposable _entriesSubscription;
        private IReadOnlyList<SaveDataDto> _cachedEntries = Array.Empty<SaveDataDto>();
        private List<SaveDataEntryRow> _rows = new();
        private int _activeLoadRequestId;
        private bool _isLocalContentLoaded;
        private bool _isLocalContentDirty;
        private bool _isLocalContentLoading;
        private bool _hasLocalAutoLoadAttempted;
        private bool _skipNextCarryOverFlush;
        private bool _disposed;

        /// <summary>
        ///     ViewModelが差し替わったときに接続し直す。
        /// </summary>
        /// <remarks>
        ///     Save DataはEdit Modeでも初期化されるため、Play Mode遷移では検知できない。
        /// </remarks>
        private void ViewModelChangedHandler()
        {
            // Window破棄後にstatic eventの通知が残っても、再購読を開始しない。
            if (_disposed) { return; }

            BindViewModel();
        }

        /// <summary>
        ///     Play Mode突入時にWindow専用インスタンスの未保存変更を保存先へ書き出す。
        /// </summary>
        /// <param name="state"> EditorのPlay Mode遷移状態。 </param>
        private void PlayModeStateChangedHandler(PlayModeStateChange state)
        {
            // Edit Mode終了以外の通知と、破棄後の遅延通知では保存を開始しない。
            if (state != PlayModeStateChange.ExitingEditMode || _disposed) { return; }

            // 非同期保存後に自分で再開した遷移では、失敗時の再試行ループも含めて二重保存を防ぐ。
            if (_skipNextCarryOverFlush)
            {
                _skipNextCarryOverFlush = false;
                return;
            }

            // 明示的に有効化され、非接続のWindow専用インスタンスが編集済みの場合だけ持ち越す。
            if (!SymphonyUserSettingConfig.instance.IsSaveDataPlayModeCarryOverEnabled
                || !_isLocalContentDirty
                || _localContent == null
                || _selectedType != _localContentType
                || (_bindingSource != SaveDataBindingSourceEnum.Loaded
                    && _bindingSource != SaveDataBindingSourceEnum.Instance))
            {
                return;
            }

            SaveDataViewStore viewStore = SaveStore.CurrentViewStore;
            // Composition未初期化時は保存経路が無いため、Play Mode遷移を妨げない。
            if (viewStore == null) { return; }

            Type selectedType = _selectedType;
            SaveDataContent localContent = _localContent;
            Task saveTask;

            try
            {
                // 同期完了する同梱Loaderでは、Play Mode遷移を取り消さず結果だけを反映する。
                saveTask = viewStore.SaveDetachedAsync(selectedType, localContent);
            }
            catch (Exception ex)
            {
                // 同期例外を記録しても、利用者が要求したPlay Mode遷移自体は続行する。
                ReportCarryOverFailure(ex);
                return;
            }

            if (saveTask.IsCompleted)
            {
                CompleteSynchronousCarryOver(saveTask, selectedType, localContent);
                return;
            }

            // 真に非同期なLoaderでは遷移を一度取り消し、保存完了後に入り直す。
            EditorApplication.isPlaying = false;
            Debug.Log($"[{nameof(SaveDataWindow)}] Save Dataの持ち越し完了後にPlay Modeへ入り直します。");
            _ = CompleteCarryOverAndEnterPlayModeAsync(saveTask, selectedType, localContent);
        }

        /// <summary>
        ///     持ち越し設定の変更を個人設定へ保存する。
        /// </summary>
        /// <param name="changeEvent"> Toggleの値変更。 </param>
        private static void CarryOverValueChangedHandler(ChangeEvent<bool> changeEvent)
        {
            // ScriptableSingletonのsetterを通し、Editor再起動後も選択値を維持する。
            SymphonyUserSettingConfig.instance.IsSaveDataPlayModeCarryOverEnabled = changeEvent.newValue;
        }

        /// <summary>
        ///     レジストリ操作ボタン、一覧、データInspectorを構成する。
        /// </summary>
        protected override Awaitable Initialize_S(VisualElement root)
        {
            // IMGUI描画と型一覧の同期が参照するため、他の初期化より先に編集用SerializedObjectを構築する。
            RebindDebugState(null);

            // 管理操作と状態表示に使うVisualElementを生成済みUXMLへ接続する。
            SymphonyDocumentationGUI.BindOpenButton(root, SymphonyDocumentPageEnum.SaveDataSystem);

            _currentLoaderLabel = root.Q<Label>("save-current-loader");
            _loadedEntriesCountLabel = root.Q<Label>("save-loaded-entries");
            _panelRoot = root.Q<VisualElement>(className: "base");
            _bindingLamp = root.Q<VisualElement>("save-binding-lamp");
            _bindingStateLabel = root.Q<Label>("save-binding-state");
            _bindingSuffixLabel = root.Q<Label>("save-binding-suffix");
            _statusLabel = root.Q<Label>("save-status");
            _carryOverToggle = root.Q<Toggle>("save-carry-over");
            _editorContainer = root.Q<IMGUIContainer>("save-editor");
            _cacheListView = root.Q<ListView>("save-cache-list");

            // VisualElementへの匿名ラムダは、Windowが閉じたときに要素ごと破棄される。
            root.Q<Button>("save-load").clicked += () => ExecuteActionAsync(LoadSelectedAsync);
            root.Q<Button>("save-save").clicked += () => ExecuteActionAsync(SaveSelectedAsync);
            root.Q<Button>("save-delete").clicked += () => ExecuteActionAsync(DeleteSelectedAsync);
            _carryOverToggle.SetValueWithoutNotify(
                SymphonyUserSettingConfig.instance.IsSaveDataPlayModeCarryOverEnabled);
            _carryOverToggle.RegisterValueChangedCallback(CarryOverValueChangedHandler);

            _editorContainer.onGUIHandler = DrawEditorInspector;

            // ViewModel購読前に一覧の描画規則と対象型を揃え、初回通知を安全に反映できる状態にする。
            ConfigureCacheList();
            EnsureTypeListCurrent();

            // ViewModelはCompositionが所有し、Windowは差し替え通知と購読ハンドルだけを所有する。
            SaveStore.OnCurrentViewModelChanged += ViewModelChangedHandler;
            EditorApplication.playModeStateChanged += PlayModeStateChangedHandler;
            BindViewModel();

            return SymphonyAwaitable.Completed();
        }

        /// <summary>
        ///     現在のSave Data ViewModelへ接続する。
        /// </summary>
        private void BindViewModel()
        {
            // ViewModelはCompositionが所有するため、Windowは既存の購読ハンドルだけを置き換える。
            UnbindViewModel();

            SaveDataViewModel viewModel = _disposed ? null : SaveStore.CurrentViewModel;
            // Edit ModeでもComposition未初期化または破棄後なら、空の一覧を未接続表示として使用する。
            if (viewModel == null)
            {
                ApplyEntries(Array.Empty<SaveDataDto>());
                return;
            }

            // RuntimeのReactivePropertyはWindowより長生きするため、解除可能な購読ハンドルを保持する。
            _entriesSubscription = viewModel.Entries.Subscribe(ApplyEntries);
        }

        /// <summary>
        ///     現在のViewModel購読を解除する。
        /// </summary>
        private void UnbindViewModel()
        {
            // 初期化時の購読とDisposeを対にし、古いCompositionへの参照を残さない。
            _entriesSubscription?.Dispose();
            _entriesSubscription = null;
        }

        /// <summary>
        ///     ViewModelが公開した最新のキャッシュ一覧を表示へ反映する。
        /// </summary>
        /// <remarks>
        ///     未選択時は既存キャッシュだけを自動選択し、Getによる新規生成は行わない。
        /// </remarks>
        /// <param name="saveDataDtos"> ViewModelが公開した変更不能なDto一覧。 </param>
        private void ApplyEntries(IReadOnlyList<SaveDataDto> saveDataDtos)
        {
            // Window破棄後に遅延通知が届いた場合は、表示状態を更新しない。
            if (_disposed) { return; }

            // 通知後に元の一覧が変化しても表示が揺れないよう、行データを現在値から再構築する。
            _cachedEntries = saveDataDtos ?? Array.Empty<SaveDataDto>();

            // 未選択の場合だけ、既に実利用されているキャッシュから自動選択を試みる。
            if (_selectedType == null)
            {
                Type typeToSelect = ResolveAutoSelectType();
                // 既存キャッシュから候補を解決できた場合だけ選択を反映する。
                if (typeToSelect != null) { ApplyAutoSelection(typeToSelect); }
            }

            // 通知ごとに所有元を評価し、状態またはRegistry正本の参照が変わった場合だけ再バインドする。
            UpdateCurrentBinding();

            RefreshView();
        }

        /// <summary>
        ///     AppDomain内の対応セーブデータ型一覧を最新状態へ同期する。
        /// </summary>
        private void EnsureTypeListCurrent()
        {
            // ロード可能な全Assemblyから対応型だけを抽出し、表示順を完全名で固定する。
            List<Type> latestTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetTypesSafe)
                .Where(IsSupportedSaveDataType)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            // 対応型が存在しない場合は、古い選択と一時編集参照を残さない。
            // 初期値の一覧も空のため、下の変更判定では「変化なし」となり理由を表示できない。
            // 利用者から見れば未初期化と区別が付かないので、判定より先に扱う。
            if (latestTypes.Count <= 0)
            {
                _saveDataTypes = latestTypes;
                _selectedType = null;
                DisposeLocalContent();
                _bindingSource = SaveDataBindingSourceEnum.None;
                _boundRegistryContent = null;
                RebindDebugState(null);
                _statusMessage = "プロジェクト内に SaveDataContent を継承したセーブデータ型が見つかりません。";
                return;
            }

            bool changed = latestTypes.Count != _saveDataTypes.Count
                || !latestTypes.SequenceEqual(_saveDataTypes);

            // 型一覧が同一なら、現在の選択とInspector状態を維持する。
            if (!changed) { return; }

            _saveDataTypes = latestTypes;

            // _selectedType はドメインリロードで作り直されると null に戻る。その場合、
            // 「まだ誰もインスタンス化していない型」を自動選択して Get() で無理やり
            // インスタンス化させることはしない。Registry に既に乗っているデータ
            // （＝どこかで実際に使われているデータ）があればそれを優先して表示するだけに留める。
            ApplyAutoSelection(_selectedType ?? ResolveAutoSelectType());
        }

        /// <summary>
        ///     解決済みの型を選択状態へ反映する。
        /// </summary>
        /// <remarks>
        ///     RegistryとSessionStateに手がかりが無い場合は自動選択せず、新規生成を避ける。
        /// </remarks>
        private void ApplyAutoSelection(Type typeToSelect)
        {
            // 解決不能または対応外の型では、選択を消してキャッシュからの明示選択を待つ。
            if (typeToSelect == null || !_saveDataTypes.Contains(typeToSelect))
            {
                _selectedType = null;
                DisposeLocalContent();
                _bindingSource = SaveDataBindingSourceEnum.None;
                _boundRegistryContent = null;
                RebindDebugState(null);
                _statusMessage = "Save Data Types からセーブデータを選択してください。";
                return;
            }

            SetSelectedType(typeToSelect);
            UpdateCurrentBinding();
        }

        /// <summary>
        ///     自動選択するキャッシュ済みの型を決定する。
        /// </summary>
        /// <returns> 自動選択する型。キャッシュが空の場合はnull。 </returns>
        private Type ResolveAutoSelectType()
        {
            // 未使用の型をGetで生成しないよう、キャッシュが空なら自動選択しない。
            if (_cachedEntries.Count <= 0) { return null; }

            Type sessionType = RestoreSelectedTypeFromSession();
            // 前回選択が現在もキャッシュに存在する場合は、利用者の選択を優先する。
            if (sessionType != null
                && _cachedEntries.Any(entry => entry.DataType == sessionType))
            {
                return sessionType;
            }

            // ViewModel が公開する一覧は型名の昇順で並んでいる。
            return _cachedEntries[0].DataType;
        }

        /// <summary>
        ///     選択型を更新してSessionStateへ保存する。
        /// </summary>
        private void SetSelectedType(Type type)
        {
            // 別の型へ切り替える場合は、前のInspectorのスクロール位置を引き継がない。
            if (_selectedType != type)
            {
                _editorScrollPosition = Vector2.zero;
                DisposeLocalContent();
                _bindingSource = SaveDataBindingSourceEnum.None;
                _boundRegistryContent = null;
            }

            // ドメインリロード後も同じ型を復元できる形式でEditor Sessionへ保持する。
            _selectedType = type;
            SessionState.SetString(SELECTED_TYPE_SESSION_KEY, type?.AssemblyQualifiedName ?? string.Empty);
        }

        /// <summary>
        ///     SessionStateから前回選択していたセーブデータ型を復元する。
        /// </summary>
        private static Type RestoreSelectedTypeFromSession()
        {
            // 保存値が無い場合や型を解決できない場合は、未選択として扱う。
            string typeName = SessionState.GetString(SELECTED_TYPE_SESSION_KEY, string.Empty);
            return string.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName);
        }

        /// <summary>
        ///     キャッシュ一覧の要素生成、表示内容、選択イベントを構成する。
        /// </summary>
        private void ConfigureCacheList()
        {
            // 行ごとに生成するLabelへ、ロード状態と保存日時を同じ書式で表示する。
            _cacheListView.makeItem = () => new Label();
            _cacheListView.bindItem = (element, index) =>
            {
                SaveDataEntryRow row = _rows[index];
                string state = row.IsLoaded
                    ? "Loaded"
                    : row.IsSaved
                        ? "Saved"
                        : "Empty";

                ((Label)element).text =
                    $"{row.DataType.FullName}\nState: {state} / Date: {row.SaveDate ?? "(unknown)"}";
            };
            _cacheListView.selectionType = SelectionType.Single;
            // ListViewはWindowと同時に破棄されるため、要素側の購読は匿名で保持してよい。
            _cacheListView.selectionChanged += OnCacheSelectionChanged;
        }

        /// <summary>
        ///     キャッシュ一覧で選択されたセーブデータ型を編集対象へ反映する。
        /// </summary>
        private void OnCacheSelectionChanged(IEnumerable<object> selectedItems)
        {
            // 単一選択ListViewの先頭要素だけを処理し、追加列挙を避ける。
            foreach (object selectedItem in selectedItems)
            {
                // ListViewが行データを返した場合だけ、対応する型へ選択を切り替える。
                if (selectedItem is SaveDataEntryRow row) { SelectType(row.DataType); }

                return;
            }
        }

        /// <summary>
        ///     有効なセーブデータ型を選択して現在のキャッシュへバインドする。
        /// </summary>
        private void SelectType(Type type)
        {
            // null、対応外、選択済みの型では不要な再バインドと表示更新を行わない。
            if (type == null || !_saveDataTypes.Contains(type) || type == _selectedType) { return; }

            // 選択状態、編集対象、一覧表示を同じ型へまとめて更新する。
            SetSelectedType(type);
            UpdateCurrentBinding();
            RefreshView();
        }

        /// <summary>
        ///     選択中セーブデータをスクロール可能なInspectorとして描画する。
        /// </summary>
        private void DrawEditorInspector()
        {
            // SerializedObjectの最新状態を取り込み、必ず対になるスクロール領域内で描画する。
            _debugSerializedObject.Update();
            _editorScrollPosition = EditorGUILayout.BeginScrollView(
                _editorScrollPosition,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            try
            {
                SerializedProperty dataProperty = _debugSerializedObject.FindProperty("_data");
                // 未選択時は理由を表示し、選択時は[ReadOnly]のSaveDateを保ったまま派生フィールドを描画する。
                if (dataProperty.managedReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
                }
                else
                {
                    // 自動ロード完了時の上書きで編集内容を失わないよう、ロード中はInspectorを無効化する。
                    using (new EditorGUI.DisabledScope(_isLocalContentLoading))
                    {
                        EditorGUILayout.PropertyField(dataProperty, new GUIContent("Data"), true);
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }

            // Window専用インスタンスへの変更だけを未保存状態として記録する。
            bool wasModified = _debugSerializedObject.ApplyModifiedProperties();
            if (wasModified
                && (_bindingSource == SaveDataBindingSourceEnum.Loaded
                    || _bindingSource == SaveDataBindingSourceEnum.Instance))
            {
                _isLocalContentDirty = true;
                RefreshBindingIndicators();
            }
        }

        /// <summary>
        ///     現在の選択型に対するバインド元を評価し、必要な場合だけInspectorを再構築する。
        /// </summary>
        private void UpdateCurrentBinding()
        {
            SaveDataViewStore viewStore = SaveStore.CurrentViewStore;
            bool isRegistryLoaded = _selectedType != null
                && viewStore != null
                && viewStore.IsLoaded(_selectedType);

            // Registry正本が優先された時点で、Window専用インスタンスへの古い完了を表示対象から外す。
            if (isRegistryLoaded && _isLocalContentLoading)
            {
                _activeLoadRequestId++;
                _isLocalContentLoading = false;
                _hasLocalAutoLoadAttempted = false;
            }

            // 非接続時だけWindow専用インスタンスを用意し、保存値があれば自動ロードする。
            if (_selectedType != null && !isRegistryLoaded) { EnsureLocalContent(viewStore); }

            SaveDataBindingSourceEnum resolvedSource = SaveDataBindingState.Resolve(
                _selectedType != null,
                isRegistryLoaded,
                _isLocalContentLoaded);
            SaveDataContent registryContent = isRegistryLoaded
                ? viewStore.GetLoadedContent(_selectedType)
                : null;
            SaveDataContent bindingContent = resolvedSource == SaveDataBindingSourceEnum.Registry
                ? registryContent
                : _localContent;

            // 状態変化とRegistry正本の差し替えだけを再バインド対象にし、編集中のInspectorを維持する。
            bool shouldRebind = resolvedSource != _bindingSource
                || (resolvedSource == SaveDataBindingSourceEnum.Registry
                    && !ReferenceEquals(registryContent, _boundRegistryContent));
            if (shouldRebind)
            {
                _bindingSource = resolvedSource;
                _boundRegistryContent = registryContent;
                RebindDebugState(bindingContent);
                UpdateBindingStatusMessage();
            }

            RefreshBindingIndicators();
        }

        /// <summary>
        ///     選択型に対応するWindow専用インスタンスを用意する。
        /// </summary>
        /// <param name="viewStore"> I/Oを仲介する現在のViewStore。 </param>
        private void EnsureLocalContent(SaveDataViewStore viewStore)
        {
            // 同じ選択型のインスタンスがあれば、編集中の状態を維持する。
            if (_localContent != null && _localContentType == _selectedType)
            {
                // Composition接続後に保存値が見つかった場合は、未開始の自動ロードだけを補う。
                if (viewStore != null
                    && !_isLocalContentLoading
                    && !_isLocalContentLoaded
                    && !_isLocalContentDirty
                    && !_hasLocalAutoLoadAttempted
                    && viewStore.Exists(_selectedType))
                {
                    StartAutomaticLocalLoad(viewStore, _selectedType, _localContent);
                }

                return;
            }

            // 別の型が所有していたインスタンスを解放してから、現在型の既定値を生成する。
            DisposeLocalContent();
            _localContentType = _selectedType;
            _localContent = (SaveDataContent)Activator.CreateInstance(_selectedType);

            // Compositionが利用可能で保存値がある場合だけ、自動ロードを開始する。
            if (viewStore != null && viewStore.Exists(_selectedType))
            {
                StartAutomaticLocalLoad(viewStore, _selectedType, _localContent);
            }
        }

        /// <summary>
        ///     Window専用インスタンスへの自動ロードを開始する。
        /// </summary>
        /// <param name="viewStore"> I/Oを仲介するViewStore。 </param>
        /// <param name="selectedType"> 開始時点の選択型。 </param>
        /// <param name="target"> 読み込み先のWindow専用インスタンス。 </param>
        private void StartAutomaticLocalLoad(
            SaveDataViewStore viewStore,
            Type selectedType,
            SaveDataContent target)
        {
            // 同じWindow専用インスタンスでは、失敗後も無条件に自動再試行しない。
            _hasLocalAutoLoadAttempted = true;
            int requestId = BeginLocalLoad();
            _ = ExecuteAutomaticLocalLoadAsync(viewStore, selectedType, target, requestId);
        }

        /// <summary>
        ///     自動ロードの例外をConsoleとステータスへ記録する。
        /// </summary>
        /// <param name="viewStore"> I/Oを仲介するViewStore。 </param>
        /// <param name="selectedType"> 開始時点の選択型。 </param>
        /// <param name="target"> 読み込み先のWindow専用インスタンス。 </param>
        /// <param name="requestId"> 開始時点の要求ID。 </param>
        /// <returns> 自動ロードの完了を表すTask。 </returns>
        private async Task ExecuteAutomaticLocalLoadAsync(
            SaveDataViewStore viewStore,
            Type selectedType,
            SaveDataContent target,
            int requestId)
        {
            try
            {
                // UIをブロックせず、Window専用インスタンスへ保存値を復元する。
                await LoadLocalContentAsync(viewStore, selectedType, target, requestId);
            }
            catch (Exception ex)
            {
                // 古い要求の失敗は現在の選択へ表示せず、有効な要求だけを診断する。
                if (!IsCurrentLocalRequest(requestId, selectedType, target)) { return; }

                Debug.LogException(ex);
            }
        }

        /// <summary>
        ///     Window専用インスタンスへ保存値を読み込み、現在の要求だけを表示へ反映する。
        /// </summary>
        /// <param name="viewStore"> I/Oを仲介するViewStore。 </param>
        /// <param name="selectedType"> 開始時点の選択型。 </param>
        /// <param name="target"> 読み込み先のWindow専用インスタンス。 </param>
        /// <param name="requestId"> 開始時点の要求ID。 </param>
        /// <returns> 読み込みの完了を表すTask。 </returns>
        private async Task LoadLocalContentAsync(
            SaveDataViewStore viewStore,
            Type selectedType,
            SaveDataContent target,
            int requestId)
        {
            try
            {
                // Registryへ触れないViewStore経路で、指定インスタンスだけを更新する。
                await viewStore.LoadDetachedAsync(selectedType, target);
            }
            catch (Exception ex)
            {
                // 選択が変わった後に届いた失敗は、現在のUIへ反映せず呼び出し元へ返す。
                if (!IsCurrentLocalRequest(requestId, selectedType, target)) { throw; }

                _isLocalContentLoading = false;
                _hasLocalAutoLoadAttempted = true;
                _statusMessage = ex.Message;
                UpdateCurrentBinding();
                RefreshView();
                throw;
            }

            // 選択変更またはWindow破棄後に届いた完了は、現在の状態へ反映しない。
            if (!IsCurrentLocalRequest(requestId, selectedType, target)) { return; }

            _isLocalContentLoading = false;
            _isLocalContentLoaded = true;
            _isLocalContentDirty = false;
            _hasLocalAutoLoadAttempted = false;
            UpdateCurrentBinding();
            RefreshView();
        }

        /// <summary>
        ///     Window専用インスタンスへのロード要求を採番する。
        /// </summary>
        /// <returns> 新しい要求ID。 </returns>
        private int BeginLocalLoad()
        {
            // 新しい要求だけが完了状態を反映できるよう、単調増加するIDへ切り替える。
            _activeLoadRequestId++;
            _isLocalContentLoading = true;
            _isLocalContentLoaded = false;
            RefreshBindingIndicators();
            return _activeLoadRequestId;
        }

        /// <summary>
        ///     ロード完了が現在の選択とWindow専用インスタンスに対応するか確認する。
        /// </summary>
        /// <param name="requestId"> 完了した要求ID。 </param>
        /// <param name="selectedType"> 要求開始時の選択型。 </param>
        /// <param name="target"> 要求開始時の読み込み先。 </param>
        /// <returns> 現在も同じ要求が有効な場合はtrue。 </returns>
        private bool IsCurrentLocalRequest(
            int requestId,
            Type selectedType,
            SaveDataContent target) =>
            !_disposed
            && requestId == _activeLoadRequestId
            && selectedType == _selectedType
            && selectedType == _localContentType
            && ReferenceEquals(target, _localContent);

        /// <summary>
        ///     選択中の型を保存先から再ロードして編集状態へ反映する。
        /// </summary>
        /// <returns> ロード完了までの待機を表すTask。 </returns>
        private async Task LoadSelectedAsync()
        {
            SaveDataViewStore viewStore = GetCurrentViewStoreOrThrow();
            UpdateCurrentBinding();

            if (_bindingSource == SaveDataBindingSourceEnum.Registry)
            {
                // 接続中はRegistry正本を保存先から読み直す。
                await viewStore.LoadAsync(_selectedType);
                UpdateCurrentBinding();
            }
            else
            {
                // 非接続時は現在のWindow専用インスタンスだけへ読み込む。
                EnsureLocalContent(viewStore);
                // 進行中の自動ロードと同じインスタンスへのI/Oを重複させない。
                if (_isLocalContentLoading)
                {
                    _statusMessage = $"{_selectedType.FullName} をロード中です。";
                    RefreshView();
                    return;
                }

                int requestId = BeginLocalLoad();
                await LoadLocalContentAsync(viewStore, _selectedType, _localContent, requestId);
            }

            _statusMessage = $"{_selectedType.FullName} をロードしました。";
            RefreshView();
        }

        /// <summary>
        ///     Inspectorの編集内容をレジストリ正本へ同期して保存する。
        /// </summary>
        /// <returns> 保存完了までの待機を表すTask。 </returns>
        private async Task SaveSelectedAsync()
        {
            SaveDataViewStore viewStore = GetCurrentViewStoreOrThrow();
            UpdateCurrentBinding();

            if (_bindingSource == SaveDataBindingSourceEnum.Registry)
            {
                // SerializeReferenceの再構築で別インスタンスになった場合だけ、保存前にRegistry正本へ同期する。
                SaveDataContent editingData = _debugState.GetData();
                SaveDataContent canonical = viewStore.GetLoadedContent(_selectedType);
                if (!ReferenceEquals(canonical, editingData))
                {
                    JsonUtility.FromJsonOverwrite(
                        JsonUtility.ToJson(editingData),
                        canonical);
                }

                // 接続中はRegistry正本を通常経路で保存する。
                await viewStore.SaveAsync(_selectedType);
                UpdateCurrentBinding();
            }
            else
            {
                // 非接続時はWindow専用インスタンスだけを保存し、Registryへ登録しない。
                EnsureLocalContent(viewStore);
                if (_isLocalContentLoading)
                {
                    _statusMessage = $"{_selectedType.FullName} をロード中です。";
                    RefreshView();
                    return;
                }

                await viewStore.SaveDetachedAsync(_selectedType, _localContent);
                _isLocalContentDirty = false;
            }

            // 保存日時と永続化状態を一覧へ反映する。
            _statusMessage = $"{_selectedType.FullName} を保存しました。";
            RefreshView();
        }

        /// <summary>
        ///     確認後に選択型の保存データを削除する。
        /// </summary>
        /// <remarks> 削除後は現在インスタンスを初期化する。 </remarks>
        /// <returns> 削除完了までの待機を表すTask。 </returns>
        private async Task DeleteSelectedAsync()
        {
            // 確認ダイアログは同期のまま先に出し、承諾後だけ待機へ入る。
            if (!EditorUtility.DisplayDialog(
                    "Delete Save Data",
                    $"{_selectedType.FullName} の保存データを削除しますか？",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            SaveDataViewStore viewStore = GetCurrentViewStoreOrThrow();
            UpdateCurrentBinding();

            if (_bindingSource == SaveDataBindingSourceEnum.Registry)
            {
                // 接続中はRegistry正本を通常経路で既定値へ戻す。
                await viewStore.DeleteAsync(_selectedType);
                UpdateCurrentBinding();
            }
            else
            {
                // 非接続時は保存先だけを削除し、Window専用インスタンスを既定値で作り直す。
                if (_isLocalContentLoading)
                {
                    _statusMessage = $"{_selectedType.FullName} をロード中です。";
                    RefreshView();
                    return;
                }

                await viewStore.DeleteDetachedAsync(_selectedType);
                DisposeLocalContent();
                _bindingSource = SaveDataBindingSourceEnum.None;
                EnsureLocalContent(viewStore);
                UpdateCurrentBinding();
            }

            _statusMessage = $"{_selectedType.FullName} の保存データを削除し、現在インスタンスを初期化しました。";
            RefreshView();
        }

        /// <summary>
        ///     一時編集用のScriptableObjectを生成する。
        /// </summary>
        /// <remarks>
        ///     フィールド初期化子から呼ぶため、インスタンスの状態には触れない。
        /// </remarks>
        /// <returns> 永続化対象外のデバッグ用コンテナ。 </returns>
        private static SaveDataDebugState CreateDebugState()
        {
            // SerializedObjectで一時編集するため、Window専用のScriptableObjectを生成する。
            SaveDataDebugState debugState = ScriptableObject.CreateInstance<SaveDataDebugState>();

            // HideAndDontSave には NotEditable も含まれ、SerializedProperty がすべて
            // 読み取り専用になる。永続化だけを防ぎ、デバッグ編集は許可する。
            debugState.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            return debugState;
        }

        /// <summary>
        ///     デバッグInspectorをRegistryの正本へ再バインドする。
        /// </summary>
        /// <remarks>
        ///     SerializeReferenceの入れ替えを確実に反映するため、SerializedObject自体を作り直す。
        /// </remarks>
        private void RebindDebugState(SaveDataContent data)
        {
            // Window破棄後は、一時Unityオブジェクトを再生成しない。
            if (_disposed) { return; }

            // 古いSerializedObjectを破棄してから参照先を替え、新しい追跡対象として構築する。
            _debugSerializedObject?.Dispose();
            _debugState.SetData(data);
            _debugSerializedObject = new SerializedObject(_debugState);
        }

        /// <summary>
        ///     同期完了した持ち越し保存の結果をPlay Mode遷移前に反映する。
        /// </summary>
        /// <param name="saveTask"> 完了済みの保存Task。 </param>
        /// <param name="selectedType"> 保存開始時の選択型。 </param>
        /// <param name="localContent"> 保存開始時のWindow専用インスタンス。 </param>
        private void CompleteSynchronousCarryOver(
            Task saveTask,
            Type selectedType,
            SaveDataContent localContent)
        {
            // 失敗とキャンセルではDirtyを維持し、Play Mode遷移だけを続行する。
            if (saveTask.IsFaulted)
            {
                ReportCarryOverFailure(saveTask.Exception);
                return;
            }

            if (saveTask.IsCanceled)
            {
                ReportCarryOverFailure(new TaskCanceledException(saveTask));
                return;
            }

            // 保存対象が現在も同じ場合だけ、未保存表示を下ろす。
            if (selectedType == _selectedType && ReferenceEquals(localContent, _localContent))
            {
                _isLocalContentDirty = false;
                RefreshView();
            }
        }

        /// <summary>
        ///     非同期の持ち越し保存を待ち、結果に関係なくPlay Modeへ入り直す。
        /// </summary>
        /// <param name="saveTask"> 進行中の保存Task。 </param>
        /// <param name="selectedType"> 保存開始時の選択型。 </param>
        /// <param name="localContent"> 保存開始時のWindow専用インスタンス。 </param>
        /// <returns> 保存待機とPlay Mode再突入の完了を表すTask。 </returns>
        private async Task CompleteCarryOverAndEnterPlayModeAsync(
            Task saveTask,
            Type selectedType,
            SaveDataContent localContent)
        {
            try
            {
                // Editorの同期コンテキストを維持したまま、利用側の非同期Loaderを待つ。
                await saveTask;

                // 保存対象が現在も同じ場合だけ、未保存表示を下ろす。
                if (!_disposed
                    && selectedType == _selectedType
                    && ReferenceEquals(localContent, _localContent))
                {
                    _isLocalContentDirty = false;
                    RefreshView();
                }
            }
            catch (Exception ex)
            {
                // 保存失敗を記録しても、利用者が要求したPlay Mode遷移は再開する。
                ReportCarryOverFailure(ex);
            }

            // Windowが残っている場合は、再突入時のExitingEditModeで同じ保存を二重実行しない。
            if (!_disposed) { _skipNextCarryOverFlush = true; }

            // Windowが閉じられてもEditor自体は有効なため、要求済みのPlay Mode遷移を再開する。
            EditorApplication.EnterPlaymode();
        }

        /// <summary>
        ///     持ち越し保存の失敗をConsoleとパネルへ記録する。
        /// </summary>
        /// <param name="exception"> 保存処理で発生した例外。 </param>
        private void ReportCarryOverFailure(Exception exception)
        {
            Exception reportedException = exception is AggregateException aggregateException
                ? aggregateException.GetBaseException()
                : exception;
            Debug.LogException(reportedException);

            // Windowが残っている場合だけ、現在のパネルへ失敗理由を表示する。
            if (_disposed) { return; }

            _statusMessage = reportedException.Message;
            RefreshView();
        }

        /// <summary>
        ///     現在のSave Data ViewStoreを取得する。
        /// </summary>
        /// <returns> Compositionが所有する現在のViewStore。 </returns>
        /// <exception cref="InvalidOperationException"> Save Dataが未初期化の場合。 </exception>
        private static SaveDataViewStore GetCurrentViewStoreOrThrow()
        {
            // 操作のたびにCompositionが現在所有するViewStoreを取得する。
            SaveDataViewStore viewStore = SaveStore.CurrentViewStore;
            if (viewStore == null)
            {
                throw new InvalidOperationException("Save Data Systemが初期化されていません。");
            }

            return viewStore;
        }

        /// <summary>
        ///     Window専用インスタンスと遅延ロード状態を破棄する。
        /// </summary>
        private void DisposeLocalContent()
        {
            // 先に要求IDを進め、破棄後に届くロード完了を現在状態から切り離す。
            _activeLoadRequestId++;
            _isLocalContentLoading = false;
            _isLocalContentLoaded = false;
            _isLocalContentDirty = false;
            _hasLocalAutoLoadAttempted = false;

            // 利用側が所有するリソースも解放できるよう、基底契約のDisposeを必ず呼ぶ。
            _localContent?.Dispose();
            _localContent = null;
            _localContentType = null;
        }

        /// <summary>
        ///     バインド元の変化に対応するステータス文言を更新する。
        /// </summary>
        private void UpdateBindingStatusMessage()
        {
            // 未選択時は一覧からの明示選択を促す。
            if (_bindingSource == SaveDataBindingSourceEnum.None)
            {
                _statusMessage = "Save Data Types からセーブデータを選択してください。";
                return;
            }

            // 状態ラベルとは別に、現在の型とインスタンス所有者を診断用に表示する。
            _statusMessage = _bindingSource == SaveDataBindingSourceEnum.Registry
                ? $"{_selectedType.FullName} の現在インスタンスを表示しています。"
                : $"{_selectedType.FullName} のWindow専用インスタンスを表示しています。";
        }

        /// <summary>
        ///     バインド状態のランプ、枠線、状態文言を現在値へ更新する。
        /// </summary>
        private void RefreshBindingIndicators()
        {
            // UXML構築前または破棄後は、表示要素へ触れない。
            if (_disposed || _bindingLamp == null || _panelRoot == null) { return; }

            // 前状態の修飾クラスをすべて除去し、現在状態だけを付与する。
            foreach (string className in SaveDataBindingState.AllLampUssClassNames)
            {
                _bindingLamp.RemoveFromClassList(className);
                _panelRoot.RemoveFromClassList(className);
            }

            string currentClassName = SaveDataBindingState.GetLampUssClassName(_bindingSource);
            _bindingLamp.AddToClassList(currentClassName);
            _panelRoot.AddToClassList(currentClassName);
            _bindingStateLabel.text = SaveDataBindingState.GetDisplayText(_bindingSource);
            _bindingSuffixLabel.text = SaveDataBindingState.BuildStatusSuffix(
                _isLocalContentLoading,
                _isLocalContentDirty);
        }

        /// <summary>
        ///     選択状態を検証し、管理パネル操作中の例外をステータス表示へ変換する。
        /// </summary>
        /// <remarks>
        ///     Awaitableの同期待機によるEditor停止を避ける。UIイベント境界で例外を捕捉するためasync voidとする。
        /// </remarks>
        /// <param name="operation"> 実行する非同期操作。 </param>
        private async void ExecuteActionAsync(Func<Task> operation)
        {
            // 対象型が無い場合は非同期操作を開始せず、利用者へ選択を促す。
            if (_selectedType == null)
            {
                _statusMessage = "Save Data Types からセーブデータを選択してください。";
                RefreshView();
                return;
            }

            try
            {
                // Editorのメインスレッドを止めず、選択された操作の完了を待つ。
                await operation();
            }
            catch (Exception ex)
            {
                // 操作完了前にWindowが閉じられた場合は、破棄済みUIへ結果を反映しない。
                if (_disposed) { return; }

                // UIイベント境界で例外を捕捉し、Consoleとパネルの双方へ診断を残す。
                Debug.LogException(ex);
                _statusMessage = ex.Message;
                RefreshView();
            }
        }

        /// <summary>
        ///     管理パネルの表示を最新の行一覧へ更新する。
        /// </summary>
        /// <remarks> ViewModel通知とパネル操作の直後に呼び出す。 </remarks>
        private void RefreshView()
        {
            // Window破棄後またはUXML構築前に通知された場合は、VisualElementへ触れない。
            if (_disposed || _cacheListView == null) { return; }

            // ラベル、一覧、選択、Inspectorを同じ行スナップショットへまとめて更新する。
            _rows = BuildRows();
            _currentLoaderLabel.text = $"Current Loader: {GetCurrentLoaderName()}";
            _loadedEntriesCountLabel.text = $"Visible Entries: {_rows.Count}";
            _statusLabel.text = _statusMessage;
            RefreshBindingIndicators();
            _cacheListView.itemsSource = _rows;
            _cacheListView.Rebuild();
            SyncCacheSelection();

            _editorContainer.MarkDirtyRepaint();
        }

        /// <summary>
        ///     現在選択されているローダーの型名を取得する。
        /// </summary>
        /// <returns> ローダーの型名。未初期化の場合は代替表示。 </returns>
        private static string GetCurrentLoaderName()
        {
            // 未初期化時はローダー取得を避け、診断用の代替表示を返す。
            SaveDataViewStore viewStore = SaveStore.CurrentViewStore;
            return viewStore?.CurrentLoaderName ?? "(uninitialized)";
        }

        /// <summary>
        ///     現在の選択型に対応する一覧行を通知なしで選択状態へ同期する。
        /// </summary>
        private void SyncCacheSelection()
        {
            int selectedEntryIndex = -1;
            // 選択型と一致する最初の行を探し、ListViewの選択位置へ変換する。
            for (int index = 0; index < _rows.Count; index++)
            {
                // 現在の選択型と一致する行に到達した時点で探索を終える。
                if (_rows[index].DataType == _selectedType)
                {
                    selectedEntryIndex = index;
                    break;
                }
            }

            // 対応行が無い場合は、選択変更イベントを発火せず一覧選択だけを解除する。
            if (selectedEntryIndex < 0)
            {
                _cacheListView.SetSelectionWithoutNotify(Array.Empty<int>());
                return;
            }

            // 表示同期から再び選択処理へ入らないよう、通知なしで選択する。
            _cacheListView.SetSelectionWithoutNotify(new[] { selectedEntryIndex });
        }

        /// <summary>
        ///     対応型の順序に揃えた一覧行を組み立てる。
        /// </summary>
        /// <remarks>
        ///     対応する全セーブデータ型を行に含める。
        /// </remarks>
        /// <returns> 表示順に並んだ行一覧。 </returns>
        private List<SaveDataEntryRow> BuildRows()
        {
            // ViewModelのDtoを型で索引化し、対応型一覧との照合を一度ずつで済ませる。
            Dictionary<Type, SaveDataDto> cachedEntries = _cachedEntries
                .ToDictionary(entry => entry.DataType);
            SaveDataViewStore viewStore = SaveStore.CurrentViewStore;

            List<SaveDataEntryRow> rows = new(_saveDataTypes.Count);
            // 対応型の固定順を維持し、Runtime状態と永続化状態を一つの行へ統合する。
            foreach (Type saveDataType in _saveDataTypes)
            {
                bool isSaved = viewStore != null && viewStore.Exists(saveDataType);

                // キャッシュ済みの型は、Dtoのロード状態と保存日時を優先して行へ反映する。
                if (cachedEntries.TryGetValue(saveDataType, out SaveDataDto cachedEntry))
                {
                    rows.Add(new SaveDataEntryRow(
                        saveDataType,
                        cachedEntry.SaveDate,
                        cachedEntry.IsLoaded,
                        isSaved));
                    continue;
                }

                // 未キャッシュかつ未保存でも、Window専用インスタンスを選択できる行として表示する。
                rows.Add(new SaveDataEntryRow(saveDataType, null, false, isSaved));
            }

            return rows;
        }

        /// <summary>
        ///     Assemblyから取得可能な型だけを列挙する。
        /// </summary>
        /// <remarks> 一部の型をロードできないAssemblyでも、取得済みの型を保持する。 </remarks>
        private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                // すべての型を解決できるAssemblyでは通常の型一覧を返す。
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // 一部解決失敗でも、取得できた非null型まで捨てずに列挙する。
                return ex.Types.Where(type => type != null);
            }
        }

        /// <summary>
        ///     管理パネルで生成・編集できるセーブデータ具象型か検証する。
        /// </summary>
        private static bool IsSupportedSaveDataType(Type type)
        {
            // ScriptableObjectなどのUnity Objectと抽象・未構築型は、SerializeReferenceの編集対象にしない。
            if (type == null
                || !type.IsClass
                || type.IsAbstract
                || type.IsGenericTypeDefinition
                || typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return false;
            }

            // SaveStoreが生成できるよう、引数なしコンストラクタを持つ型だけを許可する。
            if (type.GetConstructor(Type.EmptyTypes) == null) { return false; }

            // セーブデータ契約を満たさない型は、同じ生成条件を持っていても対象外とする。
            if (!typeof(SaveDataContent).IsAssignableFrom(type)) { return false; }

            // UnityのSerializeReferenceで編集できるよう、Serializable指定を最後に確認する。
            return type.IsDefined(typeof(SerializableAttribute), false);
        }

        /// <summary>
        ///     一覧の1行分の表示値を保持する。
        /// </summary>
        /// <remarks>
        ///     キャッシュ済みエントリと、永続化データだけが存在する型を同じ形で扱う。
        /// </remarks>
        private readonly struct SaveDataEntryRow
        {
            #region 外部向けAPI

            /// <summary>
            ///     行の表示値を指定して初期化する。
            /// </summary>
            /// <param name="dataType"> 対象のセーブデータ型。 </param>
            /// <param name="saveDate"> 最終保存日時。 </param>
            /// <param name="isLoaded"> 永続化データを読み込み済みかどうか。 </param>
            /// <param name="isSaved"> 永続化データが存在するかどうか。 </param>
            internal SaveDataEntryRow(Type dataType, string saveDate, bool isLoaded, bool isSaved)
            {
                // 一覧描画に必要な値を、変更不能な単一行のスナップショットとして保持する。
                DataType = dataType;
                SaveDate = saveDate;
                IsLoaded = isLoaded;
                IsSaved = isSaved;
            }

            /// <summary> 対象のセーブデータ型。 </summary>
            internal Type DataType { get; }

            /// <summary> 最終保存日時。 </summary>
            internal string SaveDate { get; }

            /// <summary> 永続化データを読み込み済みかどうか。 </summary>
            internal bool IsLoaded { get; }

            /// <summary> 永続化データが存在するかどうか。 </summary>
            internal bool IsSaved { get; }

            #endregion
        }

        #endregion
    }
}
