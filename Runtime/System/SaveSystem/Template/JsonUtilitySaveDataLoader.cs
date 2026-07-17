using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     JsonUtility と PlayerPrefs を利用するセーブデータローダーです。
    /// </summary>
    [Serializable]
    public class JsonUtilitySaveDataLoader : ISaveDataLoader
    {
        public bool Exists(Type dataType)
        {
            ValidateDataType(dataType);
            return PlayerPrefs.HasKey(GetKey(dataType));
        }

        public ValueTask<SaveData> LoadAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            token.ThrowIfCancellationRequested();

            string json = PlayerPrefs.GetString(GetKey(dataType));
            if (string.IsNullOrEmpty(json))
            {
                SaveDataContent created = (SaveDataContent)Activator.CreateInstance(dataType);
                Debug.Log($"[{nameof(JsonUtilitySaveDataLoader)}]\n{dataType.Name} のデータが見つからないので生成しました。");
                return new(new SaveData(created));
            }

            JsonUtilitySaveDataContainer container = JsonUtility.FromJson<JsonUtilitySaveDataContainer>(json);
            if (container == null)
            {
                SaveDataContent created = (SaveDataContent)Activator.CreateInstance(dataType);
                Debug.LogWarning($"[{nameof(JsonUtilitySaveDataLoader)}]\n{dataType.Name} のロードに失敗しました。新たなインスタンスを生成します。");
                return new(new SaveData(created));
            }

            SaveDataContent data = (SaveDataContent)JsonUtility.FromJson(container.MainDataJson, dataType);
            if (data == null)
            {
                data = (SaveDataContent)Activator.CreateInstance(dataType);
                Debug.LogWarning($"[{nameof(JsonUtilitySaveDataLoader)}]\n{dataType.Name} の本体データ復元に失敗しました。新たなインスタンスを生成します。");
            }

            return new(new SaveData(data, ParseSaveDate(container.SaveDate)));
        }

        public ValueTask<SaveData> SaveAsync(Type dataType, SaveDataContent data, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            ValidateDataInstance(dataType, data);
            token.ThrowIfCancellationRequested();

            SaveData saveData = new(data);
            JsonUtilitySaveDataContainer container = new()
            {
                SaveDate = saveData.SaveDate,
                MainDataJson = JsonUtility.ToJson(data, true)
            };

            PlayerPrefs.SetString(GetKey(dataType), JsonUtility.ToJson(container, true));
            PlayerPrefs.Save();

            Debug.Log($"[{nameof(JsonUtilitySaveDataLoader)}]\nデータをセーブしました。 date : {saveData.SaveDate}\n{data}");
            return new(saveData);
        }

        public ValueTask DeleteAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            token.ThrowIfCancellationRequested();
            PlayerPrefs.DeleteKey(GetKey(dataType));
            PlayerPrefs.Save();
            return default;
        }

        private static string GetKey(Type dataType) => dataType.FullName;

        private static DateTime ParseSaveDate(string saveDate)
        {
            return DateTime.TryParse(saveDate, out DateTime parsed)
                ? parsed
                : default;
        }

        private static void ValidateDataType(Type dataType)
        {
            if (dataType == null)
            {
                throw new ArgumentNullException(nameof(dataType));
            }
        }

        private static void ValidateDataInstance(Type dataType, SaveDataContent data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!dataType.IsInstanceOfType(data))
            {
                throw new ArgumentException($"{dataType.Name} のインスタンスを指定してください。", nameof(data));
            }
        }

        [Serializable]
        private sealed class JsonUtilitySaveDataContainer
        {
            public string SaveDate;
            public string MainDataJson;
        }
    }
}
