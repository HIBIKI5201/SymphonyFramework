using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     Nugetで導入したJson.NETを使ってデータをセーブするクラス。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NugetDataLoader<T> : ISaveDataLoader<T>
        where T : class, new()
    {
        /// <summary>
        ///     データをJson化してセーブする。
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ValueTask<SaveData<T>> Save(T data)
        {
            //Json化してセーブ。
            SaveData<T> saveData = new SaveData<T>(data);
            string jsonData = JsonConvert.SerializeObject(saveData);
            PlayerPrefs.SetString(typeof(T).FullName, jsonData); // 型のフルネームをキーにしてセーブする。

            Debug.Log($"[{nameof(NugetDataLoader<T>)}]\nデータをセーブしました。 date : {saveData.SaveDate}\n{data}");
            return new(saveData);
        }

        /// <summary>
        ///     データをロードする。
        ///     データがない場合は新しいインスタンスを生成する。
        /// </summary>
        /// <returns></returns>
        public ValueTask<SaveData<T>> Load()
        {
            #region Prefsからデータをロードする

            string json = PlayerPrefs.GetString(typeof(T).FullName);
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log($"[{nameof(NugetDataLoader<T>)}]\n{typeof(T).Name}のデータが見つからないので生成しました。");
                return new(new SaveData<T>(new T()));
            }

            #endregion

            #region JSONに変換して返す

            SaveData<T> data = JsonConvert.DeserializeObject<SaveData<T>>(json);

            if (data == null)
            {
                Debug.LogWarning($"[{nameof(NugetDataLoader<T>)}]\n{typeof(T).Name}のロードに失敗しました。\n新たなインスタンスを生成します。");
                return new(new SaveData<T>(new T()));
            }

            Debug.Log($"[{nameof(NugetDataLoader<T>)}]\n{typeof(T).Name}のデータがロードされました。\n{data}");
            return new(data);

            #endregion
        }
    }
}
