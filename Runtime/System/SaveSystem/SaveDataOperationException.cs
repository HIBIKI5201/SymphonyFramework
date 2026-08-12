using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータ操作のI/O失敗を表す。
    /// </summary>
    public sealed class SaveDataOperationException : Exception
    {
        #region 外部向けAPI

        /// <summary>
        ///     失敗した操作と対象を指定して生成する。
        /// </summary>
        /// <param name="operation"> 失敗した操作。 </param>
        /// <param name="dataType"> 操作対象のセーブデータ型。 </param>
        /// <param name="loaderType"> 操作に使用したローダー型。 </param>
        /// <param name="innerException"> 原因となった例外。 </param>
        public SaveDataOperationException(
            SaveDataOperationEnum operation,
            Type dataType,
            Type loaderType,
            Exception innerException)
            : base(
                $"[{nameof(SaveStore)}] {dataType?.FullName ?? "(null)"} の{GetOperationName(operation)}に失敗しました。"
                + $" Loader: {loaderType?.FullName ?? "(null)"}",
                innerException)
        {
            // 保存先固有の原因を保持しつつ、利用側が判断できる操作文脈を公開する。
            Operation = operation;
            DataType = dataType;
            LoaderType = loaderType;
        }

        /// <summary> 失敗した操作。 </summary>
        public SaveDataOperationEnum Operation { get; }

        /// <summary> 操作対象のセーブデータ型。 </summary>
        public Type DataType { get; }

        /// <summary> 操作に使用したローダー型。 </summary>
        public Type LoaderType { get; }

        #endregion

        #region 内部処理

        /// <summary>
        ///     操作名を例外メッセージ向けの日本語へ変換する。
        /// </summary>
        /// <param name="operation"> 変換する操作。 </param>
        /// <returns> 例外メッセージに使用する操作名。 </returns>
        private static string GetOperationName(SaveDataOperationEnum operation)
        {
            // 既知の操作は利用者向けの名称へ変換し、将来追加された値は列挙名を失わず表示する。
            return operation switch
            {
                SaveDataOperationEnum.Exists => "存在確認",
                SaveDataOperationEnum.Load => "読み込み",
                SaveDataOperationEnum.Save => "保存",
                SaveDataOperationEnum.Delete => "削除",
                _ => operation.ToString()
            };
        }

        #endregion
    }
}
