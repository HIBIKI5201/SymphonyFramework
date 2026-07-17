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

            SaveDataContent data = GetOrCreateCache(dataType, out bool isFirstAccess);

            if (isFirstAccess)
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
            SaveDataContent current = GetOrCreateCache(dataType, out _);

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
            SaveDataContent data = GetOrCreateCache(dataType, out _);
            await GetLoader().SaveAsync(dataType, data, token);
        }

        public static async ValueTask DeleteAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
        {
            await DeleteAsync(typeof(T), token);
        }

        public static async ValueTask DeleteAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            SaveDataContent current = GetOrCreateCache(dataType, out _);
            await GetLoader().DeleteAsync(dataType, token);
            await GetLoader().LoadAsync(dataType, current, token);
        }

        public static IReadOnlyList<SaveDataRegistryEntryInfo> GetEntries()
        {
            lock (_lock)
            {
                List<SaveDataRegistryEntryInfo> entries = new(_cache.Count);
                foreach ((Type type, SaveDataContent saveData) in _cache)
                {
                    entries.Add(new SaveDataRegistryEntryInfo(type, saveData));
                }

                return entries;
            }
        }

        public static void RefreshLoader()
        {
            _cachedLoader = null;
            ClearCache();
        }

        public static ISaveDataLoader GetCurrentLoader() => GetLoader();

        /// <summary>
        ///     ロードを発生させずにキャッシュ済みインスタンスを取得します。無ければ既定値で作成します。
        /// </summary>
        private static SaveDataContent GetOrCreateCache(Type dataType, out bool isFirstAccess)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(dataType, out SaveDataContent cached) && cached != null)
                {
                    isFirstAccess = false;
                    return cached;
                }

                SaveDataContent created = (SaveDataContent)Activator.CreateInstance(dataType);
                _cache[dataType] = created;
                isFirstAccess = true;
                return created;
            }
        }

        private static async Task LoadInternalAsync(Type dataType, SaveDataContent current, CancellationToken token)
        {
            try
            {
                await GetLoader().LoadAsync(dataType, current, token);
            }
            finally
            {
                lock (_lock)
                {
                    _loadingTasks.Remove(dataType);
                }
            }
        }

        private static ISaveDataLoader GetLoader()
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
                _loadingTasks.Clear();
            }
        }

        private static readonly object _lock = new();
        private static readonly Dictionary<Type, SaveDataContent> _cache = new();
        private static readonly Dictionary<Type, Task> _loadingTasks = new();

        private static ISaveDataLoader _cachedLoader;
    }
}
