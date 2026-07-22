using SymphonyFrameWork.Config;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     プロジェクト設定に従ってセーブデータを一括管理するレジストリです。
    /// </summary>
    public static class SaveDataRegistry
    {
        public static bool Exists<T>() where T : SaveDataContent, new()
        {
            return Exists(typeof(T));
        }

        public static bool Exists(Type dataType)
        {
            ValidateDataType(dataType);
            return GetLoader().Exists(dataType);
        }

        public static T Get<T>() where T : SaveDataContent, new()
        {
            return (T)Get(typeof(T));
        }

        /// <summary>
        ///     Registry が保持している現在のインスタンスを取得します。
        ///     キャッシュが無い初回アクセス時は、自動的に永続化データをロードします。
        /// </summary>
        public static SaveDataContent Get(Type dataType)
        {
            ValidateDataType(dataType);

            SaveDataContent data = GetOrCreateCache(dataType);

            if (!IsLoaded(dataType))
            {
                LoadAsync(dataType).GetAwaiter().GetResult();
            }

            return data;
        }

        public static async ValueTask<T> LoadAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
        {
            await LoadAsync(typeof(T), token);
            return Get<T>();
        }

        public static ValueTask LoadAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            SaveDataContent current = GetOrCreateCache(dataType);

            lock (_lock)
            {
                if (_loadingTasks.TryGetValue(dataType, out Task loadingTask))
                {
                    return new ValueTask(loadingTask);
                }

                Task loadTask = LoadInternalAsync(dataType, current, token);

                // 既定のローダー（PlayerPrefs ベース）は同期的に完了するため、この時点で
                // loadTask は既に完了しており、LoadInternalAsync の finally による
                // 自己解除もすでに実行済みである。ここで無条件に登録すると、完了済みの
                // 古いタスクが _loadingTasks に残り続け、以降の LoadAsync 呼び出しが
                // すべて「重複リクエスト」とみなされて実際のロードが二度と走らなくなる
                // （＝ Load しても保存済みデータが読み込まれない）バグになる。
                // 未完了（本当に非同期I/Oを行うローダー）の場合のみ重複排除用に登録する。
                if (!loadTask.IsCompleted)
                {
                    _loadingTasks[dataType] = loadTask;
                }

                return new ValueTask(loadTask);
            }
        }

        public static ValueTask SaveAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
        {
            return SaveAsync(typeof(T), token);
        }

        public static async ValueTask SaveAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            SaveDataContent data = GetOrCreateCache(dataType);
            await GetLoader().SaveAsync(dataType, data, token);
            MarkLoaded(dataType, data);
        }

        public static async ValueTask DeleteAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
        {
            await DeleteAsync(typeof(T), token);
        }

        public static async ValueTask DeleteAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            SaveDataContent current = GetOrCreateCache(dataType);

            lock (_lock)
            {
                _loadedTypes.Remove(dataType);
            }

            await GetLoader().DeleteAsync(dataType, token);
            await GetLoader().LoadAsync(dataType, current, token);
            MarkLoaded(dataType, current);
        }

        public static IReadOnlyList<SaveDataRegistryEntryInfo> GetEntries()
        {
            lock (_lock)
            {
                if (!_entrySnapshotDirty)
                {
                    return _entrySnapshot;
                }

                List<SaveDataRegistryEntryInfo> entries = new(_cache.Count);
                foreach ((Type type, SaveDataContent saveData) in _cache)
                {
                    entries.Add(new SaveDataRegistryEntryInfo(type, saveData));
                }

                _entrySnapshot = entries.AsReadOnly();
                _entrySnapshotDirty = false;
                return _entrySnapshot;
            }
        }

        public static void RefreshLoader()
        {
            ResetRuntimeState();
        }

        internal static void ResetRuntimeState()
        {
            _cachedLoader = null;
            ClearCache();
        }

        public static SaveDataLoader GetCurrentLoader() => GetLoader();

        /// <summary>
        ///     ロードを発生させずにキャッシュ済みインスタンスを取得します。無ければ既定値で作成します。
        /// </summary>
        private static SaveDataContent GetOrCreateCache(Type dataType)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(dataType, out SaveDataContent cached) && cached != null)
                {
                    return cached;
                }

                SaveDataContent created = (SaveDataContent)Activator.CreateInstance(dataType);
                _cache[dataType] = created;
                _entrySnapshotDirty = true;
                return created;
            }
        }

        private static bool IsLoaded(Type dataType)
        {
            lock (_lock)
            {
                return _loadedTypes.Contains(dataType);
            }
        }

        private static void MarkLoaded(Type dataType, SaveDataContent data)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(dataType, out SaveDataContent cached)
                    && ReferenceEquals(cached, data))
                {
                    _loadedTypes.Add(dataType);
                }
            }
        }

        private static async Task LoadInternalAsync(Type dataType, SaveDataContent current, CancellationToken token)
        {
            try
            {
                await GetLoader().LoadAsync(dataType, current, token);
                MarkLoaded(dataType, current);
            }
            finally
            {
                lock (_lock)
                {
                    _loadingTasks.Remove(dataType);
                }
            }
        }

        private static SaveDataLoader GetLoader()
        {
            if (_cachedLoader != null)
            {
                return _cachedLoader;
            }

            SaveSystemConfig config = SymphonyConfigLocator.GetConfig<SaveSystemConfig>();
            _cachedLoader = config?.Loader ?? new JsonUtilitySaveDataLoader();
            return _cachedLoader;
        }

        private static void ValidateDataType(Type dataType)
        {
            if (dataType == null)
            {
                throw new ArgumentNullException(nameof(dataType));
            }

            if (!dataType.IsClass || dataType.IsAbstract || dataType.IsGenericTypeDefinition)
            {
                throw new ArgumentException("セーブ対象は new() 可能な具象クラスにしてください。", nameof(dataType));
            }

            if (dataType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException("デフォルトコンストラクタが必要です。", nameof(dataType));
            }

            if (!typeof(SaveDataContent).IsAssignableFrom(dataType))
            {
                throw new ArgumentException($"{nameof(SaveDataContent)} を継承した型を指定してください。", nameof(dataType));
            }
        }

        private static void ClearCache()
        {
            lock (_lock)
            {
                foreach (SaveDataContent saveData in _cache.Values)
                {
                    saveData?.Dispose();
                }

                _cache.Clear();
                _loadedTypes.Clear();
                _loadingTasks.Clear();
                _entrySnapshotDirty = true;
            }
        }

        private static readonly object _lock = new();
        private static readonly Dictionary<Type, SaveDataContent> _cache = new();
        private static readonly HashSet<Type> _loadedTypes = new();
        private static readonly Dictionary<Type, Task> _loadingTasks = new();

        private static IReadOnlyList<SaveDataRegistryEntryInfo> _entrySnapshot = Array.Empty<SaveDataRegistryEntryInfo>();
        private static bool _entrySnapshotDirty = true;

        private static SaveDataLoader _cachedLoader;
    }
}
