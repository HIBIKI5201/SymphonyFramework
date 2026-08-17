using System;

using NUnit.Framework;

using SymphonyFrameWork.Debugger.HUD;

namespace SymphonyFrameWork.Tests
{
    /// <summary> DebugHUDViewModelの差分通知と破棄契約を検証する。 </summary>
    public sealed class DebugHUDViewModelTests
    {
        private DebugHUDViewModel _viewModel;

        /// <summary> 各テスト用ViewModelを生成する。 </summary>
        [SetUp]
        public void SetUp() => _viewModel = new DebugHUDViewModel();

        /// <summary> ViewModelを破棄する。 </summary>
        [TearDown]
        public void TearDown() => _viewModel?.Dispose();

        /// <summary> 異なるDtoでは購読者へ1回通知する。 </summary>
        [Test]
        public void SetState_ChangedDto_NotifiesSubscriberOnce()
        {
            int notifiedCount = 0;
            using IDisposable subscription =
                _viewModel.State.Subscribe(_ => notifiedCount++, notifyCurrent: false);

            _viewModel.SetState(new DebugHUDDto(true, true, false, 0));

            Assert.That(notifiedCount, Is.EqualTo(1));
        }

        /// <summary> 同値Dtoでは重複通知しない。 </summary>
        [Test]
        public void SetState_EquivalentDto_DoesNotNotifyAgain()
        {
            DebugHUDDto state = new(true, true, false, 2);
            _viewModel.SetState(state);
            int notifiedCount = 0;
            using IDisposable subscription =
                _viewModel.State.Subscribe(_ => notifiedCount++, notifyCurrent: false);

            _viewModel.SetState(state);

            Assert.That(notifiedCount, Is.Zero);
        }

        /// <summary> 破棄後は更新を拒否し、多重破棄は無害である。 </summary>
        [Test]
        public void Dispose_AfterSubscription_StopsNotifications()
        {
            int notifiedCount = 0;
            _viewModel.State.Subscribe(_ => notifiedCount++, notifyCurrent: false);
            _viewModel.Dispose();
            _viewModel.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                _viewModel.SetState(new DebugHUDDto(true, true, true, 1)));
            Assert.That(notifiedCount, Is.Zero);
        }
    }
}
