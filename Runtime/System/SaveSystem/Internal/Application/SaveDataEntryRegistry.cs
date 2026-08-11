using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータ型ごとのEntityを所有する。
    /// </summary>
    /// <remarks> 排他制御を一括で持ち、Service側へ公開しない。 </remarks>
    internal sealed class SaveDataEntryRegistry
    {
        #region 外部向けAPI

        /// <summary> 保持状態が変化するたびに増える版番号。 </summary>
        /// <remarks>
        ///     エントリの作成、読み込み状態の変化、全消去で増える。
        ///     キャッシュ内容の書き換えでは増えないため、保存日時の変化は検出できない。
        /// </remarks>
        public int Version
        {
            get
            {
                lock (_lock)
                {
                    // 版番号と状態を同じ同期単位で読み取る。
                    return _version;
                }
            }
        }

        /// <summary>
        ///     指定型のエントリを取得または作成する。
        /// </summary>
        /// <remarks> 新規作成時は既定値を使用し、永続化データを読み込まない。 </remarks>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> 既存または新規作成したエントリ。 </returns>
        public SaveDataEntryEntity GetOrCreate(Type dataType)
        {
            lock (_lock)
            {
                // 読み込み済みかどうかに関係なく、内容を保持する既存エントリはそのまま再利用する。
                if (_entries.TryGetValue(dataType, out SaveDataEntryEntity existing)
                    && existing.Content != null)
                {
                    return existing;
                }

                // 永続化データは明示的なロードまで読まず、初回アクセス時は既定値だけをキャッシュする。
                SaveDataEntryEntity created = new(
                    dataType,
                    (SaveDataContent)Activator.CreateInstance(dataType));
                _entries[dataType] = created;
                _version++;
                return created;
            }
        }

        /// <summary>
        ///     指定型が読み込み済みとして記録されているか確認する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> 読み込み済みの場合はtrue。 </returns>
        public bool IsLoaded(Type dataType)
        {
            lock (_lock)
            {
                // 既定値のキャッシュ作成だけでは、永続化データの読み込み済みと扱わない。
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
                // エントリが消去済み、または既に読み込み済みなら状態を変更しない。
                if (!_entries.TryGetValue(dataType, out SaveDataEntryEntity entry)
                    || entry.IsLoaded)
                {
                    return;
                }

                // 読み込み中に差し替えられていない現在のインスタンスだけを完了状態へ進める。
                if (entry.MarkLoadedIfCurrent(content)) { _version++; }
            }
        }

        /// <summary>
        ///     指定型の読み込み済み状態を解除する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        public void MarkUnloaded(Type dataType)
        {
            lock (_lock)
            {
                // 未登録または未読み込みのエントリでは、観測可能な状態変化が無いため何もしない。
                if (!_entries.TryGetValue(dataType, out SaveDataEntryEntity entry)
                    || !entry.IsLoaded)
                {
                    return;
                }

                // 読み込み状態の解除と版番号の更新を同じロック内で確定する。
                entry.MarkUnloaded();
                _version++;
            }
        }

        /// <summary>
        ///     進行中のロード処理があれば取得する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="loadingTask"> 進行中のロード処理。 </param>
        /// <returns> 進行中の処理がある場合はtrue。 </returns>
        public bool TryGetLoadingTask(Type dataType, out Task loadingTask)
        {
            lock (_lock)
            {
                // 同じ型の並行ロードが同じTaskを共有できるよう、登録済み処理を返す。
                return _loadingTasks.TryGetValue(dataType, out loadingTask);
            }
        }

        /// <summary>
        ///     進行中のロード処理を重複排除用に登録する。
        /// </summary>
        /// <remarks>
        ///     同期完了するローダーでは自己解除後となるため、完了済みTaskを登録しない。
        ///     登録すると以降のロードが重複扱いになり実行されない。
        /// </remarks>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="loadingTask"> 登録するロード処理。 </param>
        public void RegisterLoadingTaskIfPending(Type dataType, Task loadingTask)
        {
            // 同期完了したTaskは自己解除後なので、再登録して以後のロードを塞がない。
            if (loadingTask.IsCompleted) { return; }

            lock (_lock)
            {
                // 同じ型の後続要求が進行中のTaskを共有できるよう登録する。
                _loadingTasks[dataType] = loadingTask;
            }
        }

        /// <summary>
        ///     進行中のロード処理の登録を解除する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        public void RemoveLoadingTask(Type dataType)
        {
            lock (_lock)
            {
                // 完了したTaskを次回ロードの重複判定へ残さない。
                _loadingTasks.Remove(dataType);
            }
        }

        /// <summary>
        ///     保持している全エントリの複製を取得する。
        /// </summary>
        /// <remarks> バックグラウンドのロードと競合しないよう、ロック内で複製する。 </remarks>
        /// <returns> 保持順のEntity一覧。 </returns>
        public IReadOnlyList<SaveDataEntryEntity> GetEntities()
        {
            lock (_lock)
            {
                // ロック外の列挙中にバックグラウンド処理がコレクションを変更しないよう複製する。
                return new List<SaveDataEntryEntity>(_entries.Values);
            }
        }

        /// <summary>
        ///     全エントリとロード管理状態を消去する。
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                // キャッシュと進行中Taskのどちらも無ければ、版番号を不要に進めない。
                if (_entries.Count <= 0 && _loadingTasks.Count <= 0) { return; }

                // キャッシュを破棄する前に、利用側データが保持する資源をすべて解放する。
                foreach (SaveDataEntryEntity entry in _entries.Values)
                {
                    entry.ReleaseContent();
                }

                // ロード完了後の古いTaskが新しい状態へ影響しないよう、管理状態をまとめて消去する。
                _entries.Clear();
                _loadingTasks.Clear();
                _version++;
            }
        }

        #endregion

        #region 内部処理

        private readonly object _lock = new();
        private readonly Dictionary<Type, SaveDataEntryEntity> _entries = new();
        private readonly Dictionary<Type, Task> _loadingTasks = new();

        private int _version;

        #endregion
    }
}
