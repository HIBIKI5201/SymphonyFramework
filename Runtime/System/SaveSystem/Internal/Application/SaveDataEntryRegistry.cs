using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータ型をキーに<see cref="SaveDataEntryEntity"/>を所有し、検索と除去を担当する。
    ///     排他制御はこの型が一括で持ち、Service側はロックを意識しない。
    /// </summary>
    internal sealed class SaveDataEntryRegistry
    {
        /// <summary>
        ///     指定型のエントリを取得し、無ければ既定値のインスタンスで作成する。
        ///     永続化データの読み込みは行わない。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> 既存または新規作成したエントリ。 </returns>
        public SaveDataEntryEntity GetOrCreate(Type dataType)
        {
            lock (_lock)
            {
                if (_entries.TryGetValue(dataType, out SaveDataEntryEntity existing)
                    && existing.Content != null)
                {
                    return existing;
                }

                var created = new SaveDataEntryEntity(
                    dataType,
                    (SaveDataContent)Activator.CreateInstance(dataType));
                _entries[dataType] = created;
                _version++;
                return created;
            }
        }

        /// <summary> 指定型が読み込み済みとして記録されているか確認する。 </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> 読み込み済みの場合はtrue。 </returns>
        public bool IsLoaded(Type dataType)
        {
            lock (_lock)
            {
                return _entries.TryGetValue(dataType, out SaveDataEntryEntity entry) && entry.IsLoaded;
            }
        }

        /// <summary>
        ///     指定インスタンスが現在のキャッシュと同一の場合だけ読み込み済みにする。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="content"> 読み込みに使用したインスタンス。 </param>
        public void MarkLoaded(Type dataType, SaveDataContent content)
        {
            lock (_lock)
            {
                if (!_entries.TryGetValue(dataType, out SaveDataEntryEntity entry)
                    || entry.IsLoaded)
                {
                    return;
                }

                if (entry.MarkLoadedIfCurrent(content))
                {
                    _version++;
                }
            }
        }

        /// <summary> 指定型の読み込み済み状態を解除する。 </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        public void MarkUnloaded(Type dataType)
        {
            lock (_lock)
            {
                if (!_entries.TryGetValue(dataType, out SaveDataEntryEntity entry)
                    || !entry.IsLoaded)
                {
                    return;
                }

                entry.MarkUnloaded();
                _version++;
            }
        }

        /// <summary> 進行中のロード処理があれば取得する。 </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="loadingTask"> 進行中のロード処理。 </param>
        /// <returns> 進行中の処理がある場合はtrue。 </returns>
        public bool TryGetLoadingTask(Type dataType, out Task loadingTask)
        {
            lock (_lock)
            {
                return _loadingTasks.TryGetValue(dataType, out loadingTask);
            }
        }

        /// <summary>
        ///     進行中のロード処理を重複排除用に登録する。
        ///     **完了済みのタスクは登録しない。** 同期的に完了するローダーでは、登録した時点で
        ///     <see cref="RemoveLoadingTask"/>による自己解除が済んでおり、完了済みタスクが
        ///     残り続けると以降のロードがすべて重複扱いになって実行されなくなる。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="loadingTask"> 登録するロード処理。 </param>
        public void RegisterLoadingTaskIfPending(Type dataType, Task loadingTask)
        {
            if (loadingTask.IsCompleted)
            {
                return;
            }

            lock (_lock)
            {
                _loadingTasks[dataType] = loadingTask;
            }
        }

        /// <summary> 進行中のロード処理の登録を解除する。 </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        public void RemoveLoadingTask(Type dataType)
        {
            lock (_lock)
            {
                _loadingTasks.Remove(dataType);
            }
        }

        /// <summary>
        ///     現在保持している全エントリの複製を取得する。
        ///     ロード処理がバックグラウンドで進行しうるため、列挙用の複製をロック内で作る。
        /// </summary>
        /// <returns> 保持順のEntity一覧。 </returns>
        public IReadOnlyList<SaveDataEntryEntity> GetEntities()
        {
            lock (_lock)
            {
                return new List<SaveDataEntryEntity>(_entries.Values);
            }
        }

        /// <summary>
        ///     保持している状態が実際に変化するたびに増える版番号。
        ///     エントリの新規作成、読み込み済み状態の変化、全消去で増える。
        ///     キャッシュ内容の書き換えでは増えないため、保存日時の変化は検出できない。
        /// </summary>
        public int Version
        {
            get
            {
                lock (_lock)
                {
                    return _version;
                }
            }
        }

        /// <summary> 全エントリを解放し、ロード管理状態を消去する。 </summary>
        public void Clear()
        {
            lock (_lock)
            {
                if (_entries.Count <= 0 && _loadingTasks.Count <= 0)
                {
                    return;
                }

                foreach (SaveDataEntryEntity entry in _entries.Values)
                {
                    entry.ReleaseContent();
                }

                _entries.Clear();
                _loadingTasks.Clear();
                _version++;
            }
        }

        private readonly object _lock = new();
        private readonly Dictionary<Type, SaveDataEntryEntity> _entries = new();
        private readonly Dictionary<Type, Task> _loadingTasks = new();

        private int _version;
    }
}
