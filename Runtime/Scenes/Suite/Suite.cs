using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFramework.Debugger.Logger;
using SymphonyFramework.Exceptions;
using SymphonyFramework.Utility;

using UnityEngine;

namespace SymphonyFramework.Scenes
{
    /// <summary>
    ///     Suite単位のロード、アンロード、状態照会を提供する公開エントリポイント。
    /// </summary>
    /// <remarks>
    ///     **ブロック単位のロードと <see cref="Attacca" /> による単体ロードは併用できる。**
    ///     ブロックは自分が保持しているシーンだけをアンロードし、
    ///     ブロック外でロードされたシーンには手を触れない。
    /// </remarks>
    public static class Suite
    {
        #region 外部向けAPI

        /// <summary>
        ///     ブロックの全シーンを依存順にロードする。
        /// </summary>
        /// <param name="block"> ロードするSuiteアセット。 </param>
        /// <param name="progress"> ブロック全体の進捗の通知先。 </param>
        /// <param name="token"> ロード処理を中断するためのトークン。 </param>
        /// <returns> 全シーンをロードできた場合はtrue。 </returns>
        /// <exception cref="ArgumentNullException"> blockがnullの場合。 </exception>
        /// <exception cref="ArgumentException"> blockにエントリが1件も無い場合。 </exception>
        /// <exception cref="SuitePlanException"> 依存グラフを解決できない場合。 </exception>
        /// <exception cref="InvalidOperationException"> 同名の別アセットが追跡中、または同じブロックがアンロード中の場合。 </exception>
        /// <exception cref="SymphonyNotInitializedException"> Suiteが未初期化の場合。 </exception>
        public static Awaitable<bool> LoadAsync(
            SuiteAsset block,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            // 非同期処理を開始する前に、同期的に引数と初期化状態を検証する。
            EnsureInitialized();
            ValidateBlock(block);

            return SymphonyAwaitable.FromTask(
                _service.LoadBlock(
                    block.SuiteName,
                    block.GetInstanceID(),
                    block.Entries,
                    progress,
                    token));
        }

        /// <summary>
        ///     ブロックの保持を解き、保持が残らないシーンだけをアンロードする。
        /// </summary>
        /// <param name="block"> アンロードするSuiteアセット。 </param>
        /// <param name="progress"> ブロック全体の進捗の通知先。 </param>
        /// <param name="token"> アンロード処理を中断するためのトークン。 </param>
        /// <returns> アンロードが必要なシーンをすべて処理できた場合はtrue。 </returns>
        /// <exception cref="ArgumentNullException"> blockがnullの場合。 </exception>
        /// <exception cref="InvalidOperationException"> 同名の別アセットが追跡中、または同じブロックがロード中の場合。 </exception>
        /// <exception cref="SymphonyNotInitializedException"> Suiteが未初期化の場合。 </exception>
        public static Awaitable<bool> UnloadAsync(
            SuiteAsset block,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            EnsureInitialized();

            // アンロードは既存の追跡だけを見るため、エントリの中身は要求しない。
            if (block == null) { throw new ArgumentNullException(nameof(block)); }

            return SymphonyAwaitable.FromTask(
                _service.UnloadBlock(
                    block.SuiteName,
                    block.GetInstanceID(),
                    progress,
                    token));
        }

        /// <summary>
        ///     ブロックが追跡中か確認する。
        /// </summary>
        /// <param name="suiteName"> 確認するブロック名。 </param>
        /// <returns> 追跡中の場合はtrue。 </returns>
        /// <exception cref="SymphonyNotInitializedException"> Suiteが未初期化の場合。 </exception>
        public static bool IsLoaded(string suiteName)
        {
            // Entityを公開せず、Queryが生成する不変な状態の存在だけを確認する。
            EnsureInitialized();
            return _query.TryGetInfo(suiteName, out _);
        }

        /// <summary>
        ///     ブロックの状態スナップショットを取得する。
        /// </summary>
        /// <param name="suiteName"> 取得するブロック名。 </param>
        /// <param name="suiteInfo"> 取得できたスナップショット。 </param>
        /// <returns> 取得できた場合はtrue。 </returns>
        /// <exception cref="SymphonyNotInitializedException"> Suiteが未初期化の場合。 </exception>
        public static bool TryGetInfo(string suiteName, out SuiteInfo suiteInfo)
        {
            EnsureInitialized();
            return _query.TryGetInfo(suiteName, out suiteInfo);
        }

        /// <summary>
        ///     追跡中の全ブロックの状態スナップショットを取得する。
        /// </summary>
        /// <returns> ブロック名のOrdinal順に並んだスナップショット。 </returns>
        /// <exception cref="SymphonyNotInitializedException"> Suiteが未初期化の場合。 </exception>
        public static IReadOnlyList<SuiteInfo> GetInfos()
        {
            EnsureInitialized();
            return _query.GetInfos();
        }

        #endregion

        #region 内部処理

        private static SuiteRegistry _registry;
        private static SuiteService _service;
        private static SuiteQuery _query;
        private static SuiteViewModel _viewModel;

        /// <summary> Suiteが初期化済みかどうか。 </summary>
        internal static bool IsInitialized =>
            _registry != null && _service != null && _query != null && _viewModel != null;

        /// <summary>
        ///     いずれかのSuiteが保持中のシーンか判定する。
        /// </summary>
        /// <param name="sceneName"> 判定するシーン名。 </param>
        /// <returns> 保持中の場合はtrue。未初期化の場合はfalse。 </returns>
        /// <remarks> Attaccaからの単体アンロードを止めるため、Orchestratorが判定として渡す。 </remarks>
        internal static bool IsSceneHeld(string sceneName) =>
            _registry != null
            && !string.IsNullOrWhiteSpace(sceneName)
            && _registry.IsHeldByAnyBlock(sceneName);

        /// <summary> Compositionが所有する現在のSuite ViewModel。 </summary>
        internal static SuiteViewModel CurrentViewModel => _viewModel;

        /// <summary>
        ///     OrchestratorからSuiteを初期化する。
        /// </summary>
        /// <param name="sceneLoadService"> シーン操作を実行するScene Load Service。 </param>
        /// <exception cref="ArgumentNullException"> sceneLoadServiceがnullの場合。 </exception>
        internal static void Initialize(SceneLoadService sceneLoadService)
        {
            // Domain Reloadなしの再初期化でも古い保持情報を残さない。
            ResetRuntimeState();

            _registry = new SuiteRegistry();
            _service = new SuiteService(
                _registry,
                new SceneLoadServiceLoader(sceneLoadService));
            _query = new SuiteQuery(_registry);
            _viewModel = new SuiteViewModel(_query, _service);
        }

        /// <summary>
        ///     追跡状態とServiceを破棄して未初期化状態へ戻す。
        /// </summary>
        internal static void ResetRuntimeState()
        {
            // 購読を先に解除してから保持情報を消し、Compositionの参照をすべて切る。
            _viewModel?.Dispose();
            _registry?.Clear();
            _viewModel = null;
            _query = null;
            _service = null;
            _registry = null;
        }

        /// <summary>
        ///     起動時にロードするSuiteが持つシーン名を集める。
        /// </summary>
        /// <param name="suites"> 起動時にロードするSuite。nullの要素は無視する。 </param>
        /// <returns> 重複を除いたシーン名。 </returns>
        internal static IReadOnlyCollection<string> CollectSceneNames(IReadOnlyList<SuiteAsset> suites)
        {
            HashSet<string> sceneNames = new(StringComparer.Ordinal);
            if (suites == null) { return sceneNames; }

            for (int i = 0; i < suites.Count; i++)
            {
                if (suites[i] == null) { continue; }

                IReadOnlyList<SuiteEntry> entries = suites[i].Entries;
                for (int j = 0; j < entries.Count; j++)
                {
                    if (entries[j] != null && !string.IsNullOrWhiteSpace(entries[j].SceneName))
                    {
                        sceneNames.Add(entries[j].SceneName);
                    }
                }
            }

            return sceneNames;
        }

        /// <summary>
        ///     起動時のSuiteを設定順にロードする。
        /// </summary>
        /// <param name="suites"> 起動時にロードするSuite。nullの要素は無視する。 </param>
        /// <returns> すべてロードできた場合はtrue。 </returns>
        /// <remarks>
        ///     起動処理は同期入口から投げっぱなしで呼ばれるため、例外は呼び出し元へ返さずログへ出し、falseを返す。
        ///     1件でも失敗した時点で残りのSuiteはロードしない。
        /// </remarks>
        internal static async Task<bool> LoadInitialAsync(IReadOnlyList<SuiteAsset> suites)
        {
            if (suites == null) { return true; }

            for (int i = 0; i < suites.Count; i++)
            {
                SuiteAsset suite = suites[i];
                if (suite == null) { continue; }

                try
                {
                    EnsureInitialized();
                    ValidateBlock(suite);

                    bool isLoaded = await _service.LoadBlock(
                        suite.SuiteName,
                        suite.GetInstanceID(),
                        suite.Entries,
                        null,
                        CancellationToken.None);
                    if (!isLoaded) { return false; }
                }
                catch (Exception exception)
                {
                    SymphonyDebugLogger.LogException(exception);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Suiteが利用可能な状態か検証する。
        /// </summary>
        /// <exception cref="SymphonyNotInitializedException"> 未初期化の場合。 </exception>
        private static void EnsureInitialized()
        {
            // Compositionの一部でも欠けている場合は、不完全な状態で処理を続行させない。
            if (!IsInitialized) { throw new SymphonyNotInitializedException(typeof(Suite)); }
        }

        /// <summary>
        ///     ロード対象のアセットが処理を開始できる内容か検証する。
        /// </summary>
        /// <param name="block"> 検証するアセット。 </param>
        /// <exception cref="ArgumentNullException"> blockがnullの場合。 </exception>
        /// <exception cref="ArgumentException"> エントリが1件も無い場合。 </exception>
        private static void ValidateBlock(SuiteAsset block)
        {
            if (block == null) { throw new ArgumentNullException(nameof(block)); }

            // 空のブロックはロードしても何も起きないため、開始前に呼び出し側へ知らせる。
            if (block.Entries.Count == 0)
            {
                throw new ArgumentException(
                    $"Suite {block.SuiteName} にエントリがありません。",
                    nameof(block));
            }
        }

        #endregion
    }
}
