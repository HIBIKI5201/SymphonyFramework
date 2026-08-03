using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary> レジストリにキャッシュされたセーブデータと型の組を表す。 </summary>
    public readonly struct SaveDataRegistryEntryInfo
    {
        /// <summary>
        ///     セーブデータ型とキャッシュインスタンスから情報を生成する。
        ///     生成は<see cref="SaveDataQuery" />の責務であり、利用側は<see cref="SaveDataRegistry.GetEntries" />で取得する。
        /// </summary>
        internal SaveDataRegistryEntryInfo(Type dataType, SaveDataContent data, bool isLoaded)
        {
            DataType = dataType;
            Data = data;
            IsLoaded = isLoaded;
        }

        /// <summary> キャッシュされたセーブデータの型。 </summary>
        public Type DataType { get; }

        /// <summary> キャッシュされているセーブデータ。 </summary>
        public SaveDataContent Data { get; }

        /// <summary>
        ///     永続化データを読み込み済みかどうか。
        ///     <see cref="Data" />が存在することは読み込み済みであることを意味しない。
        ///     キャッシュは初回アクセス時に既定値で作られるため、両者は別の状態である。
        /// </summary>
        public bool IsLoaded { get; }

        /// <summary> キャッシュされているセーブデータの最終保存日時。 </summary>
        public string SaveDate => Data?.SaveDate;
    }
}
