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

        public static bool TryGetLoaded<T>(out T data) where T : SaveDataContent
        {
            if (TryGetLoaded(typeof(T), out SaveDataContent loadedData))
            {
                data = (T)loadedData;
                return true;
            }

            data = null;
            return false;
        }

        public static bool TryGetLoaded(Type dataType, out SaveDataContent data)
        {
            ValidateDataType(dataType);

            lock (_lock)
            {
                if (_cache.TryGetValue(dataType, out SaveDataContent saveData) && saveData != null)
                {
                    data = saveData;
                    return true;
                }
            }

            data = null;
            return false;
        }

        public static async ValueTask<T> LoadAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
        {
            return await LoadTypedAsync<T>(token);
        }

        public static ValueTask<T> LoadTypedAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(typeof(T), out SaveDataContent cached))
                {
                    return new ValueTask<T>((T)cached);
                }

                if (_loadingTasks.TryGetValue(typeof(T), out Task<SaveDataContent> loadingTask))
                {
                    return AwaitTypedTask<T>(loadingTask);
                }

                Task<SaveDataContent> loadTask = LoadInternalAsync(typeof(T), token);
                _loadingTasks[typeof(T)] = loadTask;
                return AwaitTypedTask<T>(loadTask);
            }
        }

        public static ValueTask<SaveDataContent> LoadAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);

            lock (_lock)
            {
                if (_cache.TryGetValue(dataType, out SaveDataContent cached))
                {
                    return new ValueTask<SaveDataContent>(cached);
                }

                if (_loadingTasks.TryGetValue(dataType, out Task<SaveDataContent> loadingTask))
                {
                    return new ValueTask<SaveDataContent>(loadingTask);
                }

                Task<SaveDataContent> loadTask = LoadInternalAsync(dataType, token);
                _loadingTasks[dataType] = loadTask;
                return new ValueTask<SaveDataContent>(loadTask);
            }
        }

        public static async ValueTask SaveAsync<T>(T data, CancellationToken token = default) where T : SaveDataContent, new()
        {
            await SaveAsync(typeof(T), data, token);
        }

        public static async ValueTask SaveAsync(Type dataType, SaveDataContent data, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            ValidateDataInstance(dataType, data);

            SaveDataContent saved = await GetLoader().SaveAsync(dataType, data, token);
            lock (_lock)
            {
                _cache[dataType] = saved;
            }
        }

        public static void Unload<T>() where T : SaveDataContent, new()
        {
            Unload(typeof(T));
        }

        public static void Unload(Type dataType)
        {
            ValidateDataType(dataType);

            lock (_lock)
            {
                if (_cache.TryGetValue(dataType, out SaveDataContent cached))
                {
                    cached?.Dispose();
                }

                _cache.Remove(dataType);
                _loadingTasks.Remove(dataType);
            }
        }

        public static async ValueTask DeleteAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
        {
            await DeleteAsync(typeof(T), token);
        }

        public static async ValueTask DeleteAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            Unload(dataType);
            await GetLoader().DeleteAsync(dataType, token);
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

        private static async Task<SaveDataContent> LoadInternalAsync(Type dataType, CancellationToken token)
        {
            try
            {
                SaveDataContent loaded = await GetLoader().LoadAsync(dataType, token);
                lock (_lock)
                {
                    _cache[dataType] = loaded;
                }

                return loaded;
            }
            finally
            {
                lock (_lock)
                {
                    _loadingTasks.Remove(dataType);
                }
            }
        }

        private static async ValueTask<T> AwaitTypedTask<T>(Task<SaveDataContent> task) where T : SaveDataContent, new()
        {
            SaveDataContent saveData = await task;
            return (T)saveData;
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

        private static void ValidateDataInstance(Type dataType, SaveDataContent data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!dataType.IsInstanceOfType(data))
            {
                throw new ArgumentException($"{dataType.Name} のインスタンスを指定してください。", nameof(data));
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
        private static readonly Dictionary<Type, Task<SaveDataContent>> _loadingTasks = new();

        private static ISaveDataLoader _cachedLoader;
    }
}
