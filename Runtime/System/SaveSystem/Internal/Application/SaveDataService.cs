using System;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Exceptions;

using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータの存在確認、読み込み、保存、削除の処理順と失敗時の変換を担当する。
    ///     状態の保持は<see cref="SaveDataEntryRegistry"/>、保存先へのI/Oは
    ///     <see cref="SaveDataLoader"/>へ委譲する。
    /// </summary>
    internal sealed class SaveDataService
    {
        /// <summary>
        ///     エントリの管理先とローダーの解決処理を指定して生成する。
        /// </summary>
        /// <param name="registry"> エントリを所有するレジストリ。 </param>
        /// <param name="loaderResolver"> 現在のConfigに対応するローダーを返す処理。 </param>
        public SaveDataService(
            SaveDataEntryRegistry registry,
            Func<SaveDataLoader> loaderResolver)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _loaderResolver = loaderResolver ?? throw new ArgumentNullException(nameof(loaderResolver));
        }

        /// <summary>
        ///     表示内容が変わりうる操作が完了したときに発行される。
        ///     発行はその操作を完了させたスレッドで行う。
        /// </summary>
        internal event Action OnStateChanged;

        /// <summary> 指定型の永続化データが存在するか確認する。 </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> 永続化データが存在する場合はtrue。 </returns>
        public bool Exists(Type dataType)
        {
            SaveDataLoader loader = GetLoader();

            try
            {
                return loader.Exists(dataType);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SaveDataOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SaveDataOperationException(
                    SaveDataOperation.Exists,
                    dataType,
                    loader.GetType(),
                    ex);
            }
        }

        /// <summary>
        ///     キャッシュ済みインスタンスを取得する。未読み込みの場合は同期的に読み込む。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> キャッシュしているインスタンス。 </returns>
        public SaveDataContent Get(Type dataType)
        {
            SaveDataEntryEntity entry = _registry.GetOrCreate(dataType);

            if (!_registry.IsLoaded(dataType))
            {
                LoadAsync(dataType).GetAwaiter().GetResult();
            }

            return entry.Content;
        }

        /// <summary> 指定型の永続化データをキャッシュへ非同期に読み込む。 </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 読み込みの完了を表すValueTask。 </returns>
        public ValueTask LoadAsync(Type dataType, CancellationToken token = default)
        {
            SaveDataEntryEntity entry = _registry.GetOrCreate(dataType);

            if (_registry.TryGetLoadingTask(dataType, out Task loadingTask))
            {
                return new ValueTask(loadingTask);
            }

            Task loadTask = LoadInternalAsync(dataType, entry.Content, token);
            _registry.RegisterLoadingTaskIfPending(dataType, loadTask);
            return new ValueTask(loadTask);
        }

        /// <summary> 指定型のキャッシュを保存先へ非同期に書き込む。 </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 保存の完了を表すValueTask。 </returns>
        public async ValueTask SaveAsync(Type dataType, CancellationToken token = default)
        {
            SaveDataEntryEntity entry = _registry.GetOrCreate(dataType);
            SaveDataContent content = entry.Content;
            SaveDataLoader loader = GetLoader();

            await ExecuteLoaderOperationAsync(
                SaveDataOperation.Save,
                dataType,
                loader,
                () => loader.SaveAsync(dataType, content, token));

            _registry.MarkLoaded(dataType, content);
            RaiseStateChanged();
        }

        /// <summary> 指定型の永続化データを削除し、キャッシュを既定値へ戻す。 </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 削除の完了を表すValueTask。 </returns>
        public async ValueTask DeleteAsync(Type dataType, CancellationToken token = default)
        {
            SaveDataEntryEntity entry = _registry.GetOrCreate(dataType);
            SaveDataContent content = entry.Content;
            _registry.MarkUnloaded(dataType);

            SaveDataLoader loader = GetLoader();

            await ExecuteLoaderOperationAsync(
                SaveDataOperation.Delete,
                dataType,
                loader,
                () => loader.DeleteAsync(dataType, token));

            await ExecuteLoaderOperationAsync(
                SaveDataOperation.Load,
                dataType,
                loader,
                () => loader.LoadAsync(dataType, content, token));

            _registry.MarkLoaded(dataType, content);
            RaiseStateChanged();
        }

        /// <summary>
        ///     現在選択されているローダーを取得する。未解決の場合はresolverから取得する。
        /// </summary>
        /// <returns> 現在のローダー。 </returns>
        public SaveDataLoader GetCurrentLoader() => GetLoader();

        /// <summary> ローダーとキャッシュを破棄し、次回アクセス時に再解決させる。 </summary>
        public void Reset()
        {
            _cachedLoader = null;

            int version = _registry.Version;
            _registry.Clear();

            if (_registry.Version != version)
            {
                RaiseStateChanged();
            }
        }

        /// <summary> 重複ロード管理の後始末を保証しながら対象データを読み込む。 </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="content"> 読み込み先のインスタンス。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 読み込みの完了を表すTask。 </returns>
        private async Task LoadInternalAsync(
            Type dataType,
            SaveDataContent content,
            CancellationToken token)
        {
            try
            {
                SaveDataLoader loader = GetLoader();
                await ExecuteLoaderOperationAsync(
                    SaveDataOperation.Load,
                    dataType,
                    loader,
                    () => loader.LoadAsync(dataType, content, token));
                _registry.MarkLoaded(dataType, content);
            }
            finally
            {
                _registry.RemoveLoadingTask(dataType);
                RaiseStateChanged();
            }
        }

        /// <summary>
        ///     状態変更を通知する。購読側の例外はここで止める。
        ///     購読しているのは表示専用のViewModelであり、その失敗を保存や読み込みの失敗にしない。
        ///     メインスレッド外で完了した場合のReactivePropertyの例外もここで捕捉する。
        /// </summary>
        private void RaiseStateChanged()
        {
            try
            {
                OnStateChanged?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary> ローダー操作へセーブデータ型とローダー型の文脈を付けて実行する。 </summary>
        /// <param name="operation"> 実行する操作。 </param>
        /// <param name="dataType"> 操作対象のセーブデータ型。 </param>
        /// <param name="loader"> 操作に使用するローダー。 </param>
        /// <param name="execute"> 実行するローダー処理。 </param>
        /// <returns> 操作の完了を表すValueTask。 </returns>
        private static async ValueTask ExecuteLoaderOperationAsync(
            SaveDataOperation operation,
            Type dataType,
            SaveDataLoader loader,
            Func<ValueTask> execute)
        {
            try
            {
                await execute();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SaveDataOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SaveDataOperationException(
                    operation,
                    dataType,
                    loader.GetType(),
                    ex);
            }
        }

        /// <summary> キャッシュ済みローダーを返し、未解決の場合はresolverから取得する。 </summary>
        /// <returns> 現在のローダー。 </returns>
        private SaveDataLoader GetLoader()
        {
            if (_cachedLoader != null)
            {
                return _cachedLoader;
            }

            _cachedLoader = _loaderResolver();
            if (_cachedLoader == null)
            {
                throw new InvalidOperationException(
                    $"[{nameof(SaveDataRegistry)}] ローダーの解決結果がnullです。");
            }

            return _cachedLoader;
        }

        private readonly SaveDataEntryRegistry _registry;
        private readonly Func<SaveDataLoader> _loaderResolver;

        private SaveDataLoader _cachedLoader;
    }
}
