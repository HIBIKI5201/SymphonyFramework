namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータを管理するクラス
    /// </summary>
    /// <typeparam name="DataType">データの型</typeparam>
    public static class SaveSystem<DataType, LoaderType>
        where DataType : class, new()
        where LoaderType : ISaveDataLoader<DataType>, new()
    {
        public static DataType Data
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
            _saveData = null;
        }

        private static SaveData<DataType> _saveData;
        private static readonly LoaderType _loader = new();

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