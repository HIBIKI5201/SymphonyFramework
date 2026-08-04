using System;

using NUnit.Framework;

using SymphonyFrameWork.System.ServiceLocate;

namespace SymphonyFrameWork.Tests
{
    /// <summary> fake Hostを用いてService Locateの所有権と処理順を検証する。 </summary>
    public sealed class ServiceLocateServiceTests
    {
        /// <summary> 通常登録の重複失敗では新しい候補を解放しない。 </summary>
        [Test]
        public void Register_DuplicateWithoutAutoDispose_DoesNotDisposeCandidate()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);
            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Locator,
                disposeOnFailure: false);

            bool registered = service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Locator,
                disposeOnFailure: false);

            Assert.That(registered, Is.False);
            Assert.That(host.DisposeCount, Is.Zero);
        }

        /// <summary> nullのpayloadは登録せずRegistryとHostを変更しない。 </summary>
        [Test]
        public void Register_NullInstance_ReturnsFalseWithoutStateChanges()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);

            bool registered = service.Register(
                typeof(ServiceA),
                null,
                LocateTypeEnum.Singleton,
                disposeOnFailure: true);

            Assert.That(registered, Is.False);
            Assert.That(registry.Contains(typeof(ServiceA)), Is.False);
            Assert.That(host.AttachCount, Is.Zero);
            Assert.That(host.DetachCount, Is.Zero);
            Assert.That(host.DisposeCount, Is.Zero);
        }

        /// <summary> 自動破棄登録の重複失敗では新しい候補を1回解放する。 </summary>
        [Test]
        public void Register_DuplicateWithAutoDispose_DisposesCandidateOnce()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);
            var duplicate = new ServiceA();
            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Locator,
                disposeOnFailure: false);

            bool registered = service.Register(
                typeof(ServiceA),
                duplicate,
                LocateTypeEnum.Locator,
                disposeOnFailure: true);

            Assert.That(registered, Is.False);
            Assert.That(host.DisposeCount, Is.EqualTo(1));
            Assert.That(host.LastDisposedInstance, Is.SameAs(duplicate));
        }

        /// <summary> Singleton登録だけがHostへAttachを依頼する。 </summary>
        [Test]
        public void Register_SingletonAndLocator_AttachesOnlySingleton()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);

            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Singleton,
                disposeOnFailure: false);
            service.Register(
                typeof(ServiceB),
                new ServiceB(),
                LocateTypeEnum.Locator,
                disposeOnFailure: false);

            Assert.That(host.AttachCount, Is.EqualTo(1));
        }

        /// <summary> 待機callbackからはHost接続済みの登録状態を観測できる。 </summary>
        [Test]
        public void Register_WaitingAction_InvokesAfterAttach()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);
            bool wasAttached = false;
            service.RegisterWaitingAction<ServiceA>(
                () => wasAttached = host.AttachCount == 1);

            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Singleton,
                disposeOnFailure: false);

            Assert.That(wasAttached, Is.True);
        }

        /// <summary> 登録解除ではDetachとRegistry除去だけを行いpayloadを解放しない。 </summary>
        [Test]
        public void Unregister_RegisteredType_DetachesWithoutDispose()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);
            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Singleton,
                disposeOnFailure: false);

            bool unregistered = service.Unregister(typeof(ServiceA));

            Assert.That(unregistered, Is.True);
            Assert.That(host.DetachCount, Is.EqualTo(1));
            Assert.That(host.DisposeCount, Is.Zero);
            Assert.That(registry.Contains(typeof(ServiceA)), Is.False);
        }

        /// <summary> 明示破棄ではpayloadを解放してRegistryから除去する。 </summary>
        [Test]
        public void Destroy_RegisteredType_DisposesAndRemoves()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);
            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Singleton,
                disposeOnFailure: false);

            bool destroyed = service.Destroy(typeof(ServiceA));

            Assert.That(destroyed, Is.True);
            Assert.That(host.DisposeCount, Is.EqualTo(1));
            Assert.That(host.DetachCount, Is.EqualTo(1));
            Assert.That(registry.Contains(typeof(ServiceA)), Is.False);
            Assert.That(
                host.OperationLog,
                Is.EqualTo("Attach,Detach,Dispose,"));
        }

        /// <summary> Service経由で2種類の待機callbackを個別に解除できる。 </summary>
        [Test]
        public void WaitingActions_UnregisteredThroughService_DoNotInvoke()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);
            int parameterlessCount = 0;
            int payloadCount = 0;
            Action parameterlessAction = () => parameterlessCount++;
            Action<ServiceA> payloadAction = _ => payloadCount++;
            service.RegisterWaitingAction<ServiceA>(parameterlessAction);
            service.RegisterWaitingAction(payloadAction);

            service.UnregisterWaitingAction<ServiceA>(parameterlessAction);
            service.UnregisterWaitingAction(payloadAction);
            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Locator,
                disposeOnFailure: false);

            Assert.That(parameterlessCount, Is.Zero);
            Assert.That(payloadCount, Is.Zero);
        }

        /// <summary> 成功した状態変更だけを論理更新1回につき1回通知する。 </summary>
        [Test]
        public void StateChanges_SuccessAndFailure_NotifiesExpectedCount()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost();
            var service = new ServiceLocateService(registry, host);
            int notificationCount = 0;
            service.OnStateChanged += () => notificationCount++;

            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Locator,
                disposeOnFailure: false);
            service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Locator,
                disposeOnFailure: false);
            service.Unregister(typeof(ServiceB));
            service.Unregister(typeof(ServiceA));

            Assert.That(notificationCount, Is.EqualTo(2));
        }

        /// <summary> Attach失敗時はRegistryをrollbackして通常候補を解放しない。 </summary>
        [Test]
        public void Register_AttachThrowsWithoutAutoDispose_RollsBackWithoutDispose()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost { ThrowOnAttach = true };
            var service = new ServiceLocateService(registry, host);

            Assert.Throws<InvalidOperationException>(() => service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Singleton,
                disposeOnFailure: false));

            Assert.That(registry.Contains(typeof(ServiceA)), Is.False);
            Assert.That(host.DisposeCount, Is.Zero);
        }

        /// <summary> Attach失敗時はRegistryをrollbackして自動破棄候補を解放する。 </summary>
        [Test]
        public void Register_AttachThrowsWithAutoDispose_RollsBackAndDisposes()
        {
            var registry = new ServiceLocateRegistry();
            var host = new FakeServiceHost { ThrowOnAttach = true };
            var service = new ServiceLocateService(registry, host);

            Assert.Throws<InvalidOperationException>(() => service.Register(
                typeof(ServiceA),
                new ServiceA(),
                LocateTypeEnum.Singleton,
                disposeOnFailure: true));

            Assert.That(registry.Contains(typeof(ServiceA)), Is.False);
            Assert.That(host.DisposeCount, Is.EqualTo(1));
        }

        private sealed class FakeServiceHost : IServiceHost
        {
            internal int AttachCount { get; private set; }
            internal int DetachCount { get; private set; }
            internal int DisposeCount { get; private set; }
            internal object LastDisposedInstance { get; private set; }
            internal bool ThrowOnAttach { get; set; }
            internal string OperationLog { get; private set; } = string.Empty;

            /// <inheritdoc />
            public void Attach(object instance)
            {
                AttachCount++;
                OperationLog += "Attach,";
                if (ThrowOnAttach)
                {
                    throw new InvalidOperationException("Attach failed.");
                }
            }

            /// <inheritdoc />
            public void Detach(object instance)
            {
                DetachCount++;
                OperationLog += "Detach,";
            }

            /// <inheritdoc />
            public bool DisposeInstance(object instance)
            {
                DisposeCount++;
                OperationLog += "Dispose,";
                LastDisposedInstance = instance;
                return true;
            }
        }

        private sealed class ServiceA { }

        private sealed class ServiceB { }
    }
}
