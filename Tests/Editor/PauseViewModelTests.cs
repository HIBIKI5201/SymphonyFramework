using System;

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
        /// <summary> 検証用のポーズ対象。 </summary>
        private sealed class TestPausable : PauseManager.IPausable
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
