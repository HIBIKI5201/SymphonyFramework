using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.Utility;

using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     プロジェクト設定に従ってセーブデータを一括管理する。
    /// </summary>
    /// <remarks> 引数を検証して<see cref="SaveDataService"/>へ転送し、状態は保持しない。 </remarks>
    public static class SaveStore
    {
        #region 外部向けAPI

        /// <summary>
        ///     指定型の永続化データが存在するか確認する。
        /// </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で存在確認に失敗した場合。 </exception>
        public static bool Exists<T>() where T : SaveDataContent, new()
        {
            // ジェネリック制約で保証した型をType版の共通検証と処理へ渡す。
            return Exists(typeof(T));
        }

        /// <summary>
        ///     指定型の永続化データが存在するか確認する。
        /// </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で存在確認に失敗した場合。 </exception>
        public static bool Exists(Type dataType)
        {
            // 保存先へ副作用を発生させる前に公開APIの型契約を検証する。
            ValidateDataType(dataType);
            return EnsureInitialized().Exists(dataType);
        }

        /// <summary>
        ///     読み込み済みのインスタンスを取得する。
        /// </summary>
        /// <remarks> 暗黙の読み込みは行わず、初回取得前に<see cref="LoadAsync{T}"/>をawaitする。 </remarks>
        /// <exception cref="InvalidOperationException"> 対象型がまだ読み込まれていない場合。 </exception>
        public static T Get<T>() where T : SaveDataContent, new()
        {
            // 型安全な戻り値を提供しつつ、取得条件はType版へ集約する。
            return (T)Get(typeof(T));
        }

        /// <summary>
        ///     読み込み済みのインスタンスを取得する。
        /// </summary>
        /// <remarks>
        ///     暗黙の読み込みは行わず、初回取得前に<see cref="LoadAsync(Type, CancellationToken)"/>をawaitする。
        /// </remarks>
        /// <exception cref="InvalidOperationException"> 対象型がまだ読み込まれていない場合。 </exception>
        public static SaveDataContent Get(Type dataType)
        {
            // 未読込時に既定値キャッシュを返さないよう、Serviceの明示的な取得経路へ渡す。
            ValidateDataType(dataType);
            return EnsureInitialized().Get(dataType);
        }

        /// <summary>
        ///     指定型が読み込み済みか確認する。
        /// </summary>
        public static bool IsLoaded<T>() where T : SaveDataContent, new()
        {
            // ジェネリック型をType版へ集約し、読み込み済み判定を一経路に保つ。
            return IsLoaded(typeof(T));
        }

        /// <summary>
        ///     指定型が読み込み済みか確認する。
        /// </summary>
        public static bool IsLoaded(Type dataType)
        {
            // 未初期化や無効な型を読み込み状態のfalseと混同させず、契約違反として通知する。
            ValidateDataType(dataType);
            return EnsureInitialized().IsLoaded(dataType);
        }

        /// <summary>
        ///     指定型の保存済みデータをキャッシュへ非同期に読み込む。
        /// </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static Awaitable<T> LoadAsync<T>(CancellationToken token = default)
            where T : SaveDataContent, new()
        {
            // Unity Awaitableへ変換する前に型と初期化状態を確定する。
            ValidateDataType(typeof(T));
            SaveDataService service = EnsureInitialized();

            // 読み込み後の同じキャッシュを型付きで返すTaskを、呼び出しごとのAwaitableへ変換する。
            return SymphonyAwaitable.FromTask(LoadAndGetAsync<T>(service, token));
        }

        /// <summary>
        ///     指定型の保存済みデータをキャッシュへ非同期に読み込む。
        /// </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static Awaitable LoadAsync(Type dataType, CancellationToken token = default)
        {
            // 無効な型ではロードTaskや既定値キャッシュを生成しない。
            ValidateDataType(dataType);

            // 重複ロード中は同じTaskが返るが、FromTaskが呼び出しごとに新しい
            // Awaitableを作るため、Awaitableを共有できないという制約と両立する。
            return SymphonyAwaitable.FromTask(EnsureInitialized().LoadAsync(dataType, token));
        }

        /// <summary>
        ///     指定型のキャッシュを保存先へ非同期に書き込む。
        /// </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で保存に失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static Awaitable SaveAsync<T>(CancellationToken token = default)
            where T : SaveDataContent, new() => SaveAsync(typeof(T), token);

        /// <summary>
        ///     指定型のキャッシュを保存先へ非同期に書き込む。
        /// </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で保存に失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static Awaitable SaveAsync(Type dataType, CancellationToken token = default)
        {
            // 保存日時や保存先を変更する前に、公開APIの型契約を検証する。
            ValidateDataType(dataType);
            return SymphonyAwaitable.FromTask(EnsureInitialized().SaveAsync(dataType, token));
        }

        /// <summary>
        ///     指定型の保存済みデータを削除し、キャッシュを既定値へ戻す。
        /// </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で削除または再読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static Awaitable DeleteAsync<T>(CancellationToken token = default)
            where T : SaveDataContent, new() => DeleteAsync(typeof(T), token);

        /// <summary>
        ///     指定型の保存済みデータを削除し、キャッシュを既定値へ戻す。
        /// </summary>
        /// <exception cref="SaveDataOperationException"> ローダーまたは保存先で削除または再読み込みに失敗した場合。 </exception>
        /// <exception cref="OperationCanceledException"> 呼び出し側から処理が中断された場合。 </exception>
        public static Awaitable DeleteAsync(Type dataType, CancellationToken token = default)
        {
            // 削除やキャッシュ状態の変更を始める前に、公開APIの型契約を検証する。
            ValidateDataType(dataType);
            return SymphonyAwaitable.FromTask(EnsureInitialized().DeleteAsync(dataType, token));
        }

        /// <summary>
        ///     キャッシュされている全エントリのスナップショットを取得する。
        /// </summary>
        /// <returns> <see cref="Type.FullName"/>のordinal昇順で並んだ読み取り専用一覧。 </returns>
        public static IReadOnlyList<SaveDataEntryInfo> GetEntries()
        {
            // 未初期化時は状態参照だけで初期化例外を出さず、空の不変スナップショットを返す。
            return _query?.GetInfos() ?? Array.Empty<SaveDataEntryInfo>();
        }

        #endregion

        #region 内部処理

        private static SaveDataService _service;
        private static SaveDataQuery _query;
        private static SaveDataViewModel _viewModel;
        private static SaveDataViewStore _viewStore;

        /// <summary> Save Storeが初期化済みかどうか。 </summary>
        internal static bool IsInitialized => _service != null;

        /// <summary> 状態表示に接続するViewModel。 </summary>
        /// <remarks>
        ///     未初期化の場合はnull。<see cref="OnCurrentViewModelChanged"/>で差し替えを検知する。
        /// </remarks>
        internal static SaveDataViewModel CurrentViewModel => _viewModel;

        /// <summary> Viewの操作と問い合わせを受け付ける現在のStore。 </summary>
        /// <remarks> 未初期化の場合はnull。操作のたびに現在値を取得する。 </remarks>
        internal static SaveDataViewStore CurrentViewStore => _viewStore;

        /// <summary> <see cref="CurrentViewModel"/>が差し替わったときに発行される。 </summary>
        /// <remarks> Save DataはEdit Modeでも初期化されるため、Play Mode遷移以外でも発行される。 </remarks>
        internal static event Action OnCurrentViewModelChanged;

        /// <summary>
        ///     ローダーとキャッシュを破棄する。
        /// </summary>
        /// <remarks> EditorのProject Settingsから呼び出し、次回アクセス時にConfigから再解決する。 </remarks>
        internal static void RefreshLoader()
        {
            // Project Settingsの変更を次回アクセス時のローダー解決へ反映する。
            ResetRuntimeState();
        }

        /// <summary>
        ///     Compositionが解決したローダーの取得処理を設定する。
        /// </summary>
        /// <param name="loaderResolver"> 現在のConfigに対応するローダーを返す処理。 </param>
        internal static void ConfigureLoaderResolver(
            Func<SaveDataLoaderStrategy> loaderResolver)
        {
            // 初期化不能な構成を作らないよう、解決処理が無い設定を拒否する。
            if (loaderResolver == null) { throw new ArgumentNullException(nameof(loaderResolver)); }

            // 再設定前の購読とキャッシュを解放し、古いConfigの状態を残さない。
            _viewModel?.Dispose();
            _service?.Reset();

            // 同じRegistryをCommand、Query、ViewModel、ViewStoreで共有し、状態と表示の参照先を揃える。
            SaveDataEntryRegistry registry = new();
            _service = new SaveDataService(registry, loaderResolver);
            _query = new SaveDataQuery(registry);
            _viewModel = new SaveDataViewModel(_query, _service);
            _viewStore = new SaveDataViewStore(_query, _service);

            // Editor表示が新しいViewModelとViewStoreを取得できるよう、Composition完了後に差し替えを通知する。
            OnCurrentViewModelChanged?.Invoke();
        }

        /// <summary>
        ///     ランタイム状態を初期化する。
        /// </summary>
        /// <remarks> Domain Reloadの有無に依存せず同じ状態へ戻す。 </remarks>
        internal static void ResetRuntimeState()
        {
            // Composition自体は維持し、ローダーとキャッシュだけを次回利用前の状態へ戻す。
            _service?.Reset();
        }

        /// <summary>
        ///     現在選択されているローダーを取得する。
        /// </summary>
        /// <remarks> Adaptorが選んだ実装は公開せず、状態表示用のEditorウィンドウからのみ参照する。 </remarks>
        internal static SaveDataLoaderStrategy GetCurrentLoader() => EnsureInitialized().GetCurrentLoader();

        /// <summary>
        ///     読み込み完了後のインスタンスを返す。
        /// </summary>
        /// <typeparam name="T"> 対象のセーブデータ型。 </typeparam>
        /// <param name="service"> 処理を委譲するService。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 読み込み後のインスタンス。 </returns>
        private static async Task<T> LoadAndGetAsync<T>(
            SaveDataService service,
            CancellationToken token)
            where T : SaveDataContent, new()
        {
            // ロード完了を待ってから、Serviceが読み込み済みと確認した同じキャッシュを取得する。
            await service.LoadAsync(typeof(T), token);
            return (T)service.Get(typeof(T));
        }

        /// <summary>
        ///     Compositionからローダーが注入済みであることを確認する。
        /// </summary>
        /// <returns> 処理を委譲するService。 </returns>
        private static SaveDataService EnsureInitialized()
        {
            // 未初期化をNullReferenceExceptionへ遅延させず、必要なCompositionを明示する例外へ変換する。
            return _service ?? throw new SymphonyNotInitializedException(typeof(SaveStore));
        }

        /// <summary>
        ///     ストアで扱える既定コンストラクタ付き具象型か検証する。
        /// </summary>
        private static void ValidateDataType(Type dataType)
        {
            // 型情報が無い要求はリフレクションへ渡さず、引数違反として通知する。
            if (dataType == null) { throw new ArgumentNullException(nameof(dataType)); }

            // 抽象型や未構築のジェネリック型は既定値キャッシュを生成できないため拒否する。
            if (!dataType.IsClass || dataType.IsAbstract || dataType.IsGenericTypeDefinition)
            {
                throw new ArgumentException("セーブ対象は new() 可能な具象クラスにしてください。", nameof(dataType));
            }

            // 保存値が無い場合に既定インスタンスを生成できることを保証する。
            if (dataType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException("デフォルトコンストラクタが必要です。", nameof(dataType));
            }

            // 保存日時と破棄の共通ライフサイクルを持たない型は保存対象にしない。
            if (!typeof(SaveDataContent).IsAssignableFrom(dataType))
            {
                throw new ArgumentException($"{nameof(SaveDataContent)} を継承した型を指定してください。", nameof(dataType));
            }
        }

        #endregion
    }
}
