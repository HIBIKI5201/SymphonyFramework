using System.Threading;
using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータを管理する互換 API です。
    /// </summary>
    /// <typeparam name="TData">データの型</typeparam>
    /// <typeparam name="TLoader">ローダーの型</typeparam>
    public static class SaveSystem<TData, TLoader>
        where TData : SaveDataContent, new()
        where TLoader : ISaveDataLoader<TData>, new()
    {
        public static async ValueTask<TData> Get()
        {
            if (_saveData == null)
            {
                _saveData = await _loader.Load();
            }

            return _saveData;
        }

        public static async ValueTask<string> GetDate()
        {
            if (_saveData == null)
            {
                await Load();
            }

            return _saveData?.SaveDate;
        }

        /// <summary>
        ///     既存のジェネリックローダーを用いて保存します。
        /// </summary>
        public static async ValueTask Save()
        {
            TData data = await Get();
            _saveData = await _loader.Save(data);
        }

        /// <summary>
        ///     既存のジェネリックローダーを用いてロードします。
        /// </summary>
        public static async ValueTask Load()
        {
            _saveData = await _loader.Load();
        }

        /// <summary>
        ///     SaveDataRegistry 経由でロードします。
        /// </summary>
        public static async ValueTask LoadFromRegistry(CancellationToken token = default)
        {
            await SaveDataRegistry.LoadAsync<TData>(token);
            _saveData = SaveDataRegistry.Get<TData>();
        }

        /// <summary>
        ///     SaveDataRegistry 経由で保存します。
        /// </summary>
        public static async ValueTask SaveToRegistry(CancellationToken token = default)
        {
            _saveData = SaveDataRegistry.Get<TData>();
            await SaveDataRegistry.SaveAsync<TData>(token);
        }

        public static void Dispose()
        {
            if (_saveData == null) { return; }

            _saveData.Dispose();
            _saveData = null;
        }

        private static TData _saveData;
        private static readonly TLoader _loader = new();
    }
}
