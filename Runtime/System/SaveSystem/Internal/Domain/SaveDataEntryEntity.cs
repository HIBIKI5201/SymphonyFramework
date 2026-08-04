using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータ型ごとの登録単位。
    ///     セーブデータ型を同一性とし、キャッシュしている内容と読み込み済み状態を保持する。
    ///     状態の変更は状態遷移メソッドからのみ行い、外部から直接書き換えさせない。
    /// </summary>
    internal sealed class SaveDataEntryEntity
    {
        /// <summary>
        ///     セーブデータ型と初期インスタンスを指定して生成する。
        /// </summary>
        /// <param name="dataType"> このエントリが表すセーブデータ型。 </param>
        /// <param name="content"> 初期のキャッシュ内容。 </param>
        public SaveDataEntryEntity(Type dataType, SaveDataContent content)
        {
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
        ///     読み込み中にキャッシュが差し替わった場合へ、古い結果を反映させないため。
        /// </summary>
        /// <param name="content"> 読み込みに使用したインスタンス。 </param>
        /// <returns> 読み込み済みとして記録した場合はtrue。 </returns>
        public bool MarkLoadedIfCurrent(SaveDataContent content)
        {
            if (!ReferenceEquals(Content, content))
            {
                return false;
            }

            IsLoaded = true;
            return true;
        }

        /// <summary> 読み込み済み状態を解除する。永続化データを削除したときに使う。 </summary>
        public void MarkUnloaded()
        {
            IsLoaded = false;
        }

        /// <summary>
        ///     キャッシュしているインスタンスを解放し、読み込み済み状態を解除する。
        /// </summary>
        public void ReleaseContent()
        {
            Content?.Dispose();
            IsLoaded = false;
        }
    }
}
