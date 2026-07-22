using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary> レジストリにキャッシュされたセーブデータと型の組を表す。 </summary>
    public readonly struct SaveDataRegistryEntryInfo
    {
        /// <summary> セーブデータ型とキャッシュインスタンスから情報を生成する。 </summary>
        public SaveDataRegistryEntryInfo(Type dataType, SaveDataContent data)
        {
            DataType = dataType;
            Data = data;
        }

        /// <summary> キャッシュされたセーブデータの型。 </summary>
        public Type DataType { get; }

        /// <summary> キャッシュされているセーブデータ。 </summary>
        public SaveDataContent Data { get; }

        /// <summary> キャッシュされているセーブデータの最終保存日時。 </summary>
        public string SaveDate => Data?.SaveDate;
    }
}
