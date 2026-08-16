using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータ型ごとのキャッシュ状態を保持する。
    /// </summary>
    /// <remarks> セーブデータ型を同一性とし、状態遷移メソッドからのみ変更する。 </remarks>
    internal sealed class SaveDataEntryEntity
    {
        #region 外部向けAPI

        /// <summary>
        ///     セーブデータ型と初期インスタンスを指定して生成する。
        /// </summary>
        /// <param name="dataType"> このエントリが表すセーブデータ型。 </param>
        /// <param name="content"> 初期のキャッシュ内容。 </param>
        public SaveDataEntryEntity(Type dataType, SaveDataContent content)
        {
            // 型と内容が欠けたエントリは状態遷移できないため、生成時に拒否する。
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary> このエントリの同一性を表すセーブデータ型。 </summary>
        public Type DataType { get; }

        /// <summary> 現在キャッシュしているインスタンス。 </summary>
        public SaveDataContent Content { get; private set; }

        /// <summary> 永続化データを読み込み済みかどうか。 </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        ///     指定インスタンスが現在のキャッシュと同一の場合だけ読み込み済みにする。
        /// </summary>
        /// <remarks> 読み込み中にキャッシュが差し替わった場合は、古い結果を反映しない。 </remarks>
        /// <param name="content"> 読み込みに使用したインスタンス。 </param>
        /// <returns> 読み込み済みとして記録した場合はtrue。 </returns>
        public bool MarkLoadedIfCurrent(SaveDataContent content)
        {
            // 読み込み中にキャッシュが差し替わった場合は、古い完了結果を現在状態へ反映しない。
            if (!ReferenceEquals(Content, content)) { return false; }

            // 現在のキャッシュへ永続化データが反映されたことを記録する。
            IsLoaded = true;
            return true;
        }

        /// <summary>
        ///     読み込み済み状態を解除する。
        /// </summary>
        /// <remarks> 永続化データを削除するときに使用する。 </remarks>
        public void MarkUnloaded()
        {
            // キャッシュの内容は維持し、削除処理中の取得可否だけを変更する。
            IsLoaded = false;
        }

        /// <summary>
        ///     キャッシュしているインスタンスを解放し、読み込み済み状態を解除する。
        /// </summary>
        public void ReleaseContent()
        {
            // 利用側の破棄処理を実行してから、エントリを未読み込み状態へ戻す。
            Content?.Dispose();
            IsLoaded = false;
        }

        #endregion
    }
}
