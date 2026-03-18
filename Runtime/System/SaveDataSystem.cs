using System;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     セーブデータを管理するクラス
    /// </summary>
    /// <typeparam name="DataType">データの型</typeparam>
    public static class SaveDataSystem<DataType, LoaderType>
        where DataType : class, new()
        where LoaderType : SaveDataLoader<DataType>, new()
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

    [Serializable]
    public class SaveData<T>
    {
        public SaveData(T dataType, DateTime saveDate = default)
        {
            if (saveDate == default) { saveDate = DateTime.Now; }

            SaveDate = saveDate.ToString("O");
            MainData = dataType;
        }

        public string SaveDate { get; set; }
        public T MainData { get; set; }

        public static bool operator ==(SaveData<T> a, SaveData<T> b)
        {
            if (ReferenceEquals(a, null)) { return ReferenceEquals(b, null); }

            if (a.MainData == null) { return true; }

            return ReferenceEquals(a, b);
        }

        public static bool operator !=(SaveData<T> a, SaveData<T> b) => !(a == b);

        public override bool Equals(object obj) => this == obj as SaveData<T>;

        public override string ToString()
        {
            return $"SaveDate: {SaveDate}\nMainData:\n{MainData}";
        }
    }
}