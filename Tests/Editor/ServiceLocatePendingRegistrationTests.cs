using NUnit.Framework;

using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.System.ServiceLocate;

using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary> ServiceLocateServiceの保留登録キューを検証する。 </summary>
    public sealed class ServiceLocatePendingRegistrationTests
    {
        /// <summary> Enqueue後のFlushで登録される。 </summary>
        [Test]
        public void FlushPendingRegistrations_WithQueuedEntry_RegistersInstance()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);
            var instance = new ServiceA();

            service.EnqueuePendingRegistration(typeof(ServiceA), instance, LocateTypeEnum.Locator);
            service.FlushPendingRegistrations();

            Assert.That(registry.Contains(typeof(ServiceA)), Is.True);
        }

        /// <summary> 空キューでのFlushは何もしない。 </summary>
        [Test]
        public void FlushPendingRegistrations_EmptyQueue_DoesNothing()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);

            Assert.DoesNotThrow(() => service.FlushPendingRegistrations());

            Assert.That(host.AttachCount, Is.Zero);
        }

        /// <summary> Flush前のCancelで登録されない。 </summary>
        [Test]
        public void CancelPendingRegistration_BeforeFlush_PreventsRegistration()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);
            var instance = new ServiceA();
            service.EnqueuePendingRegistration(typeof(ServiceA), instance, LocateTypeEnum.Locator);

            bool cancelled = service.CancelPendingRegistration(typeof(ServiceA), instance);
            service.FlushPendingRegistrations();

            Assert.That(cancelled, Is.True);
            Assert.That(registry.Contains(typeof(ServiceA)), Is.False);
        }

        /// <summary> 未Enqueueの対象へのCancelはfalseを返す。 </summary>
        [Test]
        public void CancelPendingRegistration_UnknownEntry_ReturnsFalse()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);

            bool cancelled = service.CancelPendingRegistration(typeof(ServiceA), new ServiceA());

            Assert.That(cancelled, Is.False);
        }

        /// <summary> Flush前に破棄されたUnityObjectはスキップされる。 </summary>
        [Test]
        public void FlushPendingRegistrations_DestroyedUnityObjectBetweenEnqueueAndFlush_SkipsEntry()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);
            var hostObject = new GameObject("PendingRegistrationTest");
            Transform target = hostObject.transform;
            service.EnqueuePendingRegistration(typeof(Transform), target, LocateTypeEnum.Locator);

            Object.DestroyImmediate(hostObject);
            service.FlushPendingRegistrations();

            Assert.That(registry.Contains(typeof(Transform)), Is.False);
        }

        private sealed class FakeServiceHost : IServiceHost
        {
            internal int AttachCount { get; private set; }

            /// <inheritdoc />
            public void Attach(object instance) => AttachCount++;

            /// <inheritdoc />
            public void Detach(object instance)
            {
            }

            /// <inheritdoc />
            public bool DisposeInstance(object instance) => true;
        }

        private sealed class ServiceA
        {
        }
    }

    /// <summary> ServiceLocatorの保留登録用エントリポイントを検証する。 </summary>
    public sealed class ServiceLocateAutoRegistrationBridgeTests
    {
        /// <summary> 各テスト用のService HostでFacadeを初期化する。 </summary>
        [SetUp]
        public void SetUp()
        {
            _hostObject = new GameObject("Service Locator Bridge Test Host");
            ServiceHostComponent host = _hostObject.AddComponent<ServiceHostComponent>();
            ServiceLocator.Initialize(host);
        }

        /// <summary> static状態とテスト用GameObjectを解放する。 </summary>
        [TearDown]
        public void TearDown()
        {
            ServiceLocator.ResetRuntimeState();
            if (_hostObject != null)
            {
                Object.DestroyImmediate(_hostObject);
            }
        }

        private GameObject _hostObject;

        /// <summary> EnqueueとFlushを通した登録が既存のGetInstanceから取得できる。 </summary>
        [Test]
        public void EnqueueAutoRegistration_ThenFlush_BecomesRetrievable()
        {
            var instance = new BridgeService();

            ServiceLocator.EnqueueAutoRegistration(typeof(BridgeService), instance, LocateTypeEnum.Locator);
            Assert.That(ServiceLocator.GetInstance<BridgeService>(), Is.Null);

            ServiceLocator.FlushPendingRegistrations();

            Assert.That(ServiceLocator.GetInstance<BridgeService>(), Is.SameAs(instance));
        }

        /// <summary> CancelPendingRegistrationで反映前に取り消せる。 </summary>
        [Test]
        public void CancelPendingRegistration_BeforeFlush_PreventsVisibleRegistration()
        {
            var instance = new BridgeService();
            ServiceLocator.EnqueueAutoRegistration(typeof(BridgeService), instance, LocateTypeEnum.Locator);

            bool cancelled = ServiceLocator.CancelPendingRegistration(typeof(BridgeService), instance);
            ServiceLocator.FlushPendingRegistrations();

            Assert.That(cancelled, Is.True);
            Assert.That(ServiceLocator.GetInstance<BridgeService>(), Is.Null);
        }

        private sealed class BridgeService
        {
        }
    }

    /// <summary> 未初期化状態でのServiceLocator保留登録APIの挙動を検証する。 </summary>
    public sealed class ServiceLocateAutoRegistrationNotInitializedTests
    {
        /// <summary> 未初期化であることを前提へ入る前に確認する。 </summary>
        [SetUp]
        public void SetUp()
        {
            ServiceLocator.ResetRuntimeState();
            Assert.That(ServiceLocator.IsInitialized, Is.False);
        }

        /// <summary> 未初期化でも次のテストへ影響を残さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            ServiceLocator.ResetRuntimeState();
        }

        /// <summary> 登録系は例外のまま。 </summary>
        [Test]
        public void EnqueueAutoRegistration_NotInitialized_Throws()
        {
            Assert.Throws<SymphonyNotInitializedException>(
                () => ServiceLocator.EnqueueAutoRegistration(typeof(object), new object(), LocateTypeEnum.Locator));
        }

        /// <summary> 取り消しは何も無いものとしてfalseを返す。 </summary>
        [Test]
        public void CancelPendingRegistration_NotInitialized_ReturnsFalse()
        {
            Assert.That(
                ServiceLocator.CancelPendingRegistration(typeof(object), new object()),
                Is.False);
        }

        /// <summary> Flushは未初期化でも例外を投げない。 </summary>
        [Test]
        public void FlushPendingRegistrations_NotInitialized_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ServiceLocator.FlushPendingRegistrations());
        }
    }
}
