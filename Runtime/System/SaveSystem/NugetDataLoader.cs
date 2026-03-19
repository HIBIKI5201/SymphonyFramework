using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    public class NugetDataLoader<T> : ISaveDataLoader<T>
        where T : class, new()
    {
        public ValueTask<SaveData<T>> Save(T data)
        {
            //Json化してセーブ。
            SaveData<T> saveData = new SaveData<T>(data);
            string jsonData = JsonConvert.SerializeObject(saveData);
            PlayerPrefs.SetString(typeof(T).FullName, jsonData);

            Debug.Log($"[{nameof(NugetDataLoader<T>)}]\nデータをセーブしました date : {saveData.SaveDate}\n{data}");
            return new(saveData);
        }

        public ValueTask<SaveData<T>> Load()
        {
            #region Prefsからデータをロードする

            var json = PlayerPrefs.GetString(typeof(T).FullName);
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log($"[{nameof(NugetDataLoader<T>)}]\n{typeof(T).Name}のデータが見つからないので生成しました");
                return new(new SaveData<T>(new T()));
            }

            #endregion

            #region JSONに変換して返す

            var data = JsonConvert.DeserializeObject<SaveData<T>>(json);
            if (data == null)
            {
                Debug.Log($"[{nameof(NugetDataLoader<T>)}]\n{typeof(T).Name}のデータがロードされました\n{data}");
                return new(data);
            }
            else
            {
                Debug.LogWarning($"[{nameof(NugetDataLoader<T>)}]\n{typeof(T).Name}のロードが出来ませんでした\n新たなインスタンスを生成します");
                return new(new SaveData<T>(new T()));
            }

            #endregion
        }
    }
}
