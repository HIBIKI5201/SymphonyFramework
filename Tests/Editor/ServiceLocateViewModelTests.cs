using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.System.ServiceLocate;

namespace SymphonyFrameWork.Tests
{
    /// <summary> ServiceLocateViewModelの初期値、通知抑制、購読解除を検証する。 </summary>
    public sealed class ServiceLocateViewModelTests
    {
        /// <summary> 生成時にQueryの現在値をReactivePropertyへ反映する。 </summary>
        [Test]
        public void Constructor_RegisteredService_ExposesInitialDtos()
        {
            var registry = new ServiceLocateRegistry();
            registry.TryRegister(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Singleton,
                out _);
            var service = new ServiceLocateService(registry, new FakeServiceHost());
            using var viewModel = new ServiceLocateViewModel(
                new ServiceLocateQuery(registry),
                service);

            IReadOnlyList<ServiceLocateDto> serviceDtos =
                viewModel.Registrations.Value;

            Assert.That(serviceDtos, Has.Count.EqualTo(1));
            Assert.That(serviceDtos[0].ServiceTypeName, Is.EqualTo(nameof(ServiceA)));
            Assert.That(serviceDtos[0].LocateType, Is.EqualTo(LocateTypeEnum.Singleton));
        }

        /// <summary> 登録、解除、破棄のService eventで最新一覧を通知する。 </summary>
        [Test]
        public void ServiceStateChanged_Commands_NotifyLatestDtos()
        {
            var registry = new ServiceLocateRegistry();
            var service = new ServiceLocateService(registry, new FakeServiceHost());
            using var viewModel = new ServiceLocateViewModel(
                new ServiceLocateQuery(registry),
                service);
            var notifiedCounts = new List<int>();
            using IDisposable subscription = viewModel.Registrations.Subscribe(
                serviceDtos => notifiedCounts.Add(serviceDtos.Count),
                notifyCurrent: false);

            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Locator,
                disposeOnFailure: false);
            service.Unregister(typeof(ServiceA));
            service.Register(
                typeof(ServiceB),
                new ServiceB(),
                LocateTypeEnum.Singleton,
                disposeOnFailure: false);
            service.Destroy(typeof(ServiceB));

            Assert.That(notifiedCounts, Is.EqualTo(new[] { 1, 0, 1, 0 }));
        }

        /// <summary> 重複登録失敗で状態が変わらない場合は再通知しない。 </summary>
        [Test]
        public void Register_DuplicateType_DoesNotNotifyAgain()
        {
            var registry = new ServiceLocateRegistry();
            var service = new ServiceLocateService(registry, new FakeServiceHost());
            using var viewModel = new ServiceLocateViewModel(
                new ServiceLocateQuery(registry),
                service);
            int notificationCount = 0;
            using IDisposable subscription = viewModel.Registrations.Subscribe(
                _ => notificationCount++,
                notifyCurrent: false);

            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Locator,
                disposeOnFailure: false);
            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Singleton,
                disposeOnFailure: false);

            Assert.That(notificationCount, Is.EqualTo(1));
        }

        /// <summary> Dispose後はService eventから切り離され、多重Disposeも無害になる。 </summary>
        [Test]
        public void Dispose_ServiceStateChanges_DoesNotNotifySubscribers()
        {
            var registry = new ServiceLocateRegistry();
            var service = new ServiceLocateService(registry, new FakeServiceHost());
            var viewModel = new ServiceLocateViewModel(
                new ServiceLocateQuery(registry),
                service);
            int notificationCount = 0;
            viewModel.Registrations.Subscribe(
                _ => notificationCount++,
                notifyCurrent: false);
            viewModel.Dispose();

            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Locator,
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
