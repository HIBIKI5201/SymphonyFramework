using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    public readonly struct SaveDataRegistryEntryInfo
    {
        public SaveDataRegistryEntryInfo(Type dataType, string saveDate, SaveDataContent data)
        {
            DataType = dataType;
            SaveDate = saveDate;
            Data = data;
        }

        public Type DataType { get; }
        public string SaveDate { get; }
        public SaveDataContent Data { get; }
    }
}
