using NUnit.Framework;

using SymphonyFrameWork.System.SceneLoad;

namespace SymphonyFrameWork.Tests
{
    /// <summary> SceneLoadEntityの状態遷移と値の制約を検証する。 </summary>
    public sealed class SceneLoadEntityTests
    {
        /// <summary> ロード開始時は指定優先度、Loading、進捗0になる。 </summary>
        [Test]
        public void Constructor_LoadingState_StoresInitialValues()
        {
            var entity = new SceneLoadEntity("Game", 4);

            Assert.That(entity.Name, Is.EqualTo("Game"));
            Assert.That(entity.Priority, Is.EqualTo(4));
            Assert.That(entity.State, Is.EqualTo(SceneLoadState.Loading));
            Assert.That(entity.Progress, Is.Zero);
        }

        /// <summary> 進捗を0から1の範囲へ正規化する。 </summary>
        [TestCase(-0.5f, 0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1.5f, 1f)]
        public void ReportProgress_AnyValue_ClampsToUnitRange(
            float input,
            float expected)
        {
            var entity = new SceneLoadEntity("Game");

            entity.ReportProgress(input);

            Assert.That(entity.Progress, Is.EqualTo(expected));
        }

        /// <summary> ロード完了時はCompleteかつ進捗1になる。 </summary>
        [Test]
        public void CompleteLoading_LoadingEntity_TransitionsToComplete()
        {
            var entity = new SceneLoadEntity("Game");
            entity.ReportProgress(0.5f);

            entity.CompleteLoading();

            Assert.That(entity.State, Is.EqualTo(SceneLoadState.Complete));
            Assert.That(entity.Progress, Is.EqualTo(1f));
        }

        /// <summary> アンロード開始時はUnloadingかつ進捗0になる。 </summary>
        [Test]
        public void StartUnloading_CompleteEntity_TransitionsToUnloading()
        {
            var entity = new SceneLoadEntity(
                "Game",
                state: SceneLoadState.Complete);

            entity.StartUnloading();

            Assert.That(entity.State, Is.EqualTo(SceneLoadState.Unloading));
            Assert.That(entity.Progress, Is.Zero);
        }

        /// <summary> 優先度を明示的な操作で更新する。 </summary>
        [Test]
        public void UpdatePriority_DifferentValue_UpdatesPriority()
        {
            var entity = new SceneLoadEntity("Game", 1);

            bool changed = entity.UpdatePriority(5);

            Assert.That(changed, Is.True);
            Assert.That(entity.Priority, Is.EqualTo(5));
        }
    }
}
