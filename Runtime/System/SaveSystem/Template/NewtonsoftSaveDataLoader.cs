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
                object created = Activator.CreateInstance(dataType);
                Debug.Log($"[{nameof(NewtonsoftSaveDataLoader)}]\n{dataType.Name} のデータが見つからないので生成しました。");
                return new(new SaveData<object>(created));
            }

            JsonSaveDataEnvelope envelope = JsonConvert.DeserializeObject<JsonSaveDataEnvelope>(json);
            if (envelope == null)
            {
                object created = Activator.CreateInstance(dataType);
                Debug.LogWarning($"[{nameof(NewtonsoftSaveDataLoader)}]\n{dataType.Name} のロードに失敗しました。新たなインスタンスを生成します。");
                return new(new SaveData<object>(created));
            }

            object mainData = string.IsNullOrEmpty(envelope.MainDataJson)
                ? Activator.CreateInstance(dataType)
                : JsonConvert.DeserializeObject(envelope.MainDataJson, dataType);

            return new(new SaveData<object>(mainData, ParseSaveDate(envelope.SaveDate)));
        }

        public ValueTask<SaveData<object>> SaveAsync(Type dataType, object data, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            ValidateDataInstance(dataType, data);
            token.ThrowIfCancellationRequested();

            SaveData<object> saveData = new(data);
            JsonSaveDataEnvelope envelope = new()
            {
                SaveDate = saveData.SaveDate,
                MainDataJson = JsonConvert.SerializeObject(data, Formatting.Indented)
            };

            PlayerPrefs.SetString(GetKey(dataType), JsonConvert.SerializeObject(envelope, Formatting.Indented));
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

        private static void ValidateDataInstance(Type dataType, object data)
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
        private sealed class JsonSaveDataEnvelope
        {
            public string SaveDate;
            public string MainDataJson;
        }
    }
}
