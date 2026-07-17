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
        where T : SaveDataContent, new()
    {
        private static readonly JsonUtilitySaveDataLoader s_Loader = new();

        public ValueTask<T> Save(T data)
        {
            return SaveInternalAsync(data);
        }

        public ValueTask<T> Load()
        {
            return LoadInternalAsync();
        }

        private static async ValueTask<T> SaveInternalAsync(T data)
        {
            return (T)await s_Loader.SaveAsync(typeof(T), data);
        }

        private static async ValueTask<T> LoadInternalAsync()
        {
            return (T)await s_Loader.LoadAsync(typeof(T));
        }
    }
}
