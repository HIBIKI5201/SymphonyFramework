using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Exceptions;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     プロジェクト設定に従ってセーブデータを一括管理するレジストリです。
    ///     引数の検証と<see cref="SaveDataService"/>への転送だけを行い、状態は保持しません。
    /// </summary>
    public static class SaveDataRegistry
    {
        /// <summary> 指定型の永続化データが存在するか確認する。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で存在確認に失敗した場合。 </exception>
        public static bool Exists<T>() where T : SaveDataContent, new()
        {
            return Exists(typeof(T));
        }

        /// <summary> 指定型の永続化データが存在するか確認する。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で存在確認に失敗した場合。 </exception>
        public static bool Exists(Type dataType)
        {
            ValidateDataType(dataType);
            return EnsureInitialized().Exists(dataType);
        }

        /// <summary> 指定型のキャッシュまたは保存済みデータを同期的に取得する。 </summary>
        /// <exception cref="SaveDataOperationException"> 初回の同期読み込みに失敗した場合。 </exception>
        public static T Get<T>() where T : SaveDataContent, new()
        {
            return (T)Get(typeof(T));
        }

        /// <summary>
        ///     Registry が保持している現在のインスタンスを取得します。
        ///     キャッシュが無い初回アクセス時は、自動的に永続化データをロードします。
        /// </summary>
        /// <exception cref="SaveDataOperationException"> 初回の同期読み込みに失敗した場合。 </exception>
        public static SaveDataContent Get(Type dataType)
        {
            ValidateDataType(dataType);
            return EnsureInitialized().Get(dataType);
        }

        /// <summary> 指定型の保存済みデータをキャッシュへ非同期に読み込む。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static async ValueTask<T> LoadAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
        {
            await LoadAsync(typeof(T), token);
            return Get<T>();
        }

        /// <summary> 指定型の保存済みデータをキャッシュへ非同期に読み込む。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static ValueTask LoadAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            return EnsureInitialized().LoadAsync(dataType, token);
        }

        /// <summary> 指定型のキャッシュを保存先へ非同期に書き込む。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で保存に失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static ValueTask SaveAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
        {
            return SaveAsync(typeof(T), token);
        }

        /// <summary> 指定型のキャッシュを保存先へ非同期に書き込む。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で保存に失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static ValueTask SaveAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            return EnsureInitialized().SaveAsync(dataType, token);
        }

        /// <summary> 指定型の保存済みデータを削除し、キャッシュを既定値へ戻す。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で削除または再読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static async ValueTask DeleteAsync<T>(CancellationToken token = default) where T : SaveDataContent, new()
        {
            await DeleteAsync(typeof(T), token);
        }

        /// <summary> 指定型の保存済みデータを削除し、キャッシュを既定値へ戻す。 </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で削除または再読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static ValueTask DeleteAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            return EnsureInitialized().DeleteAsync(dataType, token);
        }

        /// <summary> 現在キャッシュされている全エントリの読み取り専用スナップショットを取得する。 </summary>
        public static IReadOnlyList<SaveDataRegistryEntryInfo> GetEntries()
        {
            return _service?.GetEntries() ?? Array.Empty<SaveDataRegistryEntryInfo>();
        }

        /// <summary> Save Data Registryが初期化済みかどうか。 </summary>
        internal static bool IsInitialized => _service != null;

        /// <summary> 読み込み済みとして記録されているセーブデータ型のスナップショット。 </summary>
        internal static IReadOnlyCollection<Type> LoadedTypes
        {
            get => _service?.GetLoadedTypes() ?? Array.Empty<Type>();
        }

        /// <summary>
        ///     ローダーとキャッシュを破棄し、次回アクセス時にConfigから再解決する。
        ///     Configを編集するEditorのProject Settings画面から呼び出す。
        /// </summary>
        internal static void RefreshLoader()
        {
            ResetRuntimeState();
        }

        /// <summary>
        ///     Compositionが解決したローダーの取得処理を設定する。
        /// </summary>
        /// <param name="loaderResolver"> 現在のConfigに対応するローダーを返す処理。 </param>
        internal static void ConfigureLoaderResolver(
            Func<SaveDataLoader> loaderResolver)
        {
            if (loaderResolver == null)
            {
                throw new ArgumentNullException(nameof(loaderResolver));
            }

            _service?.Reset();
            _service = new SaveDataService(new SaveDataEntryRegistry(), loaderResolver);
        }

        /// <summary> Domain Reloadの有無に依存しないようランタイム状態を初期化する。 </summary>
        internal static void ResetRuntimeState()
        {
            _service?.Reset();
        }

        /// <summary>
        ///     現在選択されているローダーを取得する。
        ///     Adaptorが選んだ実装は公開APIへ出さず、状態を表示するEditorウィンドウからのみ参照する。
        /// </summary>
        internal static SaveDataLoader GetCurrentLoader() => EnsureInitialized().GetCurrentLoader();

        /// <summary> Compositionからローダーが注入済みであることを確認する。 </summary>
        /// <returns> 処理を委譲するService。 </returns>
        private static SaveDataService EnsureInitialized()
        {
            return _service ?? throw new SymphonyNotInitializedException(typeof(SaveDataRegistry));
        }

        /// <summary> レジストリで扱えるデフォルトコンストラクタ付き具象型か検証する。 </summary>
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

        private static SaveDataService _service;
    }
}
