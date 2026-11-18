using NUnit.Framework;

using SymphonyFramework.Exceptions;
using SymphonyFramework.Services;

using UnityEngine;

namespace SymphonyFramework.Tests
{
    /// <summary> PartituraServiceの保留登録キューを検証する。 </summary>
    public sealed class PartituraPendingRegistrationTests
    {
        /// <summary> Enqueue後のFlushで登録される。 </summary>
        [Test]
        public void FlushPendingRegistrations_WithQueuedEntry_RegistersInstance()
        {
            var registry = new PartituraRegistry();
            var host = new FakeServiceHost();
            var service = new PartituraService(registry, host);
            var instance = new ServiceA();

            service.EnqueuePendingRegistration(typeof(ServiceA), instance, ServiceLifetimeEnum.Scene);
            service.FlushPendingRegistrations();

            Assert.That(registry.Contains(typeof(ServiceA)), Is.True);
        }

        /// <summary> 空キューでのFlushは何もしない。 </summary>
        [Test]
        public void FlushPendingRegistrations_EmptyQueue_DoesNothing()
        {
            var registry = new PartituraRegistry();
            var host = new FakeServiceHost();
            var service = new PartituraService(registry, host);

            Assert.DoesNotThrow(() => service.FlushPendingRegistrations());

            Assert.That(host.AttachCount, Is.Zero);
        }

        /// <summary> Flush前のCancelで登録されない。 </summary>
        [Test]
        public void CancelPendingRegistration_BeforeFlush_PreventsRegistration()
        {
            var registry = new PartituraRegistry();
            var host = new FakeServiceHost();
            var service = new PartituraService(registry, host);
            var instance = new ServiceA();
            service.EnqueuePendingRegistration(typeof(ServiceA), instance, ServiceLifetimeEnum.Scene);

            bool cancelled = service.CancelPendingRegistration(typeof(ServiceA), instance);
            service.FlushPendingRegistrations();

            Assert.That(cancelled, Is.True);
            Assert.That(registry.Contains(typeof(ServiceA)), Is.False);
        }

        /// <summary> 未Enqueueの対象へのCancelはfalseを返す。 </summary>
        [Test]
        public void CancelPendingRegistration_UnknownEntry_ReturnsFalse()
        {
            var registry = new PartituraRegistry();
            var host = new FakeServiceHost();
            var service = new PartituraService(registry, host);

            bool cancelled = service.CancelPendingRegistration(typeof(ServiceA), new ServiceA());

            Assert.That(cancelled, Is.False);
        }

        /// <summary> Flush前に破棄されたUnityObjectはスキップされる。 </summary>
        [Test]
        public void FlushPendingRegistrations_DestroyedUnityObjectBetweenEnqueueAndFlush_SkipsEntry()
        {
            var registry = new PartituraRegistry();
            var host = new FakeServiceHost();
            var service = new PartituraService(registry, host);
            var hostObject = new GameObject("PendingRegistrationTest");
            Transform target = hostObject.transform;
            service.EnqueuePendingRegistration(typeof(Transform), target, ServiceLifetimeEnum.Scene);

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

    /// <summary> Partituraの保留登録用エントリポイントを検証する。 </summary>
    public sealed class PartituraAutoRegistrationBridgeTests
    {
        /// <summary> 各テスト用のService HostでFacadeを初期化する。 </summary>
        [SetUp]
        public void SetUp()
        {
            _hostObject = new GameObject("Partitura Bridge Test Host");
            ServiceHostComponent host = _hostObject.AddComponent<ServiceHostComponent>();
            Partitura.Initialize(host);
        }

        /// <summary> static状態とテスト用GameObjectを解放する。 </summary>
        [TearDown]
        public void TearDown()
        {
            Partitura.ResetRuntimeState();
            if (_hostObject != null)
            {
                Object.DestroyImmediate(_hostObject);
            }
        }

        private GameObject _hostObject;

        /// <summary> EnqueueとFlushを通した登録がGetから取得できる。 </summary>
        [Test]
        public void EnqueueAutoRegistration_ThenFlush_BecomesRetrievable()
        {
            var instance = new BridgeService();

            Partitura.EnqueueAutoRegistration(typeof(BridgeService), instance, ServiceLifetimeEnum.Scene);
            Assert.That(Partitura.TryGet(out BridgeService _), Is.False);

            Partitura.FlushPendingRegistrations();

            Assert.That(Partitura.Get<BridgeService>(), Is.SameAs(instance));
        }

        /// <summary> CancelPendingRegistrationで反映前に取り消せる。 </summary>
        [Test]
        public void CancelPendingRegistration_BeforeFlush_PreventsVisibleRegistration()
        {
            var instance = new BridgeService();
            Partitura.EnqueueAutoRegistration(typeof(BridgeService), instance, ServiceLifetimeEnum.Scene);

            bool cancelled = Partitura.CancelPendingRegistration(typeof(BridgeService), instance);
            Partitura.FlushPendingRegistrations();

            Assert.That(cancelled, Is.True);
            Assert.That(Partitura.TryGet(out BridgeService _), Is.False);
        }

        /// <summary> 同じ型で別インスタンスが登録されていれば、解除しない。 </summary>
        [Test]
        public void UnregisterSameInstance_DifferentInstanceRegistered_KeepsRegistration()
        {
            var registered = new BridgeService();
            var other = new BridgeService();
            Partitura.Register(registered);

            bool unregistered = Partitura.UnregisterSameInstance(typeof(BridgeService), other);

            Assert.That(unregistered, Is.False);
            Assert.That(Partitura.Get<BridgeService>(), Is.SameAs(registered));
        }

        /// <summary> 登録と同じインスタンスなら解除する。 </summary>
        [Test]
        public void UnregisterSameInstance_SameInstance_Unregisters()
        {
            var registered = new BridgeService();
            Partitura.Register(registered);

            bool unregistered = Partitura.UnregisterSameInstance(typeof(BridgeService), registered);

            Assert.That(unregistered, Is.True);
            Assert.That(Partitura.IsRegistered<BridgeService>(), Is.False);
        }

        /// <summary> 未登録の型は警告を出さずfalseを返す。 </summary>
        [Test]
        public void UnregisterSameInstance_NotRegistered_ReturnsFalse()
        {
            Assert.That(
                Partitura.UnregisterSameInstance(typeof(BridgeService), new BridgeService()),
                Is.False);
        }

        private sealed class BridgeService
        {
        }
    }

    /// <summary> 未初期化状態でのPartitura保留登録APIの挙動を検証する。 </summary>
    public sealed class PartituraAutoRegistrationNotInitializedTests
    {
        /// <summary> 未初期化であることを前提へ入る前に確認する。 </summary>
        [SetUp]
        public void SetUp()
        {
            Partitura.ResetRuntimeState();
            Assert.That(Partitura.IsInitialized, Is.False);
        }

        /// <summary> 未初期化でも次のテストへ影響を残さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            Partitura.ResetRuntimeState();
        }

        /// <summary> 登録系は例外のまま。 </summary>
        [Test]
        public void EnqueueAutoRegistration_NotInitialized_Throws()
        {
            Assert.Throws<SymphonyNotInitializedException>(
                () => Partitura.EnqueueAutoRegistration(typeof(object), new object(), ServiceLifetimeEnum.Scene));
        }

        /// <summary> 取り消しは何も無いものとしてfalseを返す。 </summary>
        [Test]
        public void CancelPendingRegistration_NotInitialized_ReturnsFalse()
        {
            Assert.That(
                Partitura.CancelPendingRegistration(typeof(object), new object()),
                Is.False);
        }

        /// <summary> Flushは未初期化でも例外を投げない。 </summary>
        [Test]
        public void FlushPendingRegistrations_NotInitialized_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Partitura.FlushPendingRegistrations());
        }
    }
}
