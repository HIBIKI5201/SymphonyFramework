using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api = SymphonyFrameWork.System.API;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     <see cref="Api.SaveDataRegistry"/> へ移動しました。
    /// </summary>
    [Obsolete("SymphonyFrameWork.System.API.SaveDataRegistry に移動しました。今後はそちらを使用してください。", error: false)]
    public static class SaveDataRegistry
    {
        /// <summary> 指定型の永続化データが存在するか確認する。 </summary>
        public static bool Exists<T>() where T : SaveDataContent, new()
            => Api.SaveDataRegistry.Exists<T>();

        /// <summary> 指定型の永続化データが存在するか確認する。 </summary>
        public static bool Exists(Type dataType)
            => Api.SaveDataRegistry.Exists(dataType);

        /// <summary> 指定型のキャッシュまたは保存済みデータを同期的に取得する。 </summary>
        public static T Get<T>() where T : SaveDataContent, new()
            => Api.SaveDataRegistry.Get<T>();

        /// <summary>
        ///     Registry が保持している現在のインスタンスを取得します。
        ///     キャッシュが無い初回アクセス時は、自動的に永続化データをロードします。
        /// </summary>
        public static SaveDataContent Get(Type dataType)
            => Api.SaveDataRegistry.Get(dataType);

        /// <summary> 指定型の保存済みデータをキャッシュへ非同期に読み込む。 </summary>
        public static ValueTask<T> LoadAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
            => Api.SaveDataRegistry.LoadAsync<T>(token);

        /// <summary> 指定型の保存済みデータをキャッシュへ非同期に読み込む。 </summary>
        public static ValueTask LoadAsync(Type dataType, CancellationToken token = default)
            => Api.SaveDataRegistry.LoadAsync(dataType, token);

        /// <summary> 指定型のキャッシュを保存先へ非同期に書き込む。 </summary>
        public static ValueTask SaveAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
            => Api.SaveDataRegistry.SaveAsync<T>(token);

        /// <summary> 指定型のキャッシュを保存先へ非同期に書き込む。 </summary>
        public static ValueTask SaveAsync(Type dataType, CancellationToken token = default)
            => Api.SaveDataRegistry.SaveAsync(dataType, token);

        /// <summary> 指定型の保存済みデータを削除し、キャッシュを既定値へ戻す。 </summary>
        public static ValueTask DeleteAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
            => Api.SaveDataRegistry.DeleteAsync<T>(token);

        /// <summary> 指定型の保存済みデータを削除し、キャッシュを既定値へ戻す。 </summary>
        public static ValueTask DeleteAsync(Type dataType, CancellationToken token = default)
            => Api.SaveDataRegistry.DeleteAsync(dataType, token);

        /// <summary> 現在キャッシュされている全エントリの読み取り専用スナップショットを取得する。 </summary>
        public static IReadOnlyList<SaveDataRegistryEntryInfo> GetEntries()
            => Api.SaveDataRegistry.GetEntries();

        /// <summary> ローダーとキャッシュを破棄し、次回アクセス時にConfigから再解決する。 </summary>
        public static void RefreshLoader()
            => Api.SaveDataRegistry.RefreshLoader();

        /// <summary> 現在選択されているローダーを取得する。 </summary>
        public static SaveDataLoader GetCurrentLoader()
            => Api.SaveDataRegistry.GetCurrentLoader();
    }
}
