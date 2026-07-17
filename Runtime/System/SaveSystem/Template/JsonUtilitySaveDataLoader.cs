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

        public ValueTask<SaveData<object>> LoadAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            token.ThrowIfCancellationRequested();

            string json = PlayerPrefs.GetString(GetKey(dataType));
            if (string.IsNullOrEmpty(json))
            {
                object instance = Activator.CreateInstance(dataType);
                Debug.Log($"[{nameof(JsonUtilitySaveDataLoader)}]\n{dataType.Name} のデータが見つからないので生成しました。");
                return new(new SaveData<object>(instance));
            }

            SaveDataWrapper data = JsonUtility.FromJson<SaveDataWrapper>(json);
            if (data == null)
            {
                Debug.LogWarning($"[{nameof(JsonUtilitySaveDataLoader)}]\n{dataType.Name} のロードに失敗しました。新たなインスタンスを生成します。");
                return new(new SaveData<object>(Activator.CreateInstance(dataType)));
            }

            object result = string.IsNullOrEmpty(data.MainDataJson)
                ? Activator.CreateInstance(dataType)
                : JsonUtility.FromJson(data.MainDataJson, dataType);

            return new(new SaveData<object>(result, ParseSaveDate(data.SaveDate)));
        }

        public ValueTask<SaveData<object>> SaveAsync(Type dataType, object data, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            token.ThrowIfCancellationRequested();

            SaveData<object> saveData = new(data);
            SaveDataWrapper wrapper = new()
            {
                SaveDate = saveData.SaveDate,
                MainDataJson = JsonUtility.ToJson(data),
            };

            PlayerPrefs.SetString(GetKey(dataType), JsonUtility.ToJson(wrapper));
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

        [Serializable]
        private class SaveDataWrapper
        {
            public string SaveDate;
            public string MainDataJson;
        }
    }
}
