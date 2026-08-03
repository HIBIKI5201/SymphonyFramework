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
                _entrySnapshotDirty = true;
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
                if (_entries.TryGetValue(dataType, out SaveDataEntryEntity entry))
                {
                    entry.MarkLoadedIfCurrent(content);
                }
            }
        }

        /// <summary> 指定型の読み込み済み状態を解除する。 </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        public void MarkUnloaded(Type dataType)
        {
            lock (_lock)
            {
                if (_entries.TryGetValue(dataType, out SaveDataEntryEntity entry))
                {
                    entry.MarkUnloaded();
                }
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
        ///     現在保持している全エントリの読み取り専用スナップショットを取得する。
        ///     内容が変化していない間は同じインスタンスを返す。
        /// </summary>
        /// <returns> エントリのスナップショット。 </returns>
        public IReadOnlyList<SaveDataRegistryEntryInfo> GetEntrySnapshot()
        {
            lock (_lock)
            {
                if (!_entrySnapshotDirty)
                {
                    return _entrySnapshot;
                }

                List<SaveDataRegistryEntryInfo> entries = new(_entries.Count);
                foreach (SaveDataEntryEntity entry in _entries.Values)
                {
                    entries.Add(new SaveDataRegistryEntryInfo(entry.DataType, entry.Content));
                }

                _entrySnapshot = entries.AsReadOnly();
                _entrySnapshotDirty = false;
                return _entrySnapshot;
            }
        }

        /// <summary> 読み込み済みとして記録されている型のスナップショットを取得する。 </summary>
        /// <returns> 読み込み済みの型一覧。 </returns>
        public IReadOnlyCollection<Type> GetLoadedTypes()
        {
            lock (_lock)
            {
                List<Type> loaded = new();
                foreach (SaveDataEntryEntity entry in _entries.Values)
                {
                    if (entry.IsLoaded)
                    {
                        loaded.Add(entry.DataType);
                    }
                }

                return loaded.AsReadOnly();
            }
        }

        /// <summary> 全エントリを解放し、ロード管理状態を消去する。 </summary>
        public void Clear()
        {
            lock (_lock)
            {
                foreach (SaveDataEntryEntity entry in _entries.Values)
                {
                    entry.ReleaseContent();
                }

                _entries.Clear();
                _loadingTasks.Clear();
                _entrySnapshotDirty = true;
            }
        }

        private readonly object _lock = new();
        private readonly Dictionary<Type, SaveDataEntryEntity> _entries = new();
        private readonly Dictionary<Type, Task> _loadingTasks = new();

        private IReadOnlyList<SaveDataRegistryEntryInfo> _entrySnapshot =
            Array.Empty<SaveDataRegistryEntryInfo>();

        private bool _entrySnapshotDirty = true;
    }
}
