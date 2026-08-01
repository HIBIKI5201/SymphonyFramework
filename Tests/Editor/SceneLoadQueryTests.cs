using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.System.SceneLoad;

namespace SymphonyFrameWork.Tests
{
    /// <summary> SceneLoadQueryのInfo／Dto変換とスナップショット性を検証する。 </summary>
    public sealed class SceneLoadQueryTests
    {
        /// <summary> null、空、未登録のScene名は未検出として返す。 </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("Missing")]
        public void TryGetInfo_UntrackedScene_ReturnsFalse(string sceneName)
        {
            var query = new SceneLoadQuery(new SceneLoadRegistry());

            bool found = query.TryGetInfo(sceneName, out SceneLoadInfo sceneInfo);

            Assert.That(found, Is.False);
            Assert.That(sceneInfo, Is.EqualTo(default(SceneLoadInfo)));
        }

        /// <summary> Entityの全観測値とActive判定をInfoへ変換する。 </summary>
        [Test]
        public void TryGetInfo_TrackedActiveScene_ReturnsSnapshot()
        {
            var registry = new SceneLoadRegistry();
            registry.RegisterLoaded(new SceneLoadRequest("Game", 7));
            registry.SetActiveScene("Game");
            var query = new SceneLoadQuery(registry);

            bool found = query.TryGetInfo("Game", out SceneLoadInfo sceneInfo);

            Assert.That(found, Is.True);
            Assert.That(sceneInfo.SceneName, Is.EqualTo("Game"));
            Assert.That(sceneInfo.State, Is.EqualTo(SceneLoadState.Complete));
            Assert.That(sceneInfo.Priority, Is.EqualTo(7));
            Assert.That(sceneInfo.Progress, Is.EqualTo(1f));
            Assert.That(sceneInfo.IsActive, Is.True);
        }

        /// <summary> 一覧をScene名のordinal昇順で返す。 </summary>
        [Test]
        public void GetInfos_MultipleScenes_ReturnsOrdinalOrder()
        {
            var registry = new SceneLoadRegistry();
            registry.RegisterLoaded(new SceneLoadRequest("Zebra"));
            registry.RegisterLoaded(new SceneLoadRequest("Alpha"));
            registry.RegisterLoaded(new SceneLoadRequest("middle"));
            var query = new SceneLoadQuery(registry);

            IReadOnlyList<SceneLoadInfo> sceneInfos = query.GetInfos();

            Assert.That(
                new[]
                {
                    sceneInfos[0].SceneName,
                    sceneInfos[1].SceneName,
                    sceneInfos[2].SceneName
                },
                Is.EqualTo(new[] { "Alpha", "Zebra", "middle" }));
        }

        /// <summary> 取得済みInfoは後続のEntity更新から独立している。 </summary>
        [Test]
        public void GetInfos_EntityChangesAfterRead_PreservesPreviousSnapshot()
        {
            var registry = new SceneLoadRegistry();
            registry.RegisterLoaded(new SceneLoadRequest("Game", 2));
            var query = new SceneLoadQuery(registry);
            IReadOnlyList<SceneLoadInfo> previous = query.GetInfos();

            SceneLoadEntity entity = registry.StartLoading(
                new SceneLoadRequest("Game", 10));
            entity.ReportProgress(0.5f);
            IReadOnlyList<SceneLoadInfo> current = query.GetInfos();

            Assert.That(previous[0].State, Is.EqualTo(SceneLoadState.Complete));
            Assert.That(previous[0].Priority, Is.EqualTo(2));
            Assert.That(previous[0].Progress, Is.EqualTo(1f));
            Assert.That(current[0].State, Is.EqualTo(SceneLoadState.Loading));
            Assert.That(current[0].Priority, Is.EqualTo(10));
            Assert.That(current[0].Progress, Is.EqualTo(0.5f));
        }

        /// <summary> ViewModelに必要な値だけをDtoへ変換する。 </summary>
        [Test]
        public void GetDtos_TrackedActiveScene_ReturnsDisplayValues()
        {
            var registry = new SceneLoadRegistry();
            registry.RegisterLoaded(new SceneLoadRequest("Game", 3));
            registry.SetActiveScene("Game");
            var query = new SceneLoadQuery(registry);

            IReadOnlyList<SceneLoadDto> sceneDtos = query.GetDtos();

            Assert.That(sceneDtos, Has.Count.EqualTo(1));
            Assert.That(sceneDtos[0].SceneName, Is.EqualTo("Game"));
            Assert.That(sceneDtos[0].State, Is.EqualTo(SceneLoadState.Complete));
            Assert.That(sceneDtos[0].Priority, Is.EqualTo(3));
            Assert.That(sceneDtos[0].Progress, Is.EqualTo(1f));
            Assert.That(sceneDtos[0].IsActive, Is.True);
        }
    }
}
