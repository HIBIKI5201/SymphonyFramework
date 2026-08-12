using System;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Exceptions;

using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータ操作の処理順と失敗時の変換を統括する。
    /// </summary>
    /// <remarks>
    ///     状態は<see cref="SaveDataEntryRegistry"/>、I/Oは<see cref="SaveDataLoaderStrategy"/>へ委譲する。
    /// </remarks>
    internal sealed class SaveDataService
    {
        #region 外部向けAPI

        /// <summary>
        ///     エントリの管理先とローダーの解決処理を指定して生成する。
        /// </summary>
        /// <param name="registry"> エントリを所有するレジストリ。 </param>
        /// <param name="loaderResolver"> 現在のConfigに対応するローダーを返す処理。 </param>
        public SaveDataService(
            SaveDataEntryRegistry registry,
            Func<SaveDataLoaderStrategy> loaderResolver)
        {
            // 状態管理とI/O解決のどちらが欠けても操作できないため、生成時に拒否する。
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _loaderResolver = loaderResolver ?? throw new ArgumentNullException(nameof(loaderResolver));
        }

        /// <summary> 表示内容が変わりうる操作の完了時に発行される。 </summary>
        /// <remarks> 操作を完了させたスレッドで発行する。 </remarks>
        internal event Action OnStateChanged;

        /// <summary>
        ///     指定型の永続化データが存在するか確認する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> 永続化データが存在する場合はtrue。 </returns>
        public bool Exists(Type dataType)
        {
            // 操作中に設定が変わっても同じI/O実装で例外文脈まで揃うよう、先にローダーを固定する。
            SaveDataLoaderStrategy loader = GetLoader();

            try
            {
                return loader.Exists(dataType);
            }
            catch (OperationCanceledException)
            {
                // キャンセルはI/O失敗へ変換せず、呼び出し側の制御フローとして維持する。
                throw;
            }
            catch (SaveDataOperationException)
            {
                // 既に操作文脈を持つ例外は二重に包まず、そのまま伝播する。
                throw;
            }
            catch (Exception ex)
            {
                // 保存先固有の例外へ操作・データ型・ローダー型を付与して公開する。
                throw new SaveDataOperationException(
                    SaveDataOperationEnum.Exists,
                    dataType,
                    loader.GetType(),
                    ex);
            }
        }

        /// <summary>
        ///     読み込み済みのインスタンスを取得する。
        /// </summary>
        /// <remarks>
        ///     PlayerLoopで進むLoaderを完了不能にしないよう、暗黙の同期読み込みは行わない。
        /// </remarks>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> 読み込み済みのインスタンス。 </returns>
        /// <exception cref="InvalidOperationException"> 対象型がまだ読み込まれていない場合。 </exception>
        public SaveDataContent Get(Type dataType)
        {
            // 既定値キャッシュを永続化データと誤認させないよう、明示的なロード完了を要求する。
            if (!_registry.IsLoaded(dataType))
            {
                throw new InvalidOperationException(
                    $"[{nameof(SaveStore)}] {dataType.Name} はまだ読み込まれていません。"
                    + $" 先に await {nameof(SaveStore)}.{nameof(SaveStore.LoadAsync)}<{dataType.Name}>() を呼んでください。");
            }

            return _registry.GetOrCreate(dataType).Content;
        }

        /// <summary>
        ///     指定型が読み込み済みか確認する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> 読み込み済みの場合はtrue。 </returns>
        public bool IsLoaded(Type dataType) => _registry.IsLoaded(dataType);

        /// <summary>
        ///     指定型の永続化データをキャッシュへ非同期に読み込む。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 読み込みの完了を表すTask。 </returns>
        public Task LoadAsync(Type dataType, CancellationToken token = default)
        {
            // 保存データが無い場合の復元先として、I/O開始前に既定値のキャッシュを確保する。
            SaveDataEntryEntity entry = _registry.GetOrCreate(dataType);

            // 同じ型のロードが進行中ならI/Oを重複させず、先行するTaskを共有する。
            if (_registry.TryGetLoadingTask(dataType, out Task loadingTask))
            {
                return loadingTask;
            }

            // 同期完了するLoaderでも完了済みTaskが登録に残らない手順で開始する。
            Task loadTask = LoadInternalAsync(dataType, entry.Content, token);
            _registry.RegisterLoadingTaskIfPending(dataType, loadTask);
            return loadTask;
        }

        /// <summary>
        ///     指定型のキャッシュを保存先へ非同期に書き込む。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 保存の完了を表すTask。 </returns>
        public async Task SaveAsync(Type dataType, CancellationToken token = default)
        {
            // 未アクセスの型でも既定値を保存できるよう、キャッシュを先に確保する。
            SaveDataEntryEntity entry = _registry.GetOrCreate(dataType);
            SaveDataContent content = entry.Content;
            SaveDataLoaderStrategy loader = GetLoader();

            // 保存日時の更新と復元をStrategyへ委ね、失敗には操作文脈を付与する。
            await ExecuteLoaderOperationAsync(
                SaveDataOperationEnum.Save,
                dataType,
                loader,
                () => loader.SaveAsync(dataType, content, token));

            // 保存成功時のキャッシュを読み込み済みとして公開し、表示へ反映する。
            _registry.MarkLoaded(dataType, content);
            RaiseStateChanged();
        }

        /// <summary>
        ///     指定型の永続化データを削除し、キャッシュを既定値へ戻す。
        /// </summary>
        /// <remarks> 削除または既定値への復元に失敗した場合は、未読み込み状態を維持する。 </remarks>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 削除の完了を表すTask。 </returns>
        public async Task DeleteAsync(Type dataType, CancellationToken token = default)
        {
            // 削除中のキャッシュを読み込み済みとして取得させないよう、I/Oより先に状態を解除する。
            SaveDataEntryEntity entry = _registry.GetOrCreate(dataType);
            SaveDataContent content = entry.Content;
            _registry.MarkUnloaded(dataType);

            // 削除と既定値生成を同じローダーへ固定し、保存形式固有の初期化規則を維持する。
            SaveDataLoaderStrategy loader = GetLoader();

            await ExecuteLoaderOperationAsync(
                SaveDataOperationEnum.Delete,
                dataType,
                loader,
                () => loader.DeleteAsync(dataType, token));

            // 保存値が無いロード経路を通し、Strategyが定める既定状態へ既存インスタンスを戻す。
            await ExecuteLoaderOperationAsync(
                SaveDataOperationEnum.Load,
                dataType,
                loader,
                () => loader.LoadAsync(dataType, content, token));

            // 削除後の既定状態を取得可能にし、表示へ操作完了を通知する。
            _registry.MarkLoaded(dataType, content);
            RaiseStateChanged();
        }

        /// <summary>
        ///     現在選択されているローダーを取得する。
        /// </summary>
        /// <remarks> 未解決の場合はresolverから取得する。 </remarks>
        /// <returns> 現在のローダー。 </returns>
        public SaveDataLoaderStrategy GetCurrentLoader() => GetLoader();

        /// <summary>
        ///     ローダーとキャッシュを破棄する。
        /// </summary>
        /// <remarks> 次回アクセス時にローダーを再解決する。 </remarks>
        public void Reset()
        {
            // 次回操作へ変更後のConfigを反映できるよう、解決済みローダーを先に破棄する。
            _cachedLoader = null;

            // 実際にキャッシュ状態が変わった場合だけ表示更新を発行する。
            int version = _registry.Version;
            _registry.Clear();

            // Clearで版番号が進んだ場合だけ、表示側へキャッシュ破棄を通知する。
            if (_registry.Version != version) { RaiseStateChanged(); }
        }

        #endregion

        #region 内部処理

        private readonly SaveDataEntryRegistry _registry;
        private readonly Func<SaveDataLoaderStrategy> _loaderResolver;

        private SaveDataLoaderStrategy _cachedLoader;

        /// <summary>
        ///     後始末を保証しながら対象データを読み込む。
        /// </summary>
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
                // 開始時点で解決したローダーへ読み込みを委譲し、成功した現在のキャッシュだけを公開する。
                SaveDataLoaderStrategy loader = GetLoader();
                await ExecuteLoaderOperationAsync(
                    SaveDataOperationEnum.Load,
                    dataType,
                    loader,
                    () => loader.LoadAsync(dataType, content, token));
                _registry.MarkLoaded(dataType, content);
            }
            finally
            {
                // 失敗やキャンセルでも重複ロード管理を解除し、変化した表示状態を通知する。
                _registry.RemoveLoadingTask(dataType);
                RaiseStateChanged();
            }
        }

        /// <summary>
        ///     状態変更を通知する。
        /// </summary>
        /// <remarks>
        ///     表示専用ViewModelの失敗を保存操作へ伝播させず、ReactivePropertyのスレッド例外も捕捉する。
        /// </remarks>
        private void RaiseStateChanged()
        {
            try
            {
                // ViewModelの更新を操作完了と同じスレッドで同期的に通知する。
                OnStateChanged?.Invoke();
            }
            catch (Exception exception)
            {
                // 表示側の失敗を完了済みの保存操作へ逆流させず、診断可能なログだけを残す。
                Debug.LogException(exception);
            }
        }

        /// <summary>
        ///     ローダー操作へ操作対象の文脈を付けて実行する。
        /// </summary>
        /// <param name="operation"> 実行する操作。 </param>
        /// <param name="dataType"> 操作対象のセーブデータ型。 </param>
        /// <param name="loader"> 操作に使用するローダー。 </param>
        /// <param name="execute"> 実行するローダー処理。 </param>
        /// <returns> 操作の完了を表すTask。 </returns>
        private static async Task ExecuteLoaderOperationAsync(
            SaveDataOperationEnum operation,
            Type dataType,
            SaveDataLoaderStrategy loader,
            Func<Task> execute)
        {
            try
            {
                await execute();
            }
            catch (OperationCanceledException)
            {
                // キャンセルを保存先の障害と区別できるよう、そのまま伝播する。
                throw;
            }
            catch (SaveDataOperationException)
            {
                // 下位層が付与済みの操作文脈を二重に包まない。
                throw;
            }
            catch (Exception ex)
            {
                // 保存形式やI/O固有の例外を、利用側が一貫して扱える例外へ変換する。
                throw new SaveDataOperationException(
                    operation,
                    dataType,
                    loader.GetType(),
                    ex);
            }
        }

        /// <summary>
        ///     現在のローダーを返す。
        /// </summary>
        /// <remarks> 未解決の場合はresolverから取得する。 </remarks>
        /// <returns> 現在のローダー。 </returns>
        private SaveDataLoaderStrategy GetLoader()
        {
            // 同じServiceの全操作で保存形式と保存先を固定するため、解決済みなら再利用する。
            if (_cachedLoader != null) { return _cachedLoader; }

            // Config未設定を後続のNullReferenceExceptionにせず、初期化契約の違反として通知する。
            _cachedLoader = _loaderResolver();
            if (_cachedLoader == null)
            {
                throw new InvalidOperationException(
                    $"[{nameof(SaveStore)}] ローダーの解決結果がnullです。");
            }

            return _cachedLoader;
        }

        #endregion
    }
}
