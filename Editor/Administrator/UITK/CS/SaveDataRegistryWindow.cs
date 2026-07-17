using SymphonyFrameWork.Core;
using SymphonyFrameWork.System.SaveSystem;
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
    public partial class SaveDataRegistryWindow : VisualElement
    {
        private readonly SaveDataDebugState _debugState;
        private readonly IMGUIContainer _imguiContainer;
        private SerializedObject _debugSerializedObject;
        private List<Type> _saveDataTypes = new();
        private int _selectedIndex = -1;
        private Type _selectedType;
        private string _statusMessage = "Type を選択して Load してください。";

        public SaveDataRegistryWindow()
        {
            AddToClassList("base");

            Label title = new() { text = "Save Data Registry" };
            title.AddToClassList("title");
            Add(title);

            _debugState = ScriptableObject.CreateInstance<SaveDataDebugState>();
            _debugState.hideFlags = HideFlags.HideAndDontSave;
            _debugSerializedObject = new SerializedObject(_debugState);

            _imguiContainer = new IMGUIContainer(DrawIMGUI);
            Add(_imguiContainer);

            RefreshTypeList();
        }

        private void DrawIMGUI()
        {
            EditorGUILayout.LabelField("Current Loader", SaveDataRegistry.GetCurrentLoader().GetType().Name);
            EditorGUILayout.LabelField("Loaded Entries", SaveDataRegistry.GetEntries().Count.ToString());
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Types"))
                {
                    RefreshTypeList();
                }

                if (GUILayout.Button("Refresh Loader"))
                {
                    SaveDataRegistry.RefreshLoader();
                    _statusMessage = "ローダー設定を再読み込みしました。";
                }
            }

            if (_saveDataTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("対象にできるセーブデータ型が見つかりませんでした。Serializable なクラスを確認してください。", MessageType.Warning);
                return;
            }

            string[] typeNames = _saveDataTypes.Select(type => type.FullName).ToArray();
            int newIndex = EditorGUILayout.Popup("Save Data Type", Mathf.Max(_selectedIndex, 0), typeNames);
            if (newIndex != _selectedIndex)
            {
                _selectedIndex = newIndex;
                _selectedType = _saveDataTypes[_selectedIndex];
                CreateNewInstanceForSelection();
            }

            DrawActionButtons();
            DrawEditorBody();
            DrawLoadedEntries();
        }

        private void DrawActionButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New"))
                {
                    CreateNewInstanceForSelection();
                }

                if (GUILayout.Button("Load"))
                {
                    ExecuteAction(LoadSelected);
                }

                if (GUILayout.Button("Save"))
                {
                    ExecuteAction(SaveSelected);
                }

                if (GUILayout.Button("Unload"))
                {
                    ExecuteAction(UnloadSelected);
                }

                if (GUILayout.Button("Delete"))
                {
                    ExecuteAction(DeleteSelected);
                }
            }
        }

        private void DrawEditorBody()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_statusMessage, MessageType.None);

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

        private void DrawLoadedEntries()
        {
            IReadOnlyList<SaveDataRegistryEntryInfo> entries = SaveDataRegistry.GetEntries();
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Registry Cache", EditorStyles.boldLabel);

            if (entries.Count == 0)
            {
                EditorGUILayout.LabelField("No loaded entries.");
                return;
            }

            foreach (SaveDataRegistryEntryInfo entry in entries.OrderBy(entry => entry.DataType.FullName))
            {
                EditorGUILayout.LabelField(entry.DataType.FullName, entry.SaveDate ?? "(unsaved)");
            }
        }

        private void RefreshTypeList()
        {
            _saveDataTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetTypesSafe)
                .Where(IsSupportedSaveDataType)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            if (_saveDataTypes.Count <= 0)
            {
                _selectedIndex = -1;
                _selectedType = null;
                _debugState.SetData(null, null);
                return;
            }

            if (_selectedType != null)
            {
                int existingIndex = _saveDataTypes.FindIndex(type => type == _selectedType);
                if (existingIndex >= 0)
                {
                    _selectedIndex = existingIndex;
                    return;
                }
            }

            _selectedIndex = 0;
            _selectedType = _saveDataTypes[0];
            CreateNewInstanceForSelection();
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
        }

        private void UnloadSelected()
        {
            SaveDataRegistry.Unload(_selectedType);
            _statusMessage = $"{_selectedType.FullName} をレジストリキャッシュから除外しました。";
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
        }

        private void ExecuteAction(Action action)
        {
            if (_selectedType == null)
            {
                _statusMessage = "Save Data Type が未選択です。";
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
            }
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
