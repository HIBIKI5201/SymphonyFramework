using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     <see cref="SaveStore"/>へ転送する非推奨のFacadeです。
    ///     Round I1／I2で内部を分割した結果、この型はレジストリではなくストアになりました。
    ///     引数の検証も状態の保持も行わず、すべて<see cref="SaveStore"/>へ委譲します。
    /// </summary>
    [Obsolete("SaveStoreを使用してください。3.0.0で削除します。", error: false)]
    public static class SaveDataRegistry
    {
        /// <summary> 指定型の永続化データが存在するか確認する。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で存在確認に失敗した場合。 </exception>
        public static bool Exists<T>() where T : SaveDataContent, new() => SaveStore.Exists<T>();

        /// <summary> 指定型の永続化データが存在するか確認する。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で存在確認に失敗した場合。 </exception>
        public static bool Exists(Type dataType) => SaveStore.Exists(dataType);

        /// <summary> 指定型のキャッシュまたは保存済みデータを同期的に取得する。 </summary>
        /// <exception cref="SaveDataOperationException"> 初回の同期読み込みに失敗した場合。 </exception>
        public static T Get<T>() where T : SaveDataContent, new() => SaveStore.Get<T>();

        /// <summary>
        ///     ストアが保持している現在のインスタンスを取得します。
        ///     キャッシュが無い初回アクセス時は、自動的に永続化データをロードします。
        /// </summary>
        /// <exception cref="SaveDataOperationException"> 初回の同期読み込みに失敗した場合。 </exception>
        public static SaveDataContent Get(Type dataType) => SaveStore.Get(dataType);

        /// <summary> 指定型の保存済みデータをキャッシュへ非同期に読み込む。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static ValueTask<T> LoadAsync<T>(CancellationToken token = default)
            where T : SaveDataContent, new() => SaveStore.LoadAsync<T>(token);

        /// <summary> 指定型の保存済みデータをキャッシュへ非同期に読み込む。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static ValueTask LoadAsync(Type dataType, CancellationToken token = default) =>
            SaveStore.LoadAsync(dataType, token);

        /// <summary> 指定型のキャッシュを保存先へ非同期に書き込む。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で保存に失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static ValueTask SaveAsync<T>(CancellationToken token = default)
            where T : SaveDataContent, new() => SaveStore.SaveAsync<T>(token);

        /// <summary> 指定型のキャッシュを保存先へ非同期に書き込む。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で保存に失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static ValueTask SaveAsync(Type dataType, CancellationToken token = default) =>
            SaveStore.SaveAsync(dataType, token);

        /// <summary> 指定型の保存済みデータを削除し、キャッシュを既定値へ戻す。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で削除または再読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static ValueTask DeleteAsync<T>(CancellationToken token = default)
            where T : SaveDataContent, new() => SaveStore.DeleteAsync<T>(token);

        /// <summary> 指定型の保存済みデータを削除し、キャッシュを既定値へ戻す。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で削除または再読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static ValueTask DeleteAsync(Type dataType, CancellationToken token = default) =>
            SaveStore.DeleteAsync(dataType, token);

        /// <summary>
        ///     現在キャッシュされている全エントリの読み取り専用スナップショットを取得する。
        ///     並び順は<see cref="Type.FullName" />のordinal昇順。
        /// </summary>
        public static IReadOnlyList<SaveDataRegistryEntryInfo> GetEntries() => SaveStore.GetEntries();
    }
}
