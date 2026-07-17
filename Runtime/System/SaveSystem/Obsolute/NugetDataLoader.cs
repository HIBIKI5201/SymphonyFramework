using System.Threading.Tasks;
using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     Json.NET を使ってデータをセーブする互換ラッパーです。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("Use NewtonsoftSaveDataLoader instead.")]
    public class NugetDataLoader<T> : ISaveDataLoader<T>
        where T : class, new()
    {
        private static readonly NewtonsoftSaveDataLoader s_Loader = new();

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
