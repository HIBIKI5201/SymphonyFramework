using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    [Serializable]
    public class SaveData : IDisposable
    {
        public SaveData(SaveDataContent dataType, DateTime saveDate = default)
        {
            if (saveDate == default) { saveDate = DateTime.Now; }

            SaveDate = saveDate.ToString("O");
            MainData = dataType;
        }

        public string SaveDate;
        public SaveDataContent MainData;

        public void Dispose()
        {
            if (MainData is IDisposable disposable) { disposable.Dispose(); }

            SaveDate = null;
            MainData = null;
        }

        public static bool operator ==(SaveData a, SaveData b)
        {
            if (ReferenceEquals(a, null)) { return ReferenceEquals(b, null); }
            return ReferenceEquals(a, b);
        }

        public static bool operator !=(SaveData a, SaveData b) => !(a == b);

        public override bool Equals(object obj) => this == obj as SaveData;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"SaveDate: {SaveDate}\nMainData:\n{MainData}";
    }

    [Serializable]
    public class SaveData<T> : SaveData
        where T : SaveDataContent
    {
        public SaveData(T dataType, DateTime saveDate = default)
            : base(dataType, saveDate)
        {
        }

        public new T MainData
        {
            get => (T)base.MainData;
            set => base.MainData = value;
        }
    }
}
