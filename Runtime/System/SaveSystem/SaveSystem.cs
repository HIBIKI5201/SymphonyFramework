using System;
using System.Threading;
using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     CoreSystemが所有するライフタイムにSaveDataRegistryを連動させます。
    /// </summary>
    internal static class SaveSystem
    {
        /// <summary> セーブデータレジストリをシステムのライフタイムへ関連付ける。 </summary>
        internal static void Initialize(
            CancellationToken destroyCancellationToken,
            Func<SaveDataLoader> loaderResolver)
        {
            _destroyRegistration.Dispose();
            SaveDataRegistry.ConfigureLoaderResolver(loaderResolver);
            _destroyRegistration = destroyCancellationToken.Register(SaveDataRegistry.ResetRuntimeState);
        }

        private static CancellationTokenRegistration _destroyRegistration;
    }

    /// <summary>
    ///     指定したSaveDataLoaderでセーブデータを管理するジェネリックFacadeです。
    /// </summary>
    /// <typeparam name="TData"> 管理するセーブデータの型。 </typeparam>
    /// <typeparam name="TLoader"> 使用するSaveDataLoaderの型。 </typeparam>
    public static class SaveSystem<TData, TLoader>
        where TData : SaveDataContent, new()
        where TLoader : SaveDataLoader, new()
    {
        /// <summary> キャッシュまたは保存先からセーブデータを取得する。 </summary>
        /// <returns> 取得したセーブデータ。 </returns>
        public static async ValueTask<TData> Get()
        {
            if (_saveData == null)
            {
                _saveData = new TData();
                await _loader.LoadAsync(typeof(TData), _saveData);
            }

            return _saveData;
        }

        /// <summary> セーブデータに記録された最終保存日時を取得する。 </summary>
        /// <returns> ISO 8601形式の最終保存日時。 </returns>
        public static async ValueTask<string> GetDate()
        {
            if (_saveData == null)
            {
                await Load();
            }

            return _saveData?.SaveDate;
        }

        /// <summary>
        ///     指定したSaveDataLoaderを用いて保存する。
        /// </summary>
        public static async ValueTask Save()
        {
            TData data = await Get();
            await _loader.SaveAsync(typeof(TData), data);
        }

        /// <summary>
        ///     指定したSaveDataLoaderを用いて現在のインスタンスへロードする。
        /// </summary>
        public static async ValueTask Load()
        {
            _saveData ??= new TData();
            await _loader.LoadAsync(typeof(TData), _saveData);
        }

        /// <summary> キャッシュ中のセーブデータを破棄する。 </summary>
        public static void Dispose()
        {
            if (_saveData == null) { return; }

            _saveData.Dispose();
            _saveData = null;
        }

        private static TData _saveData;
        private static readonly TLoader _loader = new();
    }
}
