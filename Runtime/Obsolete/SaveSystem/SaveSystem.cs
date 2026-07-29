using System;
using System.Threading.Tasks;
using Api = SymphonyFrameWork.System.API;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     <see cref="Api.SaveSystem{TData, TLoader}"/> へ移動しました。
    /// </summary>
    [Obsolete("SymphonyFrameWork.System.API.SaveSystem<TData, TLoader> に移動しました。今後はそちらを使用してください。", error: false)]
    public static class SaveSystem<TData, TLoader>
        where TData : SaveDataContent, new()
        where TLoader : SaveDataLoader, new()
    {
        /// <summary> キャッシュまたは保存先からセーブデータを取得する。 </summary>
        /// <returns> 取得したセーブデータ。 </returns>
        public static ValueTask<TData> Get() => Api.SaveSystem<TData, TLoader>.Get();

        /// <summary> セーブデータに記録された最終保存日時を取得する。 </summary>
        /// <returns> ISO 8601形式の最終保存日時。 </returns>
        public static ValueTask<string> GetDate() => Api.SaveSystem<TData, TLoader>.GetDate();

        /// <summary>
        ///     指定したSaveDataLoaderを用いて保存する。
        /// </summary>
        public static ValueTask Save() => Api.SaveSystem<TData, TLoader>.Save();

        /// <summary>
        ///     指定したSaveDataLoaderを用いて現在のインスタンスへロードする。
        /// </summary>
        public static ValueTask Load() => Api.SaveSystem<TData, TLoader>.Load();

        /// <summary> キャッシュ中のセーブデータを破棄する。 </summary>
        public static void Dispose() => Api.SaveSystem<TData, TLoader>.Dispose();
    }
}
