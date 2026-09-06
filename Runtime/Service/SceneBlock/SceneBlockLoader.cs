using System;
using System.Collections.Generic;
using System.Threading;

using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.System.SceneLoad;
using SymphonyFrameWork.Utility;

using UnityEngine;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Block単位のロード、アンロード、状態照会を提供する公開エントリポイント。
    /// </summary>
    /// <remarks>
    ///     **ブロック単位のロードと <see cref="SceneLoader" /> による単体ロードは併用できる。**
    ///     ブロックは自分が保持しているシーンだけをアンロードし、
    ///     ブロック外でロードされたシーンには手を触れない。
    /// </remarks>
    public static class SceneBlockLoader
    {
        #region 外部向けAPI

        /// <summary>
        ///     ブロックの全シーンを依存順にロードする。
        /// </summary>
        /// <param name="block"> ロードするScene Blockアセット。 </param>
        /// <param name="progress"> ブロック全体の進捗の通知先。 </param>
        /// <param name="token"> ロード処理を中断するためのトークン。 </param>
        /// <returns> 全シーンをロードできた場合はtrue。 </returns>
        /// <exception cref="ArgumentNullException"> blockがnullの場合。 </exception>
        /// <exception cref="ArgumentException"> blockにエントリが1件も無い場合。 </exception>
        /// <exception cref="SceneBlockPlanException"> 依存グラフを解決できない場合。 </exception>
        /// <exception cref="InvalidOperationException"> 同名の別アセットが追跡中、または同じブロックがアンロード中の場合。 </exception>
        /// <exception cref="SymphonyNotInitializedException"> Scene Block Loaderが未初期化の場合。 </exception>
        public static Awaitable<bool> LoadAsync(
            SceneBlockAsset block,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            // 非同期処理を開始する前に、同期的に引数と初期化状態を検証する。
            EnsureInitialized();
            ValidateBlock(block);

            return SymphonyAwaitable.FromTask(
                _service.LoadBlock(
                    block.BlockName,
                    block.GetInstanceID(),
                    block.Entries,
                    progress,
                    token));
        }

        /// <summary>
        ///     ブロックの保持を解き、保持が残らないシーンだけをアンロードする。
        /// </summary>
        /// <param name="block"> アンロードするScene Blockアセット。 </param>
        /// <param name="progress"> ブロック全体の進捗の通知先。 </param>
        /// <param name="token"> アンロード処理を中断するためのトークン。 </param>
        /// <returns> アンロードが必要なシーンをすべて処理できた場合はtrue。 </returns>
        /// <exception cref="ArgumentNullException"> blockがnullの場合。 </exception>
        /// <exception cref="InvalidOperationException"> 同名の別アセットが追跡中、または同じブロックがロード中の場合。 </exception>
        /// <exception cref="SymphonyNotInitializedException"> Scene Block Loaderが未初期化の場合。 </exception>
        public static Awaitable<bool> UnloadAsync(
            SceneBlockAsset block,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            EnsureInitialized();

            // アンロードは既存の追跡だけを見るため、エントリの中身は要求しない。
            if (block == null) { throw new ArgumentNullException(nameof(block)); }

            return SymphonyAwaitable.FromTask(
                _service.UnloadBlock(
                    block.BlockName,
                    block.GetInstanceID(),
                    progress,
                    token));
        }

        /// <summary>
        ///     ブロックが追跡中か確認する。
        /// </summary>
        /// <param name="blockName"> 確認するブロック名。 </param>
        /// <returns> 追跡中の場合はtrue。 </returns>
        /// <exception cref="SymphonyNotInitializedException"> Scene Block Loaderが未初期化の場合。 </exception>
        public static bool IsLoaded(string blockName)
        {
            // Entityを公開せず、Queryが生成する不変な状態の存在だけを確認する。
            EnsureInitialized();
            return _query.TryGetInfo(blockName, out _);
        }

        /// <summary>
        ///     ブロックの状態スナップショットを取得する。
        /// </summary>
        /// <param name="blockName"> 取得するブロック名。 </param>
        /// <param name="blockInfo"> 取得できたスナップショット。 </param>
        /// <returns> 取得できた場合はtrue。 </returns>
        /// <exception cref="SymphonyNotInitializedException"> Scene Block Loaderが未初期化の場合。 </exception>
        public static bool TryGetBlockInfo(string blockName, out SceneBlockInfo blockInfo)
        {
            EnsureInitialized();
            return _query.TryGetInfo(blockName, out blockInfo);
        }

        /// <summary>
        ///     追跡中の全ブロックの状態スナップショットを取得する。
        /// </summary>
        /// <returns> ブロック名のOrdinal順に並んだスナップショット。 </returns>
        /// <exception cref="SymphonyNotInitializedException"> Scene Block Loaderが未初期化の場合。 </exception>
        public static IReadOnlyList<SceneBlockInfo> GetBlockInfos()
        {
            EnsureInitialized();
            return _query.GetInfos();
        }

        #endregion

        #region 内部処理

        private static SceneBlockRegistry _registry;
        private static SceneBlockService _service;
        private static SceneBlockQuery _query;
        private static SceneBlockViewModel _viewModel;

        /// <summary> Scene Block Loaderが初期化済みかどうか。 </summary>
        internal static bool IsInitialized =>
            _registry != null && _service != null && _query != null && _viewModel != null;

        /// <summary> Compositionが所有する現在のScene Block ViewModel。 </summary>
        internal static SceneBlockViewModel CurrentViewModel => _viewModel;

        /// <summary>
        ///     OrchestratorからScene Block Loaderを初期化する。
        /// </summary>
        /// <param name="sceneLoadService"> シーン操作を実行するScene Load Service。 </param>
        /// <exception cref="ArgumentNullException"> sceneLoadServiceがnullの場合。 </exception>
        internal static void Initialize(SceneLoadService sceneLoadService)
        {
            // Domain Reloadなしの再初期化でも古い保持情報を残さない。
            ResetRuntimeState();

            _registry = new SceneBlockRegistry();
            _service = new SceneBlockService(
                _registry,
                new SceneLoadServiceLoader(sceneLoadService));
            _query = new SceneBlockQuery(_registry);
            _viewModel = new SceneBlockViewModel(_query, _service);
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
        ///     Scene Block Loaderが利用可能な状態か検証する。
        /// </summary>
        /// <exception cref="SymphonyNotInitializedException"> 未初期化の場合。 </exception>
        private static void EnsureInitialized()
        {
            // Compositionの一部でも欠けている場合は、不完全な状態で処理を続行させない。
            if (!IsInitialized) { throw new SymphonyNotInitializedException(typeof(SceneBlockLoader)); }
        }

        /// <summary>
        ///     ロード対象のアセットが処理を開始できる内容か検証する。
        /// </summary>
        /// <param name="block"> 検証するアセット。 </param>
        /// <exception cref="ArgumentNullException"> blockがnullの場合。 </exception>
        /// <exception cref="ArgumentException"> エントリが1件も無い場合。 </exception>
        private static void ValidateBlock(SceneBlockAsset block)
        {
            if (block == null) { throw new ArgumentNullException(nameof(block)); }

            // 空のブロックはロードしても何も起きないため、開始前に呼び出し側へ知らせる。
            if (block.Entries.Count == 0)
            {
                throw new ArgumentException(
                    $"Scene Block {block.BlockName} にエントリがありません。",
                    nameof(block));
            }
        }

        #endregion
    }
}
