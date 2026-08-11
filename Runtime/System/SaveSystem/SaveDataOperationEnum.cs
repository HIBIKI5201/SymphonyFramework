namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータに対して実行した操作の種類を表す。
    /// </summary>
    public enum SaveDataOperationEnum
    {
        #region 外部向けAPI

        /// <summary> 永続化データの存在確認。 </summary>
        Exists,

        /// <summary> 永続化データの読み込み。 </summary>
        Load,

        /// <summary> 永続化データの保存。 </summary>
        Save,

        /// <summary> 永続化データの削除。 </summary>
        Delete

        #endregion
    }
}
