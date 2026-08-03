using NUnit.Framework;

using SymphonyFrameWork.System;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     PauseStateEntityの契約を検証する。
    ///     Unity APIに依存しないため、EditModeで直接検証できる。
    /// </summary>
    public sealed class PauseStateEntityTests
    {
        /// <summary> 生成直後は非ポーズである。 </summary>
        [Test]
        public void InitialState_IsNotPaused()
        {
            var state = new PauseStateEntity();

            Assert.That(state.IsPaused, Is.False);
        }

        /// <summary> 異なる値の設定は状態を変え、変化したことを返す。 </summary>
        [Test]
        public void SetPaused_DifferentValue_ChangesStateAndReportsChange()
        {
            var state = new PauseStateEntity();

            bool changed = state.SetPaused(true);

            Assert.That(changed, Is.True);
            Assert.That(state.IsPaused, Is.True);
        }

        /// <summary>
        ///     **同じ値の再設定では変化しない。**
        ///     ここがfalseを返すことで、OnPauseChangedの重複発行と
        ///     IPausable.Pause()の二重呼び出しを防いでいる。
        /// </summary>
        [Test]
        public void SetPaused_SameValue_ReportsNoChange()
        {
            var state = new PauseStateEntity();
            state.SetPaused(true);

            bool changed = state.SetPaused(true);

            Assert.That(changed, Is.False, "同じ値の再設定で通知が重複してはいけない。");
            Assert.That(state.IsPaused, Is.True);
        }

        /// <summary> 初期状態での非ポーズ設定も変化として扱わない。 </summary>
        [Test]
        public void SetPaused_InitialFalse_ReportsNoChange()
        {
            var state = new PauseStateEntity();

            Assert.That(state.SetPaused(false), Is.False);
        }

        /// <summary> Resetで非ポーズへ戻る。 </summary>
        [Test]
        public void Reset_ReturnsToNotPaused()
        {
            var state = new PauseStateEntity();
            state.SetPaused(true);

            state.Reset();

            Assert.That(state.IsPaused, Is.False);
        }
    }
}
