using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.System;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     PauseCategoryResolverの契約を検証する。
    ///     Unity APIに依存しないため、EditModeで直接検証できる。
    /// </summary>
    public sealed class PauseCategoryResolverTests
    {
        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IGameplayCategory : PauseManager.IPausable { }

        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IUiCategory : PauseManager.IPausable { }

        /// <summary> 検証用の派生カテゴリー。 </summary>
        private interface IHudCategory : IUiCategory { }

        /// <summary> カテゴリーを実装しない検証用の対象。 </summary>
        private sealed class UncategorizedPausable : PauseManager.IPausable
        {
            public void Pause() { }
            public void Resume() { }
        }

        /// <summary> 1つのカテゴリーを実装する検証用の対象。 </summary>
        private sealed class GameplayPausable : IGameplayCategory
        {
            public void Pause() { }
            public void Resume() { }
        }

        /// <summary> 2つのカテゴリーを実装する検証用の対象。 </summary>
        private sealed class GameplayAndUiPausable : IGameplayCategory, IUiCategory
        {
            public void Pause() { }
            public void Resume() { }
        }

        /// <summary> 派生カテゴリーを実装する検証用の対象。 </summary>
        private sealed class HudPausable : IHudCategory
        {
            public void Pause() { }
            public void Resume() { }
        }

        /// <summary> 実装したカテゴリーを解決できる。 </summary>
        [Test]
        public void Resolve_SingleCategory_ReturnsIt()
        {
            IReadOnlyList<Type> categories = PauseCategoryResolver.Resolve(typeof(GameplayPausable));

            Assert.That(categories, Is.EquivalentTo(new[] { typeof(IGameplayCategory) }));
        }

        /// <summary> 複数のカテゴリーを実装した対象は、そのすべてに属する。 </summary>
        [Test]
        public void Resolve_MultipleCategories_ReturnsAll()
        {
            IReadOnlyList<Type> categories =
                PauseCategoryResolver.Resolve(typeof(GameplayAndUiPausable));

            Assert.That(
                categories,
                Is.EquivalentTo(new[] { typeof(IGameplayCategory), typeof(IUiCategory) }));
        }

        /// <summary>
        ///     派生カテゴリーを実装した対象は基底カテゴリーにも属する。
        /// </summary>
        /// <remarks>
        ///     基底を落とすと、基底カテゴリーを止めても対象が動き続ける。
        /// </remarks>
        [Test]
        public void Resolve_DerivedCategory_IncludesBaseCategory()
        {
            IReadOnlyList<Type> categories = PauseCategoryResolver.Resolve(typeof(HudPausable));

            Assert.That(
                categories,
                Is.EquivalentTo(new[] { typeof(IHudCategory), typeof(IUiCategory) }));
        }

        /// <summary>
        ///     カテゴリーを1つも実装していない対象は既定カテゴリーに属する。
        /// </summary>
        /// <remarks>
        ///     空を返すと、どのカテゴリーを止めても動き続ける対象が黙って生まれる。
        /// </remarks>
        [Test]
        public void Resolve_NoCategory_ReturnsDefaultCategory()
        {
            IReadOnlyList<Type> categories =
                PauseCategoryResolver.Resolve(typeof(UncategorizedPausable));

            Assert.That(
                categories,
                Is.EquivalentTo(new[] { PauseCategoryResolver.DefaultCategory }));
        }

        /// <summary> 解決結果にIPausable自身は含まれない。 </summary>
        [Test]
        public void Resolve_CategorizedTarget_DoesNotIncludeIPausableItself()
        {
            IReadOnlyList<Type> categories = PauseCategoryResolver.Resolve(typeof(GameplayPausable));

            Assert.That(categories, Does.Not.Contain(typeof(PauseManager.IPausable)));
        }

        /// <summary> nullの解決は拒否される。 </summary>
        [Test]
        public void Resolve_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => PauseCategoryResolver.Resolve(null));
        }

        /// <summary> IPausableを継承したinterfaceはカテゴリーになる。 </summary>
        [Test]
        public void IsCategory_DerivedInterface_IsTrue()
        {
            Assert.That(PauseCategoryResolver.IsCategory(typeof(IGameplayCategory)), Is.True);
            Assert.That(PauseCategoryResolver.IsCategory(typeof(IHudCategory)), Is.True);
        }

        /// <summary>
        ///     IPausable自身、無関係なinterface、classはカテゴリーにならない。
        /// </summary>
        [Test]
        public void IsCategory_NotACategory_IsFalse()
        {
            Assert.That(PauseCategoryResolver.IsCategory(typeof(PauseManager.IPausable)), Is.False);
            Assert.That(PauseCategoryResolver.IsCategory(typeof(IDisposable)), Is.False);
            Assert.That(PauseCategoryResolver.IsCategory(typeof(GameplayPausable)), Is.False);
            Assert.That(PauseCategoryResolver.IsCategory(null), Is.False);
        }

        /// <summary>
        ///     操作対象の判定は既定カテゴリーも含む。
        /// </summary>
        /// <remarks>
        ///     <c>IsCategory</c> と違い、IPausable自身を操作対象として認める。
        ///     「カテゴリーを明示していない対象」を指す操作として使えるため。
        /// </remarks>
        [Test]
        public void IsOperableCategory_IncludesDefaultCategory()
        {
            Assert.That(
                PauseCategoryResolver.IsOperableCategory(typeof(PauseManager.IPausable)),
                Is.True);
            Assert.That(PauseCategoryResolver.IsOperableCategory(typeof(IGameplayCategory)), Is.True);
        }

        /// <summary> 具象型と無関係なinterfaceは操作対象にならない。 </summary>
        [Test]
        public void IsOperableCategory_NotAnInterfaceOrUnrelated_IsFalse()
        {
            Assert.That(PauseCategoryResolver.IsOperableCategory(typeof(GameplayPausable)), Is.False);
            Assert.That(PauseCategoryResolver.IsOperableCategory(typeof(IDisposable)), Is.False);
            Assert.That(PauseCategoryResolver.IsOperableCategory(null), Is.False);
        }

        /// <summary> 操作対象にできる型の検証は通る。 </summary>
        [Test]
        public void ValidateOperableCategory_Category_DoesNotThrow()
        {
            Assert.DoesNotThrow(
                () => PauseCategoryResolver.ValidateOperableCategory(
                    typeof(IGameplayCategory), "TCategory"));
            Assert.DoesNotThrow(
                () => PauseCategoryResolver.ValidateOperableCategory(
                    typeof(PauseManager.IPausable), "TCategory"));
        }

        /// <summary>
        ///     具象型の検証はArgumentExceptionで拒否する。
        /// </summary>
        /// <remarks>
        ///     型制約 <c>where TCategory : IPausable</c> は具象クラスも通すため、実行時に弾く。
        /// </remarks>
        [Test]
        public void ValidateOperableCategory_ConcreteType_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => PauseCategoryResolver.ValidateOperableCategory(
                    typeof(GameplayPausable), "TCategory"));
        }

        /// <summary> nullの検証はArgumentNullExceptionで拒否する。 </summary>
        [Test]
        public void ValidateOperableCategory_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => PauseCategoryResolver.ValidateOperableCategory(null, "TCategory"));
        }

        /// <summary> 既定カテゴリーはIPausable自身である。 </summary>
        [Test]
        public void DefaultCategory_IsIPausable()
        {
            Assert.That(
                PauseCategoryResolver.DefaultCategory,
                Is.EqualTo(typeof(PauseManager.IPausable)));
        }
    }
}
