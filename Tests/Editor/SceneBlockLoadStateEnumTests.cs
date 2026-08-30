using NUnit.Framework;

using SymphonyFrameWork.System.SceneBlock;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     Scene Blockの状態を表すenumの数値を固定する。
    /// </summary>
    /// <remarks>
    ///     シリアライズや外部ツールとの受け渡しで数値が変わらないよう、値を明示的に検証する。
    /// </remarks>
    public sealed class SceneBlockLoadStateEnumTests
    {
        /// <summary>
        ///     各状態の数値がScene Loaderの状態と同じ割り当てである。
        /// </summary>
        [Test]
        public void Values_AreStable()
        {
            Assert.That((int)SceneBlockLoadStateEnum.None, Is.EqualTo(-1));
            Assert.That((int)SceneBlockLoadStateEnum.Loading, Is.Zero);
            Assert.That((int)SceneBlockLoadStateEnum.Complete, Is.EqualTo(1));
            Assert.That((int)SceneBlockLoadStateEnum.Unloading, Is.EqualTo(2));
        }

        /// <summary>
        ///     追跡していない状態の既定値はNoneではなくLoadingになる点を明示する。
        /// </summary>
        /// <remarks>
        ///     0がLoadingであるため、既定値のまま状態として扱わないことを固定する。
        /// </remarks>
        [Test]
        public void Default_IsLoading()
        {
            SceneBlockLoadStateEnum state = default;

            Assert.That(state, Is.EqualTo(SceneBlockLoadStateEnum.Loading));
        }
    }
}
