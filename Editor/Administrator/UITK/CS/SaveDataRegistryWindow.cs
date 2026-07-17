using SymphonyFrameWork.Core;
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
    [UxmlElement]
    public partial class SaveDataRegistryWindow : SymphonyVisualElement
    {
        private const string SELECTED_TYPE_SESSION_KEY = "SymphonyFrameWork.SaveDataRegistryWindow.SelectedTypeName";

        private readonly SaveDataDebugState _debugState;
        private SerializedObject _debugSerializedObject;
        private List<Type> _saveDataTypes = new();
        private int _selectedIndex = -1;
        private Type _selectedType;
        private string _statusMessage = "初期化中です…";

        private Label _currentLoaderLabel;
        private Label _loadedEntriesCountLabel;
        private Label _statusLabel;
        private DropdownField _typeDropdown;
        private IMGUIContainer _editorContainer;
        private ListView _cacheListView;

        public SaveDataRegistryWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "SaveDataRegistryWindow.uxml",
            InitializeType.None,
            LoadType.AssetDataBase)
        {
            _debugState = ScriptableObject.CreateInstance<SaveDataDebugState>();
            _debugState.hideFlags = HideFlags.HideAndDontSave;
            _debugSerializedObject = new SerializedObject(_debugState);
        }

        protected override ValueTask Initialize_S(VisualElement root)
        {
            _currentLoaderLabel = root.Q<Label>("save-current-loader");
            _loadedEntriesCountLabel = root.Q<Label>("save-loaded-entries");
            _statusLabel = root.Q<Label>("save-status");
            _typeDropdown = root.Q<DropdownField>("save-type-dropdown");
            _editorContainer = root.Q<IMGUIContainer>("save-editor");
            _cacheListView = root.Q<ListView>("save-cache-list");

            root.Q<Button>("save-load").clicked += () => ExecuteAction(LoadSelected);
            root.Q<Button>("save-save").clicked += () => ExecuteAction(SaveSelected);
            root.Q<Button>("save-delete").clicked += () => ExecuteAction(DeleteSelected);

            _typeDropdown.RegisterValueChangedCallback(OnTypeChanged);
            _editorContainer.onGUIHandler = DrawEditorInspector;

            ConfigureCacheList();
            RefreshTypeList();
            RefreshView();

            return default;
        }

        /// <summary>
        ///     SymphonyAdministrator.Update() から毎フレーム呼び出され、Registry の最新状態を表示に反映します。
        ///     PauseWindow / ServiceLocatorWindow と同じ「親から駆動される」方式に合わせています。
        /// </summary>
        public void Update()
        {
            TryAutoSelectFromRegistry();
            RefreshView();
        }

        /// <summary>
        ///     未選択のまま Registry に新たにデータが乗った場合（他のコードが Get/Load/Save した場合など）、
        ///     ここで初めて自動選択します。Get() を呼んで新規インスタンス化することはしません。
        /// </summary>
        private void TryAutoSelectFromRegistry()
        {
            if (_selectedType != null)
            {
                return;
            }

            Type typeToSelect = ResolveAutoSelectType();
            if (typeToSelect == null)
            {
                return;
            }

            ApplyAutoSelection(typeToSelect);
        }

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
            _typeDropdown.choices = _saveDataTypes.Select(type => type.FullName).ToList();

            if (_saveDataTypes.Count <= 0)
            {
                _selectedIndex = -1;
                _selectedType = null;
                RebindDebugState(null);
                _typeDropdown.value = string.Empty;
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
        ///     選択を行わずユーザー操作（Type 選択 or Load）を待ちます。
        /// </summary>
        private void ApplyAutoSelection(Type typeToSelect)
        {
            int index = typeToSelect == null
                ? -1
                : _saveDataTypes.FindIndex(type => type == typeToSelect);

            if (index < 0)
            {
                _selectedIndex = -1;
                _selectedType = null;
                _typeDropdown.SetValueWithoutNotify(string.Empty);
                RebindDebugState(null);
                _statusMessage = "Type を選択するか、既存のセーブデータを Load してください。";
                return;
            }

            _selectedIndex = index;
            SetSelectedType(_saveDataTypes[index]);
            _typeDropdown.SetValueWithoutNotify(_selectedType.FullName);
            BindCurrentSelection();
        }

        /// <summary>
        ///     自動選択の対象を決定します。Registry に既にインスタンス化済みのデータがあれば
        ///     （SessionState に前回選択が残っていればそれを優先しつつ）それを使い、
        ///     何もインスタンス化されていなければ null（自動選択しない）を返します。
        /// </summary>
        private static Type ResolveAutoSelectType()
        {
            IReadOnlyList<SaveDataRegistryEntryInfo> cachedEntries = SaveDataRegistry.GetEntries();
            if (cachedEntries.Count <= 0)
            {
                return null;
            }

            Type sessionType = RestoreSelectedTypeFromSession();
            if (sessionType != null && cachedEntries.Any(entry => entry.DataType == sessionType))
            {
                return sessionType;
            }

            return cachedEntries
                .Select(entry => entry.DataType)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .First();
        }

        private void SetSelectedType(Type type)
        {
            _selectedType = type;
            SessionState.SetString(SELECTED_TYPE_SESSION_KEY, type?.AssemblyQualifiedName ?? string.Empty);
        }

        private static Type RestoreSelectedTypeFromSession()
        {
            string typeName = SessionState.GetString(SELECTED_TYPE_SESSION_KEY, string.Empty);
            return string.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName);
        }

        private void ConfigureCacheList()
        {
            _cacheListView.makeItem = () => new Label();
            _cacheListView.bindItem = (element, index) =>
            {
                IReadOnlyList<SaveDataRegistryEntryInfo> entries = GetSortedEntries();
                SaveDataRegistryEntryInfo entry = entries[index];
                bool isLoaded = entry.Data != null;
                bool isSaved = SaveDataRegistry.Exists(entry.DataType);
                string state = isLoaded
                    ? "Loaded"
                    : isSaved
                        ? "Saved"
                        : "Empty";

                ((Label)element).text = $"{entry.DataType.FullName}\nState: {state} / Date: {entry.SaveDate ?? "(unknown)"}";
            };
            _cacheListView.selectionType = SelectionType.None;
        }

        private void OnTypeChanged(ChangeEvent<string> evt)
        {
            int newIndex = _saveDataTypes.FindIndex(type => type.FullName == evt.newValue);
            if (newIndex < 0 || newIndex == _selectedIndex)
            {
                return;
            }

            _selectedIndex = newIndex;
            SetSelectedType(_saveDataTypes[_selectedIndex]);
            BindCurrentSelection();
            RefreshView();
        }

        private void DrawEditorInspector()
        {
            _debugSerializedObject.Update();

            SerializedProperty saveDateProperty = _debugSerializedObject.FindProperty("_saveDate");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(saveDateProperty, new GUIContent("Save Date"));
            }

            SerializedProperty dataProperty = _debugSerializedObject.FindProperty("_data");
            if (dataProperty.managedReferenceValue == null)
            {
                // SaveDataRegistry.Get() は必ず何らかのインスタンスを返すため、型が選択されていれば
                // ここには到達しない。到達するのは (a) プロジェクトに SaveDataContent を継承した型が
                // 一つも無い、または (b) まだ何もインスタンス化されておらず自動選択もしていない場合のみ。
                // どちらの理由かは _statusMessage 側で出し分けているので、そのままここに表示する。
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(dataProperty, new GUIContent("Data"), true);
            }

            _debugSerializedObject.ApplyModifiedProperties();
        }

        private void RefreshTypeList()
        {
            EnsureTypeListCurrent();
            RefreshView();
        }

        private void BindCurrentSelection()
        {
            if (_selectedType == null)
            {
                return;
            }

            SaveDataContent data = SaveDataRegistry.Get(_selectedType);
            RebindDebugState(data);
            _statusMessage = $"{_selectedType.FullName} の現在インスタンスを表示しています。";
        }

        private void LoadSelected()
        {
            SaveDataRegistry.LoadAsync(_selectedType).GetAwaiter().GetResult();
            SaveDataContent saveData = SaveDataRegistry.Get(_selectedType);

            RebindDebugState(saveData);
            _statusMessage = $"{_selectedType.FullName} をロードしました。";
            RefreshView();
        }

        private void SaveSelected()
        {
            SaveDataContent editingData = _debugState.GetData();
            if (editingData == null)
            {
                BindCurrentSelection();
                editingData = _debugState.GetData();
            }

            // インスペクタで編集中のインスタンスが [SerializeReference] の再構築などで
            // Registry のキャッシュ本体と別インスタンスになっている可能性があるため、
            // 保存前に編集内容を Registry 側の正本へ同期する（食い違ったまま Save してしまう事故を防ぐ）。
            SaveDataContent canonical = SaveDataRegistry.Get(_selectedType);
            if (!ReferenceEquals(canonical, editingData))
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(editingData), canonical);
            }

            SaveDataRegistry.SaveAsync(_selectedType).GetAwaiter().GetResult();
            SaveDataContent saveData = SaveDataRegistry.Get(_selectedType);
            RebindDebugState(saveData);
            _statusMessage = $"{_selectedType.FullName} を保存しました。";
            RefreshView();
        }

        private void DeleteSelected()
        {
            if (!EditorUtility.DisplayDialog(
                    "Delete Save Data",
                    $"{_selectedType.FullName} の保存データを削除しますか？",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            SaveDataRegistry.DeleteAsync(_selectedType).GetAwaiter().GetResult();
            SaveDataContent regenerated = SaveDataRegistry.Get(_selectedType);
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
            _debugState.SetData(data, data?.SaveDate);
            _debugSerializedObject = new SerializedObject(_debugState);
        }

        private void ExecuteAction(Action action)
        {
            if (_selectedType == null)
            {
                _statusMessage = "Save Data Type が未選択です。";
                RefreshView();
                return;
            }

            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _statusMessage = ex.Message;
                RefreshView();
            }
        }

        private void RefreshView()
        {
            _currentLoaderLabel.text = $"Current Loader: {SaveDataRegistry.GetCurrentLoader().GetType().Name}";
            _loadedEntriesCountLabel.text = $"Visible Entries: {GetSortedEntries().Count}";
            _statusLabel.text = _statusMessage;

            List<SaveDataRegistryEntryInfo> entries = GetSortedEntries();
            _cacheListView.itemsSource = entries;
            _cacheListView.Rebuild();
            _editorContainer.MarkDirtyRepaint();
        }

        private List<SaveDataRegistryEntryInfo> GetSortedEntries()
        {
            Dictionary<Type, SaveDataRegistryEntryInfo> loadedEntries = SaveDataRegistry.GetEntries()
                .ToDictionary(entry => entry.DataType);

            List<SaveDataRegistryEntryInfo> entries = new(_saveDataTypes.Count);
            foreach (Type saveDataType in _saveDataTypes)
            {
                if (loadedEntries.TryGetValue(saveDataType, out SaveDataRegistryEntryInfo loadedEntry))
                {
                    entries.Add(loadedEntry);
                    continue;
                }

                if (SaveDataRegistry.Exists(saveDataType))
                {
                    entries.Add(new SaveDataRegistryEntryInfo(saveDataType, null));
                    continue;
                }
            }

            return entries
                .OrderBy(entry => entry.DataType.FullName, StringComparer.Ordinal)
                .ToList();
        }

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

        private sealed class SaveDataDebugState : ScriptableObject
        {
            [SerializeField]
            private string _saveDate;

            [SerializeReference]
            private SaveDataContent _data;

            public SaveDataContent GetData() => _data;

            public void SetData(SaveDataContent data, string saveDate)
            {
                _data = data;
                _saveDate = saveDate;
            }
        }
    }
}
