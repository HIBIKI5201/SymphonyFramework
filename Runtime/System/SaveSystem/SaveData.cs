using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    [Serializable]
    public class SaveData<T> : IDisposable
    {
        public SaveData(T dataType, DateTime saveDate = default)
        {
            if (saveDate == default) { saveDate = DateTime.Now; }

            SaveDate = saveDate.ToString("O");
            MainData = dataType;
        }

        public string SaveDate { get; set; }
        public T MainData { get; set; }

        public void Dispose()
        {
            if (MainData is IDisposable disposable) { disposable.Dispose(); }

            SaveDate = null;
            MainData = default;
        }

        public static bool operator ==(SaveData<T> a, SaveData<T> b)
        {
            if (ReferenceEquals(a, null)) { return ReferenceEquals(b, null); }

            if (a.MainData == null) { return true; }

            return ReferenceEquals(a, b);
        }

        public static bool operator !=(SaveData<T> a, SaveData<T> b) => !(a == b);

        public override bool Equals(object obj) => this == obj as SaveData<T>;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"SaveDate: {SaveDate}\nMainData:\n{MainData}";
    }
}
