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
        public bool Exists<T>() where T : class, new()
        {
            return PlayerPrefs.HasKey(GetKey<T>());
        }

        public ValueTask<SaveData<T>> LoadAsync<T>(CancellationToken token = default) where T : class, new()
        {
            token.ThrowIfCancellationRequested();

            string json = PlayerPrefs.GetString(GetKey<T>());
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log($"[{nameof(NewtonsoftSaveDataLoader)}]\n{typeof(T).Name} のデータが見つからないので生成しました。");
                return new(new SaveData<T>(new T()));
            }

            SaveData<T> data = JsonConvert.DeserializeObject<SaveData<T>>(json);
            if (data == null)
            {
                Debug.LogWarning($"[{nameof(NewtonsoftSaveDataLoader)}]\n{typeof(T).Name} のロードに失敗しました。新たなインスタンスを生成します。");
                return new(new SaveData<T>(new T()));
            }

            return new(data);
        }

        public ValueTask<SaveData<T>> SaveAsync<T>(T data, CancellationToken token = default) where T : class, new()
        {
            token.ThrowIfCancellationRequested();

            SaveData<T> saveData = new(data);
            PlayerPrefs.SetString(GetKey<T>(), JsonConvert.SerializeObject(saveData));
            PlayerPrefs.Save();

            Debug.Log($"[{nameof(NewtonsoftSaveDataLoader)}]\nデータをセーブしました。 date : {saveData.SaveDate}\n{data}");
            return new(saveData);
        }

        public ValueTask DeleteAsync<T>(CancellationToken token = default) where T : class, new()
        {
            token.ThrowIfCancellationRequested();
            PlayerPrefs.DeleteKey(GetKey<T>());
            PlayerPrefs.Save();
            return default;
        }

        private static string GetKey<T>() => typeof(T).FullName;
    }
}
