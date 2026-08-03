using NUnit.Framework;

using SymphonyFrameWork.System;

namespace SymphonyFrameWork.Tests
{
    /// <summary> PauseInfoが取得時点の管理状態を値として表す契約を検証する。 </summary>
    public sealed class PauseInfoTests
    {
        /// <summary> 生成時の値をそのまま公開する。 </summary>
        [Test]
        public void Constructor_ExposesValues()
        {
            var info = new PauseInfo(true, 3);

            Assert.That(info.IsPaused, Is.True);
            Assert.That(info.PausableSubscriberCount, Is.EqualTo(3));
        }

        /// <summary> 同じ値なら等しい。 </summary>
        [Test]
        public void Equals_SameValues_AreEqual()
        {
            var left = new PauseInfo(true, 2);
            var right = new PauseInfo(true, 2);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        /// <summary> ポーズ状態が違えば等しくない。 </summary>
        [Test]
        public void Equals_DifferentPauseState_AreNotEqual()
        {
            Assert.That(new PauseInfo(true, 2) == new PauseInfo(false, 2), Is.False);
        }

        /// <summary> 購読件数が違えば等しくない。 </summary>
        [Test]
        public void Equals_DifferentSubscriberCount_AreNotEqual()
        {
            Assert.That(new PauseInfo(true, 2) == new PauseInfo(true, 3), Is.False);
        }

        /// <summary> 異なる型とは等しくない。 </summary>
        [Test]
        public void Equals_OtherType_IsNotEqual()
        {
            Assert.That(new PauseInfo(true, 1).Equals("pause"), Is.False);
        }
    }
}
