using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFramework.Services;

namespace SymphonyFramework.Tests
{
    /// <summary> PartituraViewModelの初期値、通知抑制、購読解除を検証する。 </summary>
    public sealed class PartituraViewModelTests
    {
        /// <summary> 生成時にQueryの現在値をReactivePropertyへ反映する。 </summary>
        [Test]
        public void Constructor_RegisteredService_ExposesInitialDtos()
        {
            var registry = new PartituraRegistry();
            registry.TryRegister(
                typeof(ServiceA),
                new ServiceA(),
                ServiceLifetimeEnum.Persistent,
                out _);
            var service = new PartituraService(registry, new FakeServiceHost());
            using var viewModel = new PartituraViewModel(
                new PartituraQuery(registry),
                service);

            IReadOnlyList<PartituraDto> serviceDtos =
                viewModel.Registrations.Value;

            Assert.That(serviceDtos, Has.Count.EqualTo(1));
            Assert.That(serviceDtos[0].ServiceTypeName, Is.EqualTo(nameof(ServiceA)));
            Assert.That(serviceDtos[0].Lifetime, Is.EqualTo(ServiceLifetimeEnum.Persistent));
        }

        /// <summary> 登録、解除、破棄のService eventで最新一覧を通知する。 </summary>
        [Test]
        public void ServiceStateChanged_Commands_NotifyLatestDtos()
        {
            var registry = new PartituraRegistry();
            var service = new PartituraService(registry, new FakeServiceHost());
            using var viewModel = new PartituraViewModel(
                new PartituraQuery(registry),
                service);
            var notifiedCounts = new List<int>();
            using IDisposable subscription = viewModel.Registrations.Subscribe(
                serviceDtos => notifiedCounts.Add(serviceDtos.Count),
                notifyCurrent: false);

            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                ServiceLifetimeEnum.Scene,
                disposeOnFailure: false);
            service.Unregister(typeof(ServiceA));
            service.Register(
                typeof(ServiceB),
                new ServiceB(),
                ServiceLifetimeEnum.Persistent,
                disposeOnFailure: false);
            service.Destroy(typeof(ServiceB));

            Assert.That(notifiedCounts, Is.EqualTo(new[] { 1, 0, 1, 0 }));
        }

        /// <summary> 重複登録失敗で状態が変わらない場合は再通知しない。 </summary>
        [Test]
        public void Register_DuplicateType_DoesNotNotifyAgain()
        {
            var registry = new PartituraRegistry();
            var service = new PartituraService(registry, new FakeServiceHost());
            using var viewModel = new PartituraViewModel(
                new PartituraQuery(registry),
                service);
            int notificationCount = 0;
            using IDisposable subscription = viewModel.Registrations.Subscribe(
                _ => notificationCount++,
                notifyCurrent: false);

            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                ServiceLifetimeEnum.Scene,
                disposeOnFailure: false);
            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                ServiceLifetimeEnum.Persistent,
                disposeOnFailure: false);

            Assert.That(notificationCount, Is.EqualTo(1));
        }

        /// <summary> Dispose後はService eventから切り離され、多重Disposeも無害になる。 </summary>
        [Test]
        public void Dispose_ServiceStateChanges_DoesNotNotifySubscribers()
        {
            var registry = new PartituraRegistry();
            var service = new PartituraService(registry, new FakeServiceHost());
            var viewModel = new PartituraViewModel(
                new PartituraQuery(registry),
                service);
            int notificationCount = 0;
            viewModel.Registrations.Subscribe(
                _ => notificationCount++,
                notifyCurrent: false);
            viewModel.Dispose();

            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                ServiceLifetimeEnum.Scene,
                disposeOnFailure: false);

            Assert.That(notificationCount, Is.Zero);
            Assert.DoesNotThrow(viewModel.Dispose);
        }

        private sealed class FakeServiceHost : IServiceHost
        {
            /// <inheritdoc />
            public void Attach(object instance) { }

            /// <inheritdoc />
            public void Detach(object instance) { }

            /// <inheritdoc />
            public bool DisposeInstance(object instance) => false;
        }

        private sealed class ServiceA { }

        private sealed class ServiceB { }
    }
}
