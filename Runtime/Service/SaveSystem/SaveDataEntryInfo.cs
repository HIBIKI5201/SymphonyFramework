using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     キャッシュされたセーブデータの読み取り用スナップショットを表す。
    /// </summary>
    public readonly struct SaveDataEntryInfo
    {
        #region 外部向けAPI

        /// <summary>
        ///     セーブデータ型とキャッシュインスタンスから生成する。
        /// </summary>
        /// <remarks>
        ///     <see cref="SaveDataQuery"/>が生成し、利用側は<see cref="SaveStore.GetEntries"/>で取得する。
        /// </remarks>
        internal SaveDataEntryInfo(Type dataType, SaveDataContent data, bool isLoaded)
        {
            // Queryが取得した同一時点の型、内容、読み込み状態を一組として公開する。
            DataType = dataType;
            Data = data;
            IsLoaded = isLoaded;
        }

        /// <summary> キャッシュされたセーブデータの型。 </summary>
        public Type DataType { get; }

        /// <summary> キャッシュされているセーブデータ。 </summary>
        public SaveDataContent Data { get; }

        /// <summary> 永続化データを読み込み済みかどうか。 </summary>
        /// <remarks>
        ///     キャッシュは初回アクセス時に既定値で作られるため、<see cref="Data"/>の存在とは別に管理する。
        /// </remarks>
        public bool IsLoaded { get; }

        /// <summary> キャッシュされているセーブデータの最終保存日時。 </summary>
        public string SaveDate => Data?.SaveDate;

        #endregion
    }
}
