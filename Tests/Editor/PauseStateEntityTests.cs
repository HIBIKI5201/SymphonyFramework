using System;

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
        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IGameplayCategory : PauseManager.IPausable { }

        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IUiCategory : PauseManager.IPausable { }

        /// <summary> 生成直後はどのカテゴリーもポーズしていない。 </summary>
        [Test]
        public void InitialState_IsNotPaused()
        {
            PauseStateEntity state = new();

            Assert.That(state.IsPausedAny, Is.False);
            Assert.That(state.IsPaused(typeof(IGameplayCategory)), Is.False);
            Assert.That(state.Categories, Is.Empty);
        }

        /// <summary> 値を変える設定では状態が変わり、変化したと報告する。 </summary>
        [Test]
        public void SetPaused_DifferentValue_ChangesStateAndReportsChange()
        {
            PauseStateEntity state = new();

            bool changed = state.SetPaused(typeof(IGameplayCategory), true);

            Assert.That(changed, Is.True);
            Assert.That(state.IsPaused(typeof(IGameplayCategory)), Is.True);
            Assert.That(state.IsPausedAny, Is.True);
        }

        /// <summary>
        ///     **同じ値の再設定は変化なしとして扱う。**
        ///     IPausable.Pause()の二重呼び出しを防いでいる。
        /// </summary>
        [Test]
        public void SetPaused_SameValue_ReportsNoChange()
        {
            PauseStateEntity state = new();
            state.SetPaused(typeof(IGameplayCategory), true);

            bool changed = state.SetPaused(typeof(IGameplayCategory), true);

            Assert.That(changed, Is.False);
            Assert.That(state.IsPaused(typeof(IGameplayCategory)), Is.True);
        }

        /// <summary> 一度も設定していないカテゴリーの解除は変化なしとして扱う。 </summary>
        [Test]
        public void SetPaused_InitialFalse_ReportsNoChange()
        {
            PauseStateEntity state = new();

            Assert.That(state.SetPaused(typeof(IGameplayCategory), false), Is.False);
        }

        /// <summary>
        ///     カテゴリーごとに独立した状態を持つ。
        /// </summary>
        /// <remarks>
        ///     片方を止めても、もう片方は止まらない。これがこの型の存在理由である。
        /// </remarks>
        [Test]
        public void SetPaused_PerCategory_IsIndependent()
        {
            PauseStateEntity state = new();

            state.SetPaused(typeof(IGameplayCategory), true);

            Assert.That(state.IsPaused(typeof(IGameplayCategory)), Is.True);
            Assert.That(state.IsPaused(typeof(IUiCategory)), Is.False);
        }

        /// <summary> 1つでもポーズ中ならIsPausedAnyが立つ。 </summary>
        [Test]
        public void IsPausedAny_OneCategoryPaused_IsTrue()
        {
            PauseStateEntity state = new();
            state.SetPaused(typeof(IGameplayCategory), true);
            state.SetPaused(typeof(IUiCategory), false);

            Assert.That(state.IsPausedAny, Is.True);
        }

        /// <summary> 全て解除されるとIsPausedAnyが落ちる。 </summary>
        [Test]
        public void IsPausedAny_AllCategoriesResumed_IsFalse()
        {
            PauseStateEntity state = new();
            state.SetPaused(typeof(IGameplayCategory), true);
            state.SetPaused(typeof(IUiCategory), true);

            state.SetPaused(typeof(IGameplayCategory), false);
            state.SetPaused(typeof(IUiCategory), false);

            Assert.That(state.IsPausedAny, Is.False);
        }

        /// <summary>
        ///     解除したカテゴリーもCategoriesに残る。
        /// </summary>
        /// <remarks>
        ///     一度でも操作したカテゴリーは、以後の一括操作の対象になる必要がある。
        /// </remarks>
        [Test]
        public void Categories_ResumedCategory_Remains()
        {
            PauseStateEntity state = new();
            state.SetPaused(typeof(IGameplayCategory), true);
            state.SetPaused(typeof(IGameplayCategory), false);

            Assert.That(state.Categories, Does.Contain(typeof(IGameplayCategory)));
        }

        /// <summary> Resetで全カテゴリーの状態が消える。 </summary>
        [Test]
        public void Reset_ClearsEveryCategory()
        {
            PauseStateEntity state = new();
            state.SetPaused(typeof(IGameplayCategory), true);
            state.SetPaused(typeof(IUiCategory), true);

            state.Reset();

            Assert.That(state.IsPausedAny, Is.False);
            Assert.That(state.Categories, Is.Empty);
        }

        /// <summary> nullのカテゴリーを渡した操作は拒否される。 </summary>
        [Test]
        public void NullCategory_Throws()
        {
            PauseStateEntity state = new();

            Assert.Throws<ArgumentNullException>(() => state.IsPaused(null));
            Assert.Throws<ArgumentNullException>(() => state.SetPaused(null, true));
        }
    }
}
