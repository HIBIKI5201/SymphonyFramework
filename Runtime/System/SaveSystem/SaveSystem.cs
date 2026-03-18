using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータを管理するクラス
    /// </summary>
    /// <typeparam name="TData">データの型</typeparam>
    ///     /// <typeparam name="TLoader">ローダーの型</typeparam>
    public static class SaveSystem<TData, TLoader>
        where TData : class, new()
        where TLoader : ISaveDataLoader<TData>, new()
    {
        public static async ValueTask<TData> Get()
        {
            if (_saveData == null) 
            {
               await Load();
            }

            return _saveData?.MainData;
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
        ///     saveDataを保存する
        /// </summary>
        public static async ValueTask Save()
        {
            TData data = await Get();
            _saveData = await _loader.Save(data);
        }

        /// <summary>
        ///     DataTypeのデータを取得する
        /// </summary>
        public static async ValueTask Load()
        {
            _saveData = await _loader.Load();
        }

        public static void Dispose()
        {
            _saveData.Dispose();
            _saveData = null;
        }

        private static SaveData<TData> _saveData;
        private static readonly TLoader _loader = new();
    }
}