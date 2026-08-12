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
        ///     管理パネル用UXMLと一時編集状態の初期化を開始する。
        /// </summary>
        /// <remarks>
        ///     UXMLの基準パスはパッケージ導入とAssets直置きの双方を解決する。
        /// </remarks>
        public SaveDataWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "SaveDataWindow.uxml",
            InitializeTypeEnum.None,
            LoadTypeEnum.AssetDataBase)
        {
            // SerializedObjectで一時編集するため、Window専用のScriptableObjectを生成する。
            _debugState = ScriptableObject.CreateInstance<SaveDataDebugState>();
            // HideAndDontSave には NotEditable も含まれ、SerializedProperty がすべて
            // 読み取り専用になる。永続化だけを防ぎ、デバッグ編集は許可する。
            _debugState.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            _debugSerializedObject = new SerializedObject(_debugState);
        }

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
            UnbindViewModel();

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

        private readonly SaveDataDebugState _debugState;
        private SerializedObject _debugSerializedObject;
        private List<Type> _saveDataTypes = new();
        private Type _selectedType;
        private string _statusMessage = "初期化中です…";
        private Vector2 _editorScrollPosition;

        private Label _currentLoaderLabel;
        private Label _loadedEntriesCountLabel;
        private Label _statusLabel;
        private IMGUIContainer _editorContainer;
        private ListView _cacheListView;
        private IDisposable _entriesSubscription;
        private IReadOnlyList<SaveDataDto> _cachedEntries = Array.Empty<SaveDataDto>();
        private List<SaveDataEntryRow> _rows = new();
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
        ///     レジストリ操作ボタン、一覧、データInspectorを構成する。
        /// </summary>
        protected override Awaitable Initialize_S(VisualElement root)
        {
            // 管理操作と状態表示に使うVisualElementを生成済みUXMLへ接続する。
            SymphonyDocumentationGUI.BindOpenButton(root, SymphonyDocumentPageEnum.SaveDataSystem);

            _currentLoaderLabel = root.Q<Label>("save-current-loader");
            _loadedEntriesCountLabel = root.Q<Label>("save-loaded-entries");
            _statusLabel = root.Q<Label>("save-status");
            _editorContainer = root.Q<IMGUIContainer>("save-editor");
            _cacheListView = root.Q<ListView>("save-cache-list");

            // VisualElementへの匿名ラムダは、Windowが閉じたときに要素ごと破棄される。
            root.Q<Button>("save-load").clicked += () => ExecuteActionAsync(LoadSelectedAsync);
            root.Q<Button>("save-save").clicked += () => ExecuteActionAsync(SaveSelectedAsync);
            root.Q<Button>("save-delete").clicked += () => ExecuteActionAsync(DeleteSelectedAsync);

            _editorContainer.onGUIHandler = DrawEditorInspector;

            // ViewModel購読前に一覧の描画規則と対象型を揃え、初回通知を安全に反映できる状態にする。
            ConfigureCacheList();
            EnsureTypeListCurrent();

            // ViewModelはCompositionが所有し、Windowは差し替え通知と購読ハンドルだけを所有する。
            SaveStore.OnCurrentViewModelChanged += ViewModelChangedHandler;
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
            _rows = BuildRows();

            // 未選択の場合だけ、既に実利用されているキャッシュから自動選択を試みる。
            if (_selectedType == null)
            {
                Type typeToSelect = ResolveAutoSelectType();
                // 既存キャッシュから候補を解決できた場合だけ選択を反映する。
                if (typeToSelect != null) { ApplyAutoSelection(typeToSelect); }
            }

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

            bool changed = latestTypes.Count != _saveDataTypes.Count
                || !latestTypes.SequenceEqual(_saveDataTypes);

            // 型一覧が同一なら、現在の選択とInspector状態を維持する。
            if (!changed) { return; }

            _saveDataTypes = latestTypes;

            // 対応型が存在しない場合は、古い選択と一時編集参照を残さない。
            if (_saveDataTypes.Count <= 0)
            {
                _selectedType = null;
                RebindDebugState(null);
                _statusMessage = "プロジェクト内に SaveDataContent を継承したセーブデータ型が見つかりません。";
                return;
            }

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
                RebindDebugState(null);
                _statusMessage = "Registry Cache からセーブデータを選択してください。";
                return;
            }

            SetSelectedType(typeToSelect);
            BindCurrentSelection();
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
            if (_selectedType != type) { _editorScrollPosition = Vector2.zero; }

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
            BindCurrentSelection();
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
                    EditorGUILayout.PropertyField(dataProperty, new GUIContent("Data"), true);
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }

            // Inspectorで編集された値を一時ScriptableObjectへ反映する。
            _debugSerializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///     選択型のレジストリ正本を一時編集状態へバインドする。
        /// </summary>
        private void BindCurrentSelection()
        {
            // 型が未選択の場合は、暗黙に先頭の型を生成しない。
            if (_selectedType == null) { return; }

            // Get は読み込み済みの型だけを返す。未読み込みの場合は暗黙にロードせず、
            // Loadボタンの操作を促す。ここで自動ロードするとパネルを開いただけで
            // 保存先へのI/Oが走り、利用側の意図しない読み込みになる。
            if (!SaveStore.IsLoaded(_selectedType))
            {
                RebindDebugState(null);
                _statusMessage = $"{_selectedType.FullName} は未ロードです。Loadを実行すると内容を表示します。";
                return;
            }

            SaveDataContent data = SaveStore.Get(_selectedType);
            // ロード済みのRegistry正本だけをInspectorへ接続する。
            RebindDebugState(data);
            _statusMessage = $"{_selectedType.FullName} の現在インスタンスを表示しています。";
        }

        /// <summary>
        ///     選択中の型を保存先から再ロードして編集状態へ反映する。
        /// </summary>
        /// <returns> ロード完了までの待機を表すAwaitable。 </returns>
        private async Awaitable LoadSelectedAsync()
        {
            // 明示操作で永続化データを読み込み、Registry正本をInspectorへ接続し直す。
            await SaveStore.LoadAsync(_selectedType);
            SaveDataContent saveData = SaveStore.Get(_selectedType);

            RebindDebugState(saveData);
            _statusMessage = $"{_selectedType.FullName} をロードしました。";
            RefreshView();
        }

        /// <summary>
        ///     Inspectorの編集内容をレジストリ正本へ同期して保存する。
        /// </summary>
        /// <returns> 保存完了までの待機を表すAwaitable。 </returns>
        private async Awaitable SaveSelectedAsync()
        {
            // 保存対象はRegistryの正本であり、未読み込みのまま保存すると
            // 既定値で保存先を上書きしてしまう。先にロードして正本を確定させる。
            if (!SaveStore.IsLoaded(_selectedType)) { await SaveStore.LoadAsync(_selectedType); }

            // Inspector参照が空の場合は、ロード済みのRegistry正本へ再接続する。
            SaveDataContent editingData = _debugState.GetData();
            // SerializeReferenceが空なら、現在選択から編集参照を復元する。
            if (editingData == null)
            {
                BindCurrentSelection();
                editingData = _debugState.GetData();
            }

            SaveDataContent canonical = SaveStore.Get(_selectedType);
            // SerializeReferenceの再構築で別インスタンスになった場合だけ、保存前にRegistry正本へ同期する。
            if (!ReferenceEquals(canonical, editingData))
            {
                JsonUtility.FromJsonOverwrite(
                    JsonUtility.ToJson(editingData),
                    canonical);
            }

            // 保存後にSaveDateを含む最新のRegistry正本をInspectorと一覧へ反映する。
            await SaveStore.SaveAsync(_selectedType);
            SaveDataContent saveData = SaveStore.Get(_selectedType);
            RebindDebugState(saveData);
            _statusMessage = $"{_selectedType.FullName} を保存しました。";
            RefreshView();
        }

        /// <summary>
        ///     確認後に選択型の保存データを削除する。
        /// </summary>
        /// <remarks> 削除後は現在インスタンスを初期化する。 </remarks>
        /// <returns> 削除完了までの待機を表すAwaitable。 </returns>
        private async Awaitable DeleteSelectedAsync()
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

            // 削除後に再生成されたRegistry正本をInspectorと一覧へ反映する。
            await SaveStore.DeleteAsync(_selectedType);
            SaveDataContent regenerated = SaveStore.Get(_selectedType);
            RebindDebugState(regenerated);
            _statusMessage = $"{_selectedType.FullName} の保存データを削除し、現在インスタンスを初期化しました。";
            RefreshView();
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
        ///     選択状態を検証し、管理パネル操作中の例外をステータス表示へ変換する。
        /// </summary>
        /// <remarks>
        ///     Awaitableの同期待機によるEditor停止を避ける。UIイベント境界で例外を捕捉するためasync voidとする。
        /// </remarks>
        /// <param name="operation"> 実行する非同期操作。 </param>
        private async void ExecuteActionAsync(Func<Awaitable> operation)
        {
            // 対象型が無い場合は非同期操作を開始せず、利用者へ選択を促す。
            if (_selectedType == null)
            {
                _statusMessage = "Registry Cache からセーブデータを選択してください。";
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
            _currentLoaderLabel.text = $"Current Loader: {GetCurrentLoaderName()}";
            _loadedEntriesCountLabel.text = $"Visible Entries: {_rows.Count}";
            _statusLabel.text = _statusMessage;
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
            return SaveStore.IsInitialized
                ? SaveStore.GetCurrentLoader().GetType().Name
                : "(uninitialized)";
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
        ///     キャッシュ済みの型と、永続化データだけが存在する型を行に含める。
        /// </remarks>
        /// <returns> 表示順に並んだ行一覧。 </returns>
        private List<SaveDataEntryRow> BuildRows()
        {
            // ViewModelのDtoを型で索引化し、対応型一覧との照合を一度ずつで済ませる。
            Dictionary<Type, SaveDataDto> cachedEntries = _cachedEntries
                .ToDictionary(entry => entry.DataType);

            List<SaveDataEntryRow> rows = new(_saveDataTypes.Count);
            // 対応型の固定順を維持し、Runtime状態と永続化状態を一つの行へ統合する。
            foreach (Type saveDataType in _saveDataTypes)
            {
                bool isSaved = SaveStore.IsInitialized
                    && SaveStore.Exists(saveDataType);

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

                // 未キャッシュでも永続化データが存在する型は、ロード操作の入口として表示する。
                if (isSaved) { rows.Add(new SaveDataEntryRow(saveDataType, null, false, true)); }
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
