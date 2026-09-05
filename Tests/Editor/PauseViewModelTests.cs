using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SymphonyFrameWork.System;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     PauseViewModelがServiceの状態変更を表示値へ反映する契約を検証する。
    ///     Unityのライフサイクルに依存しないため、EditModeで直接検証できる。
    /// </summary>
    public sealed class PauseViewModelTests
    {
        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IGameplayCategory : PauseManager.IPausable { }

        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IUiCategory : PauseManager.IPausable { }

        /// <summary> 検証用のポーズ対象。 </summary>
        private sealed class TestPausable : PauseManager.IPausable
        {
            public void Pause() { }
            public void Resume() { }
        }

        /// <summary> ゲームプレイカテゴリーの検証用ポーズ対象。 </summary>
        private sealed class GameplayPausable : IGameplayCategory
        {
            public void Pause() { }
            public void Resume() { }
        }

        /// <summary> UIカテゴリーの検証用ポーズ対象。 </summary>
        private sealed class UiPausable : IUiCategory
        {
            public void Pause() { }
            public void Resume() { }
        }

        private PauseService _service;
        private PauseQuery _query;
        private PauseViewModel _viewModel;

        /// <summary> 各テストごとに独立したViewModelを構築する。 </summary>
        [SetUp]
        public void SetUp()
        {
            var state = new PauseStateEntity();
            var registry = new PausableRegistry();
            _service = new PauseService(state, registry);
            _query = new PauseQuery(state, registry);
            _viewModel = new PauseViewModel(_query, _service);
        }

        /// <summary> ViewModelを破棄して次のテストへ状態を持ち越さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            _viewModel?.Dispose();
            _viewModel = null;
        }

        /// <summary> 引数を省略した生成は拒否される。 </summary>
        [Test]
        public void Constructor_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new PauseViewModel(null, _service));
            Assert.Throws<ArgumentNullException>(() => new PauseViewModel(_query, null));
        }

        /// <summary> 生成直後はQueryの現在値をそのまま公開する。 </summary>
        [Test]
        public void InitialValue_MatchesQuery()
        {
            Assert.That(_viewModel.State.Value, Is.EqualTo(_query.GetDto()));
            Assert.That(_viewModel.State.Value.IsPaused, Is.False);
            Assert.That(_viewModel.State.Value.PausableSubscriberCount, Is.Zero);
        }

        /// <summary> ポーズ状態の変更が表示値へ反映される。 </summary>
        [Test]
        public void SetPausedAll_UpdatesState()
        {
            _service.SetPausedAll(true);

            Assert.That(_viewModel.State.Value.IsPaused, Is.True);
        }

        /// <summary> 購読件数の変更が表示値へ反映される。 </summary>
        [Test]
        public void Register_UpdatesSubscriberCount()
        {
            var pausable = new TestPausable();

            _service.Register(pausable);
            Assert.That(_viewModel.State.Value.PausableSubscriberCount, Is.EqualTo(1));

            _service.Unregister(pausable);
            Assert.That(_viewModel.State.Value.PausableSubscriberCount, Is.Zero);
        }

        /// <summary> 内容が変わらない場合は通知しない。 </summary>
        [Test]
        public void SameValue_DoesNotNotify()
        {
            _service.SetPausedAll(true);

            int notifiedCount = 0;
            using IDisposable subscription =
                _viewModel.State.Subscribe(_ => notifiedCount++, notifyCurrent: false);

            _service.SetPausedAll(true);

            Assert.That(notifiedCount, Is.Zero);
        }

        /// <summary>
        ///     カテゴリーごとの表示値が、登録済みの全カテゴリー分そろう。
        /// </summary>
        /// <remarks>
        ///     状態側だけを見ると、まだ誰も止めていないカテゴリーを取りこぼす。
        /// </remarks>
        [Test]
        public void Categories_IncludeEveryRegisteredCategory()
        {
            _service.Register(new GameplayPausable());
            _service.Register(new UiPausable());

            IReadOnlyList<PauseCategoryDto> categories = _viewModel.State.Value.Categories;

            Assert.That(
                categories.Select(category => category.CategoryName),
                Is.EquivalentTo(new[] { nameof(IGameplayCategory), nameof(IUiCategory) }));
        }

        /// <summary>
        ///     カテゴリーの表示値は表示名の昇順で並ぶ。
        /// </summary>
        /// <remarks>
        ///     **辞書の列挙順は保証されない。** 並びが揺れると、内容が同じでも
        ///     ViewModelが変化として通知してしまう。
        /// </remarks>
        [Test]
        public void Categories_AreSortedByName()
        {
            _service.Register(new UiPausable());
            _service.Register(new GameplayPausable());
            _service.Register(new TestPausable());

            IReadOnlyList<PauseCategoryDto> categories = _viewModel.State.Value.Categories;
            List<string> names = categories.Select(category => category.CategoryName).ToList();

            Assert.That(names, Is.Ordered.Using((IComparer<string>)StringComparer.Ordinal));
        }

        /// <summary> カテゴリーのポーズ状態と件数が表示値へ反映される。 </summary>
        [Test]
        public void Categories_ReflectStateAndCount()
        {
            _service.Register(new GameplayPausable());
            _service.SetPaused(typeof(IGameplayCategory), true);

            PauseCategoryDto category = _viewModel.State.Value.Categories
                .First(item => item.CategoryName == nameof(IGameplayCategory));

            Assert.That(category.IsPaused, Is.True);
            Assert.That(category.PausableCount, Is.EqualTo(1));
        }

        /// <summary>
        ///     カテゴリーの内容が同じなら通知しない。
        /// </summary>
        /// <remarks>
        ///     **一覧を参照で比べると、毎回新しいListを作るQueryでは常に変化扱いになる。**
        ///     ここが壊れると、表示が毎フレーム更新される。
        /// </remarks>
        [Test]
        public void Categories_SameContent_DoesNotNotify()
        {
            _service.Register(new GameplayPausable());
            _service.SetPaused(typeof(IGameplayCategory), true);

            int notifiedCount = 0;
            using IDisposable subscription =
                _viewModel.State.Subscribe(_ => notifiedCount++, notifyCurrent: false);

            _service.SetPaused(typeof(IGameplayCategory), true);

            Assert.That(notifiedCount, Is.Zero);
        }

        /// <summary> 破棄後はServiceの変更を反映せず、多重破棄も無害である。 </summary>
        [Test]
        public void Dispose_StopsUpdatesAndIsIdempotent()
        {
            _viewModel.Dispose();
            _viewModel.Dispose();

            _service.SetPausedAll(true);

            Assert.That(_viewModel.State.Value.IsPaused, Is.False);
        }
    }
}
