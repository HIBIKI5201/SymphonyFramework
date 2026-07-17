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
        private readonly SaveDataDebugState _debugState;
        private SerializedObject _debugSerializedObject;
        private List<Type> _saveDataTypes = new();
        private int _selectedIndex = -1;
        private Type _selectedType;
        private string _statusMessage = "Type を選択して Load してください。";

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

            root.Q<Button>("save-refresh-types").clicked += RefreshTypeList;
            root.Q<Button>("save-refresh-loader").clicked += RefreshLoader;
            root.Q<Button>("save-new").clicked += () => ExecuteAction(CreateNewInstanceForSelection);
            root.Q<Button>("save-load").clicked += () => ExecuteAction(LoadSelected);
            root.Q<Button>("save-save").clicked += () => ExecuteAction(SaveSelected);
            root.Q<Button>("save-unload").clicked += () => ExecuteAction(UnloadSelected);
            root.Q<Button>("save-delete").clicked += () => ExecuteAction(DeleteSelected);

            _typeDropdown.RegisterValueChangedCallback(OnTypeChanged);
            _editorContainer.onGUIHandler = DrawEditorInspector;

            ConfigureCacheList();
            RefreshTypeList();
            RefreshView();

            return default;
        }

        private void ConfigureCacheList()
        {
            _cacheListView.makeItem = () => new Label();
            _cacheListView.bindItem = (element, index) =>
            {
                IReadOnlyList<SaveDataRegistryEntryInfo> entries = GetSortedEntries();
                SaveDataRegistryEntryInfo entry = entries[index];
                ((Label)element).text = $"{entry.DataType.FullName}\n{entry.SaveDate ?? "(unsaved)"}";
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
            _selectedType = _saveDataTypes[_selectedIndex];
            CreateNewInstanceForSelection();
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
                EditorGUILayout.HelpBox("現在編集中のデータはありません。New または Load を押してください。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(dataProperty, new GUIContent("Data"), true);
            }

            _debugSerializedObject.ApplyModifiedProperties();
        }

        private void RefreshTypeList()
        {
            _saveDataTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetTypesSafe)
                .Where(IsSupportedSaveDataType)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            List<string> choices = _saveDataTypes
                .Select(type => type.FullName)
                .ToList();

            _typeDropdown.choices = choices;

            if (_saveDataTypes.Count <= 0)
            {
                _selectedIndex = -1;
                _selectedType = null;
                _debugState.SetData(null, null);
                _typeDropdown.value = string.Empty;
                _statusMessage = "対象にできるセーブデータ型が見つかりませんでした。";
                RefreshView();
                return;
            }

            if (_selectedType != null)
            {
                int existingIndex = _saveDataTypes.FindIndex(type => type == _selectedType);
                if (existingIndex >= 0)
                {
                    _selectedIndex = existingIndex;
                    _typeDropdown.SetValueWithoutNotify(_saveDataTypes[_selectedIndex].FullName);
                    RefreshView();
                    return;
                }
            }

            _selectedIndex = 0;
            _selectedType = _saveDataTypes[0];
            _typeDropdown.SetValueWithoutNotify(_selectedType.FullName);
            CreateNewInstanceForSelection();
            RefreshView();
        }

        private void RefreshLoader()
        {
            SaveDataRegistry.RefreshLoader();
            _statusMessage = "ローダー設定を再読み込みしました。";
            RefreshView();
        }

        private void CreateNewInstanceForSelection()
        {
            if (_selectedType == null)
            {
                return;
            }

            _debugState.SetData(Activator.CreateInstance(_selectedType), null);
            _debugSerializedObject.Update();
            _statusMessage = $"{_selectedType.FullName} の新規インスタンスを作成しました。";
        }

        private void LoadSelected()
        {
            SaveData<object> saveData = SaveDataRegistry
                .LoadSaveDataAsync(_selectedType)
                .GetAwaiter()
                .GetResult();

            _debugState.SetData(saveData.MainData, saveData.SaveDate);
            _debugSerializedObject.Update();
            _statusMessage = $"{_selectedType.FullName} をロードしました。";
            RefreshView();
        }

        private void SaveSelected()
        {
            object data = _debugState.GetData();
            if (data == null)
            {
                CreateNewInstanceForSelection();
                data = _debugState.GetData();
            }

            SaveDataRegistry.SaveAsync(_selectedType, data).GetAwaiter().GetResult();
            SaveData<object> saveData = SaveDataRegistry.LoadSaveDataAsync(_selectedType).GetAwaiter().GetResult();
            _debugState.SetData(saveData.MainData, saveData.SaveDate);
            _debugSerializedObject.Update();
            _statusMessage = $"{_selectedType.FullName} を保存しました。";
            RefreshView();
        }

        private void UnloadSelected()
        {
            SaveDataRegistry.Unload(_selectedType);
            _statusMessage = $"{_selectedType.FullName} をレジストリキャッシュから除外しました。";
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
            CreateNewInstanceForSelection();
            _statusMessage = $"{_selectedType.FullName} の保存データを削除しました。";
            RefreshView();
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
            _loadedEntriesCountLabel.text = $"Loaded Entries: {SaveDataRegistry.GetEntries().Count}";
            _statusLabel.text = _statusMessage;

            List<SaveDataRegistryEntryInfo> entries = GetSortedEntries();
            _cacheListView.itemsSource = entries;
            _cacheListView.Rebuild();
            _editorContainer.MarkDirtyRepaint();
        }

        private static List<SaveDataRegistryEntryInfo> GetSortedEntries()
        {
            return SaveDataRegistry.GetEntries()
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

            if (!type.IsDefined(typeof(SerializableAttribute), false))
            {
                return false;
            }

            string namespaceName = type.Namespace ?? string.Empty;
            return !namespaceName.StartsWith("System", StringComparison.Ordinal)
                && !namespaceName.StartsWith("Unity", StringComparison.Ordinal);
        }

        private sealed class SaveDataDebugState : ScriptableObject
        {
            [SerializeField]
            private string _saveDate;

            [SerializeReference]
            private object _data;

            public object GetData() => _data;

            public void SetData(object data, string saveDate)
            {
                _data = data;
                _saveDate = saveDate;
            }
        }
    }
}
