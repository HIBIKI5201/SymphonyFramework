using NUnit.Framework;

using SymphonyFramework.Scenes;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     Suiteの状態を表すenumの数値を固定する。
    /// </summary>
    /// <remarks>
    ///     シリアライズや外部ツールとの受け渡しで数値が変わらないよう、値を明示的に検証する。
    /// </remarks>
    public sealed class SuiteLoadStateEnumTests
    {
        /// <summary>
        ///     各状態の数値がAttaccaの状態と同じ割り当てである。
        /// </summary>
        [Test]
        public void Values_AreStable()
        {
            Assert.That((int)SuiteLoadStateEnum.None, Is.EqualTo(-1));
            Assert.That((int)SuiteLoadStateEnum.Loading, Is.Zero);
            Assert.That((int)SuiteLoadStateEnum.Complete, Is.EqualTo(1));
            Assert.That((int)SuiteLoadStateEnum.Unloading, Is.EqualTo(2));
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
            SuiteLoadStateEnum state = default;

            Assert.That(state, Is.EqualTo(SuiteLoadStateEnum.Loading));
        }
    }
}
