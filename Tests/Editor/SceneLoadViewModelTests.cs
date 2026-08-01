using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFrameWork.System.SceneLoad;

using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.Tests
{
    /// <summary> SceneLoadViewModelの初期値、通知抑制、購読解除を検証する。 </summary>
    public sealed class SceneLoadViewModelTests
    {
        /// <summary> 生成時にQueryの現在値をReactivePropertyへ反映する。 </summary>
        [Test]
        public void Constructor_TrackedScene_ExposesInitialDtos()
        {
            var registry = new SceneLoadRegistry();
            registry.RegisterLoaded(new SceneLoadRequest("Game", 4));
            var service = new SceneLoadService(registry, new FakeSceneLoader());
            using var viewModel = new SceneLoadViewModel(
                new SceneLoadQuery(registry),
                service);

            IReadOnlyList<SceneLoadDto> sceneDtos = viewModel.Scenes.Value;

            Assert.That(sceneDtos, Has.Count.EqualTo(1));
            Assert.That(sceneDtos[0].SceneName, Is.EqualTo("Game"));
            Assert.That(sceneDtos[0].Priority, Is.EqualTo(4));
        }

        /// <summary> Service eventを受けると最新Dto一覧を通知する。 </summary>
        [Test]
        public void ServiceStateChanged_NewScene_NotifiesLatestDtos()
        {
            var registry = new SceneLoadRegistry();
            var loader = new FakeSceneLoader();
            var service = new SceneLoadService(registry, loader);
            using var viewModel = new SceneLoadViewModel(
                new SceneLoadQuery(registry),
                service);
            IReadOnlyList<SceneLoadDto> notified = null;
            using IDisposable subscription = viewModel.Scenes.Subscribe(
                sceneDtos => notified = sceneDtos,
                notifyCurrent: false);
            loader.AddLoadedScene("Game");

            service.TryRegisterLoadedScene(new SceneLoadRequest("Game", 6));

            Assert.That(notified, Is.Not.Null);
            Assert.That(notified, Has.Count.EqualTo(1));
            Assert.That(notified[0].SceneName, Is.EqualTo("Game"));
            Assert.That(notified[0].Priority, Is.EqualTo(6));
        }

        /// <summary> Serviceが同じ状態を通知してもDto内容が同じなら再通知しない。 </summary>
        [Test]
        public void ServiceStateChanged_SameDtoContent_DoesNotNotifyAgain()
        {
            var registry = new SceneLoadRegistry();
            var loader = new FakeSceneLoader();
            var service = new SceneLoadService(registry, loader);
            using var viewModel = new SceneLoadViewModel(
                new SceneLoadQuery(registry),
                service);
            int notificationCount = 0;
            using IDisposable subscription = viewModel.Scenes.Subscribe(
                _ => notificationCount++,
                notifyCurrent: false);
            loader.AddLoadedScene("Game");
            var request = new SceneLoadRequest("Game", 2);

            service.TryRegisterLoadedScene(request);
            service.TryRegisterLoadedScene(request);

            Assert.That(notificationCount, Is.EqualTo(1));
        }

        /// <summary> Dispose後はService eventから切り離される。 </summary>
        [Test]
        public void Dispose_ServiceStateChanges_DoesNotNotifySubscribers()
        {
            var registry = new SceneLoadRegistry();
            var loader = new FakeSceneLoader();
            var service = new SceneLoadService(registry, loader);
            var viewModel = new SceneLoadViewModel(
                new SceneLoadQuery(registry),
                service);
            int notificationCount = 0;
            viewModel.Scenes.Subscribe(
                _ => notificationCount++,
                notifyCurrent: false);
            viewModel.Dispose();
            loader.AddLoadedScene("Game");

            service.TryRegisterLoadedScene(new SceneLoadRequest("Game"));

            Assert.That(notificationCount, Is.Zero);
            Assert.DoesNotThrow(viewModel.Dispose);
        }

        /// <summary> テスト用にロード済みSceneだけをメモリ上で管理する。 </summary>
        private sealed class FakeSceneLoader : ISceneLoader
        {
            /// <inheritdoc />
            public string ActiveSceneName { get; private set; }

            private readonly HashSet<string> _loadedSceneNames =
                new(StringComparer.Ordinal);

            /// <summary> 指定Sceneをロード済み一覧へ追加する。 </summary>
            /// <param name="sceneName"> 追加するScene名。 </param>
            internal void AddLoadedScene(string sceneName)
            {
                _loadedSceneNames.Add(sceneName);
            }

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
            public ValueTask<bool> LoadSceneAsync(
                string sceneName,
                IProgress<float> progress,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                _loadedSceneNames.Add(sceneName);
                progress?.Report(1f);
                return new ValueTask<bool>(true);
            }

            /// <inheritdoc />
            public ValueTask<bool> UnloadSceneAsync(
                string sceneName,
                IProgress<float> progress,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                bool removed = _loadedSceneNames.Remove(sceneName);
                progress?.Report(1f);
                return new ValueTask<bool>(removed);
            }

            /// <inheritdoc />
            public ValueTask InitializeRootObjectsAsync(string sceneName)
            {
                return default;
            }
        }
    }
}
