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
    /// <summary> Save Storeのキャッシュ確認、編集、保存操作を提供する管理パネル。 </summary>
    [UxmlElement]
    public sealed partial class SaveDataWindow : SymphonyVisualElement, IDisposable
    {
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
        ///     一覧の1行分の表示値。
        ///     キャッシュ済みエントリ（<see cref="SaveDataDto" />）と、
        ///     永続化データはあるがキャッシュされていない型の両方を同じ形で扱う。
        /// </summary>
        private readonly struct SaveDataEntryRow
        {
            /// <summary> 行の表示値を指定して生成する。 </summary>
            /// <param name="dataType"> 対象のセーブデータ型。 </param>
            /// <param name="saveDate"> 最終保存日時。 </param>
            /// <param name="isLoaded"> 永続化データを読み込み済みかどうか。 </param>
            /// <param name="isSaved"> 永続化データが存在するかどうか。 </param>
            internal SaveDataEntryRow(Type dataType, string saveDate, bool isLoaded, bool isSaved)
            {
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
        }

        /// <summary> 管理パネル用UXMLと一時編集状態の初期化を開始する。 </summary>
        public SaveDataWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "SaveDataWindow.uxml",
            InitializeTypeEnum.None,
            LoadTypeEnum.AssetDataBase)
        {
            _debugState = ScriptableObject.CreateInstance<SaveDataDebugState>();
            // HideAndDontSave には NotEditable も含まれ、SerializedProperty がすべて
            // 読み取り専用になる。永続化だけを防ぎ、デバッグ編集は許可する。
            _debugState.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            _debugSerializedObject = new SerializedObject(_debugState);
        }

        /// <summary> レジストリ操作ボタン、一覧、データInspectorを構成する。 </summary>
        protected override Awaitable Initialize_S(VisualElement root)
        {
            _currentLoaderLabel = root.Q<Label>("save-current-loader");
            _loadedEntriesCountLabel = root.Q<Label>("save-loaded-entries");
            _statusLabel = root.Q<Label>("save-status");
            _editorContainer = root.Q<IMGUIContainer>("save-editor");
            _cacheListView = root.Q<ListView>("save-cache-list");

            root.Q<Button>("save-load").clicked += () => ExecuteActionAsync(LoadSelectedAsync);
            root.Q<Button>("save-save").clicked += () => ExecuteActionAsync(SaveSelectedAsync);
            root.Q<Button>("save-delete").clicked += () => ExecuteActionAsync(DeleteSelectedAsync);

            _editorContainer.onGUIHandler = DrawEditorInspector;

            ConfigureCacheList();
            EnsureTypeListCurrent();

            SaveStore.OnCurrentViewModelChanged += ViewModelChangedHandler;
            BindViewModel();

            return SymphonyAwaitable.Completed();
        }

        /// <summary> UIコールバックと一時編集用Unityオブジェクトを破棄する。 </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            SaveStore.OnCurrentViewModelChanged -= ViewModelChangedHandler;
            UnbindViewModel();

            if (_editorContainer != null)
            {
                _editorContainer.onGUIHandler = null;
            }

            _debugSerializedObject?.Dispose();
            _debugSerializedObject = null;

            if (_debugState != null)
            {
                UnityEngine.Object.DestroyImmediate(_debugState);
            }
        }

        /// <summary>
        ///     ViewModelが差し替わったときに接続し直す。
        ///     Save DataはEdit Modeでも初期化されるため、Play Mode遷移では検知できない。
        /// </summary>
        private void ViewModelChangedHandler()
        {
            if (_disposed)
            {
                return;
            }

            BindViewModel();
        }

        /// <summary> 現在のSave Data ViewModelへ接続する。 </summary>
        private void BindViewModel()
        {
            UnbindViewModel();

            SaveDataViewModel viewModel = _disposed ? null : SaveStore.CurrentViewModel;
            if (viewModel == null)
            {
                ApplyEntries(Array.Empty<SaveDataDto>());
                return;
            }

            _entriesSubscription = viewModel.Entries.Subscribe(ApplyEntries);
        }

        /// <summary> 現在のViewModel購読を解除する。 </summary>
        private void UnbindViewModel()
        {
            _entriesSubscription?.Dispose();
            _entriesSubscription = null;
        }

        /// <summary>
        ///     ViewModelが公開した最新のキャッシュ一覧を表示へ反映する。
        ///     未選択のまま新しいデータが乗った場合（他のコードがGet/Load/Saveした場合など）は、
        ///     ここで初めて自動選択する。Get()を呼んで新規インスタンス化することはしない。
        /// </summary>
        /// <param name="saveDataDtos"> ViewModelが公開した変更不能なDto一覧。 </param>
        private void ApplyEntries(IReadOnlyList<SaveDataDto> saveDataDtos)
        {
            if (_disposed)
            {
                return;
            }

            _cachedEntries = saveDataDtos ?? Array.Empty<SaveDataDto>();
            _rows = BuildRows();

            if (_selectedType == null)
            {
                Type typeToSelect = ResolveAutoSelectType();
                if (typeToSelect != null)
                {
                    ApplyAutoSelection(typeToSelect);
                }
            }

            RefreshView();
        }

        /// <summary> AppDomain内の対応セーブデータ型一覧を最新状態へ同期する。 </summary>
        private void EnsureTypeListCurrent()
        {
            List<Type> latestTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetTypesSafe)
                .Where(IsSupportedSaveDataType)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            bool changed = latestTypes.Count != _saveDataTypes.Count
                || !latestTypes.SequenceEqual(_saveDataTypes);

            if (!changed)
            {
                return;
            }

            _saveDataTypes = latestTypes;

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
        ///     解決済みの型（null の場合は「まだ何も選べない」）を実際に選択状態へ反映します。
        ///     Registry にも SessionState にも手がかりが無い場合は、先頭の型を勝手に選んで
        ///     Get() を呼ぶ（＝触られてもいないデータを新規インスタンス化する）ことはせず、
        ///     Registry Cache にデータが現れるまで選択を行いません。
        /// </summary>
        private void ApplyAutoSelection(Type typeToSelect)
        {
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
        ///     自動選択の対象を決定します。Registry に既にインスタンス化済みのデータがあれば
        ///     （SessionState に前回選択が残っていればそれを優先しつつ）それを使い、
        ///     何もインスタンス化されていなければ null（自動選択しない）を返します。
        /// </summary>
        private Type ResolveAutoSelectType()
        {
            if (_cachedEntries.Count <= 0)
            {
                return null;
            }

            Type sessionType = RestoreSelectedTypeFromSession();
            if (sessionType != null && _cachedEntries.Any(entry => entry.DataType == sessionType))
            {
                return sessionType;
            }

            // ViewModel が公開する一覧は型名の昇順で並んでいる。
            return _cachedEntries[0].DataType;
        }

        /// <summary> 選択型を更新し、ドメインリロード後に復元できるようSessionStateへ保存する。 </summary>
        private void SetSelectedType(Type type)
        {
            if (_selectedType != type)
            {
                _editorScrollPosition = Vector2.zero;
            }

            _selectedType = type;
            SessionState.SetString(SELECTED_TYPE_SESSION_KEY, type?.AssemblyQualifiedName ?? string.Empty);
        }

        /// <summary> SessionStateから前回選択していたセーブデータ型を復元する。 </summary>
        private static Type RestoreSelectedTypeFromSession()
        {
            string typeName = SessionState.GetString(SELECTED_TYPE_SESSION_KEY, string.Empty);
            return string.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName);
        }

        /// <summary> キャッシュ一覧の要素生成、表示内容、選択イベントを構成する。 </summary>
        private void ConfigureCacheList()
        {
            _cacheListView.makeItem = () => new Label();
            _cacheListView.bindItem = (element, index) =>
            {
                SaveDataEntryRow row = _rows[index];
                string state = row.IsLoaded
                    ? "Loaded"
                    : row.IsSaved
                        ? "Saved"
                        : "Empty";

                ((Label)element).text = $"{row.DataType.FullName}\nState: {state} / Date: {row.SaveDate ?? "(unknown)"}";
            };
            _cacheListView.selectionType = SelectionType.Single;
            _cacheListView.selectionChanged += OnCacheSelectionChanged;
        }

        /// <summary> キャッシュ一覧で選択されたセーブデータ型を編集対象へ反映する。 </summary>
        private void OnCacheSelectionChanged(IEnumerable<object> selectedItems)
        {
            foreach (object selectedItem in selectedItems)
            {
                if (selectedItem is SaveDataEntryRow row)
                {
                    SelectType(row.DataType);
                }

                return;
            }
        }

        /// <summary> 有効なセーブデータ型を選択して現在のキャッシュへバインドする。 </summary>
        private void SelectType(Type type)
        {
            if (type == null || !_saveDataTypes.Contains(type) || type == _selectedType)
            {
                return;
            }

            SetSelectedType(type);
            BindCurrentSelection();
            RefreshView();
        }

        /// <summary> 選択中セーブデータをスクロール可能なInspectorとして描画する。 </summary>
        private void DrawEditorInspector()
        {
            _debugSerializedObject.Update();
            _editorScrollPosition = EditorGUILayout.BeginScrollView(
                _editorScrollPosition,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            try
            {
                SerializedProperty dataProperty = _debugSerializedObject.FindProperty("_data");
                if (dataProperty.managedReferenceValue == null)
                {
                    // SaveStore.Get() は必ず何らかのインスタンスを返すため、型が選択されていれば
                    // ここには到達しない。到達するのは (a) プロジェクトに SaveDataContent を継承した型が
                    // 一つも無い、または (b) まだ何もインスタンス化されておらず自動選択もしていない場合のみ。
                    // どちらの理由かは _statusMessage 側で出し分けているので、そのままここに表示する。
                    EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
                }
                else
                {
                    // SaveDataContent.SaveDate は [ReadOnly] なので、再帰描画の中で
                    // SaveDate だけが読み取り専用になり、派生クラスのフィールドは編集できる。
                    EditorGUILayout.PropertyField(dataProperty, new GUIContent("Data"), true);
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }

            _debugSerializedObject.ApplyModifiedProperties();
        }

        /// <summary> 選択型のレジストリ正本を一時編集状態へバインドする。 </summary>
        private void BindCurrentSelection()
        {
            if (_selectedType == null)
            {
                return;
            }

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
            RebindDebugState(data);
            _statusMessage = $"{_selectedType.FullName} の現在インスタンスを表示しています。";
        }

        /// <summary> 選択中の型を保存先から再ロードして編集状態へ反映する。 </summary>
        /// <returns> ロード完了までの待機を表すAwaitable。 </returns>
        private async Awaitable LoadSelectedAsync()
        {
            await SaveStore.LoadAsync(_selectedType);
            SaveDataContent saveData = SaveStore.Get(_selectedType);

            RebindDebugState(saveData);
            _statusMessage = $"{_selectedType.FullName} をロードしました。";
            RefreshView();
        }

        /// <summary> Inspectorの編集内容をレジストリ正本へ同期して保存する。 </summary>
        /// <returns> 保存完了までの待機を表すAwaitable。 </returns>
        private async Awaitable SaveSelectedAsync()
        {
            // 保存対象はRegistryの正本であり、未読み込みのまま保存すると
            // 既定値で保存先を上書きしてしまう。先にロードして正本を確定させる。
            if (!SaveStore.IsLoaded(_selectedType))
            {
                await SaveStore.LoadAsync(_selectedType);
            }

            SaveDataContent editingData = _debugState.GetData();
            if (editingData == null)
            {
                BindCurrentSelection();
                editingData = _debugState.GetData();
            }

            // インスペクタで編集中のインスタンスが [SerializeReference] の再構築などで
            // Registry のキャッシュ本体と別インスタンスになっている可能性があるため、
            // 保存前に編集内容を Registry 側の正本へ同期する（食い違ったまま Save してしまう事故を防ぐ）。
            SaveDataContent canonical = SaveStore.Get(_selectedType);
            if (!ReferenceEquals(canonical, editingData))
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(editingData), canonical);
            }

            await SaveStore.SaveAsync(_selectedType);
            SaveDataContent saveData = SaveStore.Get(_selectedType);
            RebindDebugState(saveData);
            _statusMessage = $"{_selectedType.FullName} を保存しました。";
            RefreshView();
        }

        /// <summary> 確認後に選択型の保存データを削除し、現在インスタンスを初期化する。 </summary>
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

            await SaveStore.DeleteAsync(_selectedType);
            SaveDataContent regenerated = SaveStore.Get(_selectedType);
            RebindDebugState(regenerated);
            _statusMessage = $"{_selectedType.FullName} の保存データを削除し、現在インスタンスを初期化しました。";
            RefreshView();
        }

        /// <summary>
        ///     デバッグインスペクタが表示するインスタンスを Registry の正本に合わせて再バインドします。
        ///     [SerializeReference] フィールドは参照先の入れ替えを SerializedObject.Update() だけでは
        ///     確実に検知できない場合があるため、SerializedObject 自体を作り直して確実に反映します。
        /// </summary>
        private void RebindDebugState(SaveDataContent data)
        {
            if (_disposed)
            {
                return;
            }

            _debugSerializedObject?.Dispose();
            _debugState.SetData(data);
            _debugSerializedObject = new SerializedObject(_debugState);
        }

        /// <summary>
        ///     選択状態を検証し、管理パネル操作中の例外をステータス表示へ変換する。
        ///     **Awaitableを同期待機するとEditorのメインスレッドが止まるため、非同期で実行する。**
        ///     UIイベントからの呼び出しであり例外はここで捕捉するため、async voidでよい。
        /// </summary>
        /// <param name="operation"> 実行する非同期操作。 </param>
        private async void ExecuteActionAsync(Func<Awaitable> operation)
        {
            if (_selectedType == null)
            {
                _statusMessage = "Registry Cache からセーブデータを選択してください。";
                RefreshView();
                return;
            }

            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                if (_disposed)
                {
                    return;
                }

                Debug.LogException(ex);
                _statusMessage = ex.Message;
                RefreshView();
            }
        }

        /// <summary>
        ///     管理パネルの表示を最新の行一覧へ更新する。
        ///     呼び出しはViewModelからの通知と、パネル上の操作の直後だけに限られる。
        /// </summary>
        private void RefreshView()
        {
            if (_disposed || _cacheListView == null)
            {
                return;
            }

            _currentLoaderLabel.text = $"Current Loader: {GetCurrentLoaderName()}";
            _loadedEntriesCountLabel.text = $"Visible Entries: {_rows.Count}";
            _statusLabel.text = _statusMessage;
            _cacheListView.itemsSource = _rows;
            _cacheListView.Rebuild();
            SyncCacheSelection();

            _editorContainer.MarkDirtyRepaint();
        }

        /// <summary> 現在選択されているローダーの型名を取得する。 </summary>
        /// <returns> ローダーの型名。未初期化の場合は代替表示。 </returns>
        private static string GetCurrentLoaderName()
        {
            return SaveStore.IsInitialized
                ? SaveStore.GetCurrentLoader().GetType().Name
                : "(uninitialized)";
        }

        /// <summary> 現在の選択型に対応する一覧行を通知なしで選択状態へ同期する。 </summary>
        private void SyncCacheSelection()
        {
            int selectedEntryIndex = -1;
            for (int index = 0; index < _rows.Count; index++)
            {
                if (_rows[index].DataType == _selectedType)
                {
                    selectedEntryIndex = index;
                    break;
                }
            }

            if (selectedEntryIndex < 0)
            {
                _cacheListView.SetSelectionWithoutNotify(Array.Empty<int>());
                return;
            }

            _cacheListView.SetSelectionWithoutNotify(new[] { selectedEntryIndex });
        }

        /// <summary>
        ///     対応型の順序に揃えた一覧行を組み立てる。
        ///     キャッシュ済みの型はViewModelのDtoから、そうでない型は永続化データが
        ///     存在する場合だけ行にする。
        /// </summary>
        /// <returns> 表示順に並んだ行一覧。 </returns>
        private List<SaveDataEntryRow> BuildRows()
        {
            Dictionary<Type, SaveDataDto> cachedEntries = _cachedEntries
                .ToDictionary(entry => entry.DataType);

            List<SaveDataEntryRow> rows = new(_saveDataTypes.Count);
            foreach (Type saveDataType in _saveDataTypes)
            {
                bool isSaved = SaveStore.IsInitialized
                    && SaveStore.Exists(saveDataType);

                if (cachedEntries.TryGetValue(saveDataType, out SaveDataDto cachedEntry))
                {
                    rows.Add(new SaveDataEntryRow(
                        saveDataType,
                        cachedEntry.SaveDate,
                        cachedEntry.IsLoaded,
                        isSaved));
                    continue;
                }

                if (isSaved)
                {
                    rows.Add(new SaveDataEntryRow(saveDataType, null, false, true));
                }
            }

            return rows;
        }

        /// <summary> 一部の型をロードできないAssemblyからも取得可能な型だけを列挙する。 </summary>
        private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
        }

        /// <summary> 管理パネルで生成・編集できるセーブデータ具象型か検証する。 </summary>
        private static bool IsSupportedSaveDataType(Type type)
        {
            if (type == null
                || !type.IsClass
                || type.IsAbstract
                || type.IsGenericTypeDefinition
                || typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return false;
            }

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                return false;
            }

            if (!typeof(SaveDataContent).IsAssignableFrom(type))
            {
                return false;
            }

            return type.IsDefined(typeof(SerializableAttribute), false);
        }

    }
}
