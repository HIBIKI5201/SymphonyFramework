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
        public static bool Exists<T>() where T : class, new()
        {
            return Exists(typeof(T));
        }

        public static bool Exists(Type dataType)
        {
            ValidateDataType(dataType);
            return GetLoader().Exists(dataType);
        }

        public static async ValueTask<T> LoadAsync<T>(CancellationToken token = default) where T : class, new()
        {
            SaveData<T> saveData = await LoadSaveDataAsync<T>(token);
            return saveData.MainData;
        }

        public static ValueTask<SaveData<T>> LoadSaveDataAsync<T>(CancellationToken token = default) where T : class, new()
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(typeof(T), out SaveData<object> cached))
                {
                    return new ValueTask<SaveData<T>>(CastSaveData<T>(cached));
                }

                if (_loadingTasks.TryGetValue(typeof(T), out Task<SaveData<object>> loadingTask))
                {
                    return AwaitTypedTask<T>(loadingTask);
                }

                Task<SaveData<object>> loadTask = LoadInternalAsync(typeof(T), token);
                _loadingTasks[typeof(T)] = loadTask;
                return AwaitTypedTask<T>(loadTask);
            }
        }

        public static ValueTask<SaveData<object>> LoadSaveDataAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);

            lock (_lock)
            {
                if (_cache.TryGetValue(dataType, out SaveData<object> cached))
                {
                    return new ValueTask<SaveData<object>>(cached);
                }

                if (_loadingTasks.TryGetValue(dataType, out Task<SaveData<object>> loadingTask))
                {
                    return new ValueTask<SaveData<object>>(loadingTask);
                }

                Task<SaveData<object>> loadTask = LoadInternalAsync(dataType, token);
                _loadingTasks[dataType] = loadTask;
                return new ValueTask<SaveData<object>>(loadTask);
            }
        }

        public static async ValueTask SaveAsync<T>(T data, CancellationToken token = default) where T : class, new()
        {
            await SaveAsync(typeof(T), data, token);
        }

        public static async ValueTask SaveAsync(Type dataType, object data, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            ValidateDataInstance(dataType, data);

            SaveData<object> saved = await GetLoader().SaveAsync(dataType, data, token);
            lock (_lock)
            {
                _cache[dataType] = saved;
            }
        }

        public static void Unload<T>() where T : class, new()
        {
            Unload(typeof(T));
        }

        public static void Unload(Type dataType)
        {
            ValidateDataType(dataType);

            lock (_lock)
            {
                if (_cache.TryGetValue(dataType, out SaveData<object> cached))
                {
                    cached?.Dispose();
                }

                _cache.Remove(dataType);
                _loadingTasks.Remove(dataType);
            }
        }

        public static async ValueTask DeleteAsync<T>(CancellationToken token = default) where T : class, new()
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
                foreach ((Type type, SaveData<object> saveData) in _cache)
                {
                    entries.Add(new SaveDataRegistryEntryInfo(type, saveData?.SaveDate, saveData?.MainData));
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

        private static async Task<SaveData<object>> LoadInternalAsync(Type dataType, CancellationToken token)
        {
            try
            {
                SaveData<object> loaded = await GetLoader().LoadAsync(dataType, token);
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

        private static async ValueTask<SaveData<T>> AwaitTypedTask<T>(Task<SaveData<object>> task) where T : class, new()
        {
            SaveData<object> saveData = await task;
            return CastSaveData<T>(saveData);
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

        private static SaveData<T> CastSaveData<T>(SaveData<object> source) where T : class, new()
        {
            if (source == null || source.MainData == null)
            {
                return new SaveData<T>(new T());
            }

            return new SaveData<T>((T)source.MainData, ParseSaveDate(source.SaveDate));
        }

        private static DateTime ParseSaveDate(string saveDate)
        {
            return DateTime.TryParse(saveDate, out DateTime parsed)
                ? parsed
                : default;
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
        }

        private static void ValidateDataInstance(Type dataType, object data)
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
                foreach (SaveData<object> saveData in _cache.Values)
                {
                    saveData?.Dispose();
                }

                _cache.Clear();
                _loadingTasks.Clear();
            }
        }

        private static readonly object _lock = new();
        private static readonly Dictionary<Type, SaveData<object>> _cache = new();
        private static readonly Dictionary<Type, Task<SaveData<object>>> _loadingTasks = new();

        private static ISaveDataLoader _cachedLoader;
    }
}
