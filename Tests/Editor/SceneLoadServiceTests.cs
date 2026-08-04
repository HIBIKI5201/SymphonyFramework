using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFrameWork.System.SceneLoad;

using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.Tests
{
    /// <summary> fake Scene Loaderを用いてSceneLoadServiceの優先度判断と進捗伝播を検証する。 </summary>
    public sealed class SceneLoadServiceTests
    {
        /// <summary> 高い優先度のSceneをロードするとActive Sceneが切り替わる。 </summary>
        [Test]
        public async Task LoadScene_HigherPriority_SetsActiveScene()
        {
            var registry = new SceneLoadRegistry();
            var loader = new FakeSceneLoader();
            var service = new SceneLoadService(registry, loader);

            await service.LoadScene(new SceneLoadRequest("Low", 1));
            await service.LoadScene(new SceneLoadRequest("High", 5));

            Assert.That(loader.ActiveSceneName, Is.EqualTo("High"));
            Assert.That(registry.ActiveSceneName, Is.EqualTo("High"));
        }

        /// <summary> 低い優先度のSceneをロードしても現在のActive Sceneを維持する。 </summary>
        [Test]
        public async Task LoadScene_LowerPriority_KeepsActiveScene()
        {
            var registry = new SceneLoadRegistry();
            var loader = new FakeSceneLoader();
            var service = new SceneLoadService(registry, loader);

            await service.LoadScene(new SceneLoadRequest("High", 5));
            await service.LoadScene(new SceneLoadRequest("Low", 1));

            Assert.That(loader.ActiveSceneName, Is.EqualTo("High"));
            Assert.That(registry.ActiveSceneName, Is.EqualTo("High"));
        }

        /// <summary> Active Sceneをアンロードすると残存する最高優先度Sceneを選ぶ。 </summary>
        [Test]
        public async Task UnloadScene_ActiveScene_SelectsHighestRemainingScene()
        {
            var registry = new SceneLoadRegistry();
            var loader = new FakeSceneLoader();
            var service = new SceneLoadService(registry, loader);

            await service.LoadScene(new SceneLoadRequest("Low", 1));
            await service.LoadScene(new SceneLoadRequest("Middle", 3));
            await service.LoadScene(new SceneLoadRequest("High", 5));

            bool succeeded = await service.UnloadScene("High");

            Assert.That(succeeded, Is.True);
            Assert.That(loader.ActiveSceneName, Is.EqualTo("Middle"));
            Assert.That(registry.ActiveSceneName, Is.EqualTo("Middle"));
        }

        /// <summary> Loaderの進捗をEntityと外部observerの両方へ伝播する。 </summary>
        [Test]
        public async Task LoadScene_ReportsProgress_UpdatesEntityAndObserver()
        {
            var registry = new SceneLoadRegistry();
            var loader = new FakeSceneLoader();
            var service = new SceneLoadService(registry, loader);
            var reportedValues = new List<float>();
            var progress = new ImmediateProgress(reportedValues.Add);

            await service.LoadScene(
                new SceneLoadRequest("Game", 1),
                progress);

            Assert.That(reportedValues, Is.EqualTo(new[] { 0.5f, 1f }));
            Assert.That(registry.TryGet("Game", out SceneLoadEntity entity), Is.True);
            Assert.That(entity.Progress, Is.EqualTo(1f));
        }

        /// <summary> テスト用にUnity Scene操作をメモリ上で再現する。 </summary>
        private sealed class FakeSceneLoader : ISceneLoader
        {
            /// <inheritdoc />
            public string ActiveSceneName { get; private set; }

            private readonly HashSet<string> _loadedSceneNames =
                new(StringComparer.Ordinal);

            /// <inheritdoc />
            public IReadOnlyList<string> GetLoadedSceneNames()
            {
                return new List<string>(_loadedSceneNames);
            }

            /// <inheritdoc />
            public bool TryGetLoadedScene(string sceneName, out Scene scene)
            {
                scene = default;
                return _loadedSceneNames.Contains(sceneName);
            }

            /// <inheritdoc />
            public bool TrySetActiveScene(string sceneName)
            {
                if (!_loadedSceneNames.Contains(sceneName))
                {
                    return false;
                }

                ActiveSceneName = sceneName;
                return true;
            }

            /// <inheritdoc />
            public Task<bool> LoadSceneAsync(
                string sceneName,
                IProgress<float> progress,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report(0.5f);
                _loadedSceneNames.Add(sceneName);
                progress?.Report(1f);
                return Task.FromResult(true);
            }

            /// <inheritdoc />
            public Task<bool> UnloadSceneAsync(
                string sceneName,
                IProgress<float> progress,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report(0.5f);
                bool removed = _loadedSceneNames.Remove(sceneName);
                progress?.Report(1f);

                if (string.Equals(
                    ActiveSceneName,
                    sceneName,
                    StringComparison.Ordinal))
                {
                    ActiveSceneName = null;
                }

                return Task.FromResult(removed);
            }

            /// <inheritdoc />
            public Task InitializeRootObjectsAsync(string sceneName)
            {
                return Task.CompletedTask;
            }
        }

        /// <summary> observerを同じ呼び出しスタックで実行するテスト用進捗通知。 </summary>
        private sealed class ImmediateProgress : IProgress<float>
        {
            /// <summary> observerを指定して生成する。 </summary>
            /// <param name="observer"> 進捗observer。 </param>
            internal ImmediateProgress(Action<float> observer)
            {
                _observer = observer;
            }

            private readonly Action<float> _observer;

            /// <inheritdoc />
            public void Report(float value)
            {
                _observer.Invoke(value);
            }
        }
    }
}
