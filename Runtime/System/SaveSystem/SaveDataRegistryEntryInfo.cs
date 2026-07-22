using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    public readonly struct SaveDataRegistryEntryInfo
    {
        public SaveDataRegistryEntryInfo(Type dataType, SaveDataContent data)
        {
            DataType = dataType;
            Data = data;
        }

        public Type DataType { get; }
        public SaveDataContent Data { get; }
        public string SaveDate => Data?.SaveDate;
    }
}
