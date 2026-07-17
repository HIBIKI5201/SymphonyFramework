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

        public ValueTask<SaveDataContent> LoadAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            token.ThrowIfCancellationRequested();

            string json = PlayerPrefs.GetString(GetKey(dataType));
            if (string.IsNullOrEmpty(json))
            {
                SaveDataContent created = (SaveDataContent)Activator.CreateInstance(dataType);
                Debug.Log($"[{nameof(JsonUtilitySaveDataLoader)}]\n{dataType.Name} のデータが見つからないので生成しました。");
                created.UpdateSaveDate();
                return new(created);
            }

            JsonUtilitySaveDataContainer container = JsonUtility.FromJson<JsonUtilitySaveDataContainer>(json);
            if (container == null)
            {
                SaveDataContent created = (SaveDataContent)Activator.CreateInstance(dataType);
                Debug.LogWarning($"[{nameof(JsonUtilitySaveDataLoader)}]\n{dataType.Name} のロードに失敗しました。新たなインスタンスを生成します。");
                created.UpdateSaveDate();
                return new(created);
            }

            SaveDataContent data = (SaveDataContent)JsonUtility.FromJson(container.MainDataJson, dataType);
            if (data == null)
            {
                data = (SaveDataContent)Activator.CreateInstance(dataType);
                Debug.LogWarning($"[{nameof(JsonUtilitySaveDataLoader)}]\n{dataType.Name} の本体データ復元に失敗しました。新たなインスタンスを生成します。");
                data.UpdateSaveDate();
                return new(data);
            }

            data.SaveDate = container.SaveDate;
            return new(data);
        }

        public ValueTask<SaveDataContent> SaveAsync(Type dataType, SaveDataContent data, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            ValidateDataInstance(dataType, data);
            token.ThrowIfCancellationRequested();

            data.UpdateSaveDate();
            JsonUtilitySaveDataContainer container = new()
            {
                SaveDate = data.SaveDate,
                MainDataJson = JsonUtility.ToJson(data, true)
            };

            PlayerPrefs.SetString(GetKey(dataType), JsonUtility.ToJson(container, true));
            PlayerPrefs.Save();

            Debug.Log($"[{nameof(JsonUtilitySaveDataLoader)}]\nデータをセーブしました。 date : {data.SaveDate}\n{data}");
            return new(data);
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
