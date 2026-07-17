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

        public ValueTask LoadAsync(Type dataType, SaveDataContent data, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            ValidateDataInstance(dataType, data);
            token.ThrowIfCancellationRequested();

            string json = PlayerPrefs.GetString(GetKey(dataType));
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log($"[{nameof(JsonUtilitySaveDataLoader)}]\n{dataType.Name} のデータが見つからないので生成しました。");
                OverwriteWithDefault(dataType, data);
                return default;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(json, data);
                return default;
            }
            catch (Exception)
            {
                Debug.LogWarning($"[{nameof(JsonUtilitySaveDataLoader)}]\n{dataType.Name} の本体データ復元に失敗しました。新たなインスタンスを生成します。");
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
            PlayerPrefs.SetString(GetKey(dataType), JsonUtility.ToJson(data, true));
            PlayerPrefs.Save();

            Debug.Log($"[{nameof(JsonUtilitySaveDataLoader)}]\nデータをセーブしました。 date : {data.SaveDate}\n{data}");
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
            string defaultJson = JsonUtility.ToJson(Activator.CreateInstance(dataType), true);
            JsonUtility.FromJsonOverwrite(defaultJson, target);
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
