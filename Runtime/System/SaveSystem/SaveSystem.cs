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
        public static TData Data
        {
            get
            {
                if (_saveData == null) { Load(); }
                return _saveData?.MainData;
            }
        }

        public static string SaveDate
        {
            get
            {
                if (_saveData == null) { Load(); }
                return _saveData?.SaveDate;
            }
        }

        public static void Dispose()
        {
            _saveData.Dispose();
            _saveData = null;
        }

        private static SaveData<TData> _saveData;
        private static readonly TLoader _loader = new();

        /// <summary>
        ///     saveDataを保存する
        /// </summary>
        public static void Save()
        {
            _saveData = _loader.Save(Data);
        }

        /// <summary>
        ///     DataTypeのデータを取得する
        /// </summary>
        private static void Load()
        {
            _saveData = _loader.Load();
        }
    }
}