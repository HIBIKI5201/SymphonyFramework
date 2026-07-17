using System.Threading.Tasks;
using System;
namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     JsonUtility を使ってデータをセーブする互換ラッパーです。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("Use JsonUtilitySaveDataLoader instead.")]
    public class JsonUtilityDataLoader<T> : ISaveDataLoader<T>
        where T : class, new()
    {
        private static readonly JsonUtilitySaveDataLoader s_Loader = new();

        public ValueTask<SaveData<T>> Save(T data)
        {
            return SaveInternalAsync(data);
        }

        public ValueTask<SaveData<T>> Load()
        {
            return LoadInternalAsync();
        }

        private static async ValueTask<SaveData<T>> SaveInternalAsync(T data)
        {
            return await s_Loader.SaveAsync(data);
        }

        private static async ValueTask<SaveData<T>> LoadInternalAsync()
        {
            return await s_Loader.LoadAsync<T>();
        }
    }
}
