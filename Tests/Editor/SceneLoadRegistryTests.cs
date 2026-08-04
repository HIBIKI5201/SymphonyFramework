using System;

using NUnit.Framework;

using SymphonyFrameWork.System.SceneLoad;

namespace SymphonyFrameWork.Tests
{
    /// <summary> SceneLoadRegistryの登録、検索、同期、完了通知を検証する。 </summary>
    public sealed class SceneLoadRegistryTests
    {
        /// <summary> 登録したロード済みEntityをScene名で取得できる。 </summary>
        [Test]
        public void RegisterLoaded_ValidRequest_CanBeFound()
        {
            var registry = new SceneLoadRegistry();

            registry.RegisterLoaded(new SceneLoadRequest("Game", 3));

            Assert.That(registry.TryGet("Game", out SceneLoadEntity entity), Is.True);
            Assert.That(entity.Priority, Is.EqualTo(3));
            Assert.That(entity.State, Is.EqualTo(SceneLoadStateEnum.Complete));
        }

        /// <summary> Unity側一覧との同期時に既存Entityの優先度を維持する。 </summary>
        [Test]
        public void Synchronize_ExistingScene_PreservesPriority()
        {
            var registry = new SceneLoadRegistry();
            registry.RegisterLoaded(new SceneLoadRequest("Game", 7));

            registry.Synchronize(new[] { "Game", "Hud" }, "Game");

            Assert.That(registry.TryGet("Game", out SceneLoadEntity game), Is.True);
            Assert.That(game.Priority, Is.EqualTo(7));
            Assert.That(registry.TryGet("Hud", out SceneLoadEntity hud), Is.True);
            Assert.That(hud.Priority, Is.Zero);
            Assert.That(registry.ActiveSceneName, Is.EqualTo("Game"));
        }

        /// <summary> Complete状態のうち最高優先度のEntityを取得する。 </summary>
        [Test]
        public void TryGetHighestPriorityLoaded_MultipleEntities_ReturnsHighest()
        {
            var registry = new SceneLoadRegistry();
            registry.RegisterLoaded(new SceneLoadRequest("Low", 1));
            registry.RegisterLoaded(new SceneLoadRequest("High", 10));
            registry.StartLoading(new SceneLoadRequest("Loading", 100));

            bool found = registry.TryGetHighestPriorityLoaded(out SceneLoadEntity entity);

            Assert.That(found, Is.True);
            Assert.That(entity.Name, Is.EqualTo("High"));
        }

        /// <summary> ロード前に登録したcallbackを完了時に一度だけ取り出せる。 </summary>
        [Test]
        public void TakeLoadedAction_PendingAction_ReturnsOnlyOnce()
        {
            var registry = new SceneLoadRegistry();
            int invocationCount = 0;
            bool invokeImmediately = registry.RegisterLoadedAction(
                "Game",
                () => invocationCount++);

            registry.RegisterLoaded(new SceneLoadRequest("Game"));
            Action action = registry.TakeLoadedAction("Game");
            action?.Invoke();

            Assert.That(invokeImmediately, Is.False);
            Assert.That(invocationCount, Is.EqualTo(1));
            Assert.That(registry.TakeLoadedAction("Game"), Is.Null);
        }

        /// <summary> ロード済みSceneへのcallback登録は即時実行対象になる。 </summary>
        [Test]
        public void RegisterLoadedAction_CompletedEntity_RequestsImmediateInvocation()
        {
            var registry = new SceneLoadRegistry();
            registry.RegisterLoaded(new SceneLoadRequest("Game"));

            bool invokeImmediately = registry.RegisterLoadedAction(
                "Game",
                () => { });

            Assert.That(invokeImmediately, Is.True);
        }
    }
}
