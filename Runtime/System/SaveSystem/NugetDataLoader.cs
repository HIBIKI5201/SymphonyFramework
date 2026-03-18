using Newtonsoft.Json;
using SymphonyFrameWork.System;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    public class NugetDataLoader<T> : ISaveDataLoader<T>
        where T : class, new()
    {
        public SaveData<T> Save(T data)
        {
            //Json化してセーブ
            SaveData<T> saveData = new SaveData<T>(data);
            string jsonData = JsonConvert.SerializeObject(saveData);
            PlayerPrefs.SetString(typeof(T).Name, jsonData);

            Debug.Log($"[{nameof(NugetDataLoader<T>)}]\nデータをセーブしました date : {saveData.SaveDate}\n{jsonData}");
            return saveData;
        }

        public SaveData<T> Load()
        {
            #region Prefsからデータをロードする

            var json = PlayerPrefs.GetString(typeof(T).Name);
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log($"[{nameof(NugetDataLoader<T>)}]\n{typeof(T).Name}のデータが見つからないので生成しました");
                return new SaveData<T>(new T());
            }

            #endregion

            #region JSONに変換して保存

            var data = JsonConvert.DeserializeObject<SaveData<T>>(json);
            if (data is not null)
            {
                Debug.Log($"[{nameof(NugetDataLoader<T>)}]\n{typeof(T).Name}のデータがロードされました\n{data}");
                return data;
            }
            else
            {
                Debug.LogWarning($"[{nameof(NugetDataLoader<T>)}]\n{typeof(T).Name}のロードが出来ませんでした\n新たなインスタンスを生成します");
                return new SaveData<T>(new T());
            }

            #endregion
        }
    }
}
