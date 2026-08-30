using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFrameWork.System.SceneBlock;
using SymphonyFrameWork.System.SceneLoad;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     Scene Blockの状態変更が表示用ReactivePropertyへ反映される契約を検証する。
    /// </summary>
    public sealed class SceneBlockViewModelTests
    {
        /// <summary>
        ///     生成時点のスナップショットを初期値として公開する。
        /// </summary>
        [Test]
        public void Constructor_PublishesInitialSnapshot()
        {
            Assert.That(_viewModel.Blocks.Value, Is.Empty);
        }

        /// <summary>
        ///     Serviceの状態変更でブロック一覧を更新する。
        /// </summary>
        [Test]
        public void StateChanged_UpdatesBlocks()
        {
            LoadBlock("Town");

            IReadOnlyList<SceneBlockDto> blocks = _viewModel.Blocks.Value;

            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.That(blocks[0].BlockName, Is.EqualTo("Town"));
            Assert.That(blocks[0].State, Is.EqualTo(SceneBlockLoadStateEnum.Complete));
            Assert.That(blocks[0].HeldSceneNames, Is.EqualTo(new[] { "Base" }));
        }

        /// <summary>
        ///     購読者は状態変更のたびに最新の一覧を受け取る。
        /// </summary>
        [Test]
        public void Subscribe_ReceivesUpdatesUntilDisposed()
        {
            List<IReadOnlyList<SceneBlockDto>> received = new();
            IDisposable subscription = _viewModel.Blocks.Subscribe(received.Add);

            LoadBlock("Town");
            subscription.Dispose();
            LoadBlock("Dungeon");

            // 購読開始時の現在値と、Town のロード中に届いた更新までを受け取る。
            Assert.That(received, Is.Not.Empty);
            Assert.That(received[received.Count - 1], Has.Count.EqualTo(1));
        }

        /// <summary>
        ///     状態が変わらない再ロードでは購読者へ通知しない。
        /// </summary>
        [Test]
        public void StateChanged_AlreadyTrackedBlock_DoesNotNotifyAgain()
        {
            LoadBlock("Town");

            int notificationCount = 0;
            using IDisposable subscription = _viewModel.Blocks.Subscribe(
                _ => notificationCount++,
                notifyCurrent: false);

            // 同じアセットの再ロードは状態を変えないため、内容比較で通知が止まる。
            LoadBlock("Town");

            Assert.That(notificationCount, Is.Zero);
        }

        /// <summary>
        ///     破棄後は状態変更を反映しない。
        /// </summary>
        [Test]
        public void Dispose_StopsReflectingStateChanges()
        {
            _viewModel.Dispose();

            Assert.That(() => LoadBlock("Town"), Throws.Nothing);
        }

        /// <summary>
        ///     破棄を繰り返しても例外にならない。
        /// </summary>
        [Test]
        public void Dispose_CalledTwice_IsHarmless()
        {
            _viewModel.Dispose();

            Assert.That(() => _viewModel.Dispose(), Throws.Nothing);
        }

        /// <summary> 検証対象のViewModel。 </summary>
        private SceneBlockViewModel _viewModel;

        /// <summary> 状態変更を起こすService。 </summary>
        private SceneBlockService _service;

        /// <summary>
        ///     テストごとに追跡状態とViewModelを作り直す。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            SceneBlockRegistry registry = new();
            _service = new SceneBlockService(registry, new StubBlockSceneLoader());
            _viewModel = new SceneBlockViewModel(new SceneBlockQuery(registry), _service);
        }

        /// <summary>
        ///     テストごとにViewModelを破棄する。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _viewModel?.Dispose();
            _viewModel = null;
        }

        /// <summary>
        ///     1シーンだけのブロックを同期的にロードする。
        /// </summary>
        /// <param name="blockName"> ブロック名。 </param>
        private void LoadBlock(string blockName)
        {
            SceneBlockEntry[] entries = { new("Base") };

            _service
                .LoadBlock(blockName, blockName.GetHashCode(), entries, null, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary> 常に成功する検証用のシーン操作。 </summary>
        private sealed class StubBlockSceneLoader : IBlockSceneLoader
        {
            /// <summary>
            ///     ロード済みとみなすかを返す。
            /// </summary>
            /// <param name="sceneName"> 確認するシーン名。 </param>
            /// <returns> ロード済みの場合はtrue。 </returns>
            public bool IsSceneLoaded(string sceneName) => _loadedScenes.Contains(sceneName);

            /// <summary>
            ///     ロードを成功させる。
            /// </summary>
            /// <param name="requests"> ロード要求。 </param>
            /// <param name="progress"> 進捗の通知先。 </param>
            /// <param name="token"> 中断用トークン。 </param>
            /// <returns> 常に成功。 </returns>
            public Task<bool> LoadScenesAsync(
                IReadOnlyList<SceneLoadRequest> requests,
                IProgress<float> progress,
                CancellationToken token)
            {
                foreach (SceneLoadRequest request in requests) { _loadedScenes.Add(request.SceneName); }

                return Task.FromResult(true);
            }

            /// <summary>
            ///     アンロードを成功させる。
            /// </summary>
            /// <param name="sceneNames"> アンロード対象。 </param>
            /// <param name="progress"> 進捗の通知先。 </param>
            /// <param name="token"> 中断用トークン。 </param>
            /// <returns> 常に成功。 </returns>
            public Task<bool> UnloadScenesAsync(
                IReadOnlyList<string> sceneNames,
                IProgress<float> progress,
                CancellationToken token)
            {
                foreach (string sceneName in sceneNames) { _loadedScenes.Remove(sceneName); }

                return Task.FromResult(true);
            }

            private readonly HashSet<string> _loadedScenes = new(StringComparer.Ordinal);
        }
    }
}
