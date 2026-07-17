using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     Newtonsoft.Json と PlayerPrefs を利用するセーブデータローダーです。
    /// </summary>
    [Serializable]
    public class NewtonsoftSaveDataLoader : ISaveDataLoader
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
                Debug.Log($"[{nameof(NewtonsoftSaveDataLoader)}]\n{dataType.Name} のデータが見つからないので生成しました。");
                return new(new SaveData<object>(instance));
            }

            SaveDataWrapper data = JsonConvert.DeserializeObject<SaveDataWrapper>(json);
            if (data == null)
            {
                Debug.LogWarning($"[{nameof(NewtonsoftSaveDataLoader)}]\n{dataType.Name} のロードに失敗しました。新たなインスタンスを生成します。");
                return new(new SaveData<object>(Activator.CreateInstance(dataType)));
            }

            object result = string.IsNullOrEmpty(data.MainDataJson)
                ? Activator.CreateInstance(dataType)
                : JsonConvert.DeserializeObject(data.MainDataJson, dataType);

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
                MainDataJson = JsonConvert.SerializeObject(data),
            };

            PlayerPrefs.SetString(GetKey(dataType), JsonConvert.SerializeObject(wrapper));
            PlayerPrefs.Save();

            Debug.Log($"[{nameof(NewtonsoftSaveDataLoader)}]\nデータをセーブしました。 date : {saveData.SaveDate}\n{data}");
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
