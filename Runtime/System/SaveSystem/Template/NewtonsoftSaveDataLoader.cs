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

        public ValueTask LoadAsync(Type dataType, SaveDataContent data, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            ValidateDataInstance(dataType, data);
            token.ThrowIfCancellationRequested();

            string json = PlayerPrefs.GetString(GetKey(dataType));
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log($"[{nameof(NewtonsoftSaveDataLoader)}]\n{dataType.Name} のデータが見つからないので生成しました。");
                OverwriteWithDefault(dataType, data);
                return default;
            }

            try
            {
                OverwriteWithDefault(dataType, data);
                JsonConvert.PopulateObject(json, data);
                return default;
            }
            catch (Exception)
            {
                Debug.LogWarning($"[{nameof(NewtonsoftSaveDataLoader)}]\n{dataType.Name} のロードに失敗しました。新たなインスタンスを生成します。");
                OverwriteWithDefault(dataType, data);
                return default;
            }
        }

        public ValueTask SaveAsync(Type dataType, SaveDataContent data, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            ValidateDataInstance(dataType, data);
            token.ThrowIfCancellationRequested();

            data.UpdateSaveDate();
            PlayerPrefs.SetString(GetKey(dataType), JsonConvert.SerializeObject(data, Formatting.Indented));
            PlayerPrefs.Save();

            Debug.Log($"[{nameof(NewtonsoftSaveDataLoader)}]\nデータをセーブしました。 date : {data.SaveDate}\n{data}");
            return default;
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

        private static void OverwriteWithDefault(Type dataType, SaveDataContent target)
        {
            string defaultJson = JsonConvert.SerializeObject(Activator.CreateInstance(dataType), Formatting.Indented);
            JsonConvert.PopulateObject(defaultJson, target);
            target.ClearSaveDate();
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
    }
}
