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
        where T : SaveDataContent, new()
    {
        private static readonly NewtonsoftSaveDataLoader s_Loader = new();

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
            await s_Loader.SaveAsync(typeof(T), data);
            return data;
        }

        private static async ValueTask<T> LoadInternalAsync()
        {
            T data = new();
            await s_Loader.LoadAsync(typeof(T), data);
            return data;
        }
    }
}
