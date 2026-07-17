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

        public ValueTask<SaveDataContent> LoadAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            token.ThrowIfCancellationRequested();

            string json = PlayerPrefs.GetString(GetKey(dataType));
            if (string.IsNullOrEmpty(json))
            {
                SaveDataContent created = (SaveDataContent)Activator.CreateInstance(dataType);
                Debug.Log($"[{nameof(NewtonsoftSaveDataLoader)}]\n{dataType.Name} のデータが見つからないので生成しました。");
                created.UpdateSaveDate();
                return new(created);
            }

            JsonSaveDataEnvelope envelope = JsonConvert.DeserializeObject<JsonSaveDataEnvelope>(json);
            if (envelope == null)
            {
                SaveDataContent created = (SaveDataContent)Activator.CreateInstance(dataType);
                Debug.LogWarning($"[{nameof(NewtonsoftSaveDataLoader)}]\n{dataType.Name} のロードに失敗しました。新たなインスタンスを生成します。");
                created.UpdateSaveDate();
                return new(created);
            }

            SaveDataContent mainData = string.IsNullOrEmpty(envelope.MainDataJson)
                ? (SaveDataContent)Activator.CreateInstance(dataType)
                : (SaveDataContent)JsonConvert.DeserializeObject(envelope.MainDataJson, dataType);
            mainData.SaveDate = envelope.SaveDate;
            return new(mainData);
        }

        public ValueTask<SaveDataContent> SaveAsync(Type dataType, SaveDataContent data, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            ValidateDataInstance(dataType, data);
            token.ThrowIfCancellationRequested();

            data.UpdateSaveDate();
            JsonSaveDataEnvelope envelope = new()
            {
                SaveDate = data.SaveDate,
                MainDataJson = JsonConvert.SerializeObject(data, Formatting.Indented)
            };

            PlayerPrefs.SetString(GetKey(dataType), JsonConvert.SerializeObject(envelope, Formatting.Indented));
            PlayerPrefs.Save();

            Debug.Log($"[{nameof(NewtonsoftSaveDataLoader)}]\nデータをセーブしました。 date : {data.SaveDate}\n{data}");
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
        private sealed class JsonSaveDataEnvelope
        {
            public string SaveDate;
            public string MainDataJson;
        }
    }
}
