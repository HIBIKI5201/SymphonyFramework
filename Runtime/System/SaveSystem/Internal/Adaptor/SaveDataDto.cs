using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary> セーブデータ登録の表示用更新値。 </summary>
    internal readonly struct SaveDataDto : IEquatable<SaveDataDto>
    {
        /// <summary>
        ///     表示に必要な値を指定して生成する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="dataTypeName"> 表示用の型名。 </param>
        /// <param name="saveDate"> 最終保存日時。未保存の場合はnull。 </param>
        /// <param name="isLoaded"> 永続化データを読み込み済みかどうか。 </param>
        internal SaveDataDto(
            Type dataType,
            string dataTypeName,
            string saveDate,
            bool isLoaded)
        {
            DataType = dataType;
            DataTypeName = dataTypeName;
            SaveDate = saveDate;
            IsLoaded = isLoaded;
        }

        /// <summary>
        ///     対象のセーブデータ型。
        ///     Editorが選択行に対してロードや保存を実行するため、型そのものを渡す。
        /// </summary>
        internal Type DataType { get; }

        /// <summary> 表示用の型名。 </summary>
        internal string DataTypeName { get; }

        /// <summary> 最終保存日時。 </summary>
        internal string SaveDate { get; }

        /// <summary> 永続化データを読み込み済みかどうか。 </summary>
        internal bool IsLoaded { get; }

        /// <summary> 表示値が等しいか判定する。 </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> 全ての表示値が等しい場合はtrue。 </returns>
        public bool Equals(SaveDataDto other)
        {
            return DataType == other.DataType
                && string.Equals(DataTypeName, other.DataTypeName, StringComparison.Ordinal)
                && string.Equals(SaveDate, other.SaveDate, StringComparison.Ordinal)
                && IsLoaded == other.IsLoaded;
        }

        /// <summary> 表示値が等しいか判定する。 </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同じ型で全ての表示値が等しい場合はtrue。 </returns>
        public override bool Equals(object obj) => obj is SaveDataDto other && Equals(other);

        /// <summary> 表示値に基づくハッシュコードを返す。 </summary>
        /// <returns> ハッシュコード。 </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = DataType?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (DataTypeName?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (SaveDate?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ IsLoaded.GetHashCode();
                return hashCode;
            }
        }

        /// <summary> 2つの表示値が等しいか判定する。 </summary>
        /// <param name="left"> 左辺。 </param>
        /// <param name="right"> 右辺。 </param>
        /// <returns> 等しい場合はtrue。 </returns>
        public static bool operator ==(SaveDataDto left, SaveDataDto right) => left.Equals(right);

        /// <summary> 2つの表示値が異なるか判定する。 </summary>
        /// <param name="left"> 左辺。 </param>
        /// <param name="right"> 右辺。 </param>
        /// <returns> 異なる場合はtrue。 </returns>
        public static bool operator !=(SaveDataDto left, SaveDataDto right) => !left.Equals(right);
    }
}
