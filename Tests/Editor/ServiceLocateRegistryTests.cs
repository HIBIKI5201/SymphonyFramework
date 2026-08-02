using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.System.ServiceLocate;

namespace SymphonyFrameWork.Tests
{
    /// <summary> ServiceLocateRegistryの登録、検索、待機callbackを検証する。 </summary>
    public sealed class ServiceLocateRegistryTests
    {
        /// <summary> 登録したEntityを型で取得できる。 </summary>
        [Test]
        public void TryRegister_ValidInstance_CanBeFound()
        {
            var registry = new ServiceLocateRegistry();
            var instance = new ServiceA();

            bool registered = registry.TryRegister(
                typeof(ServiceA),
                instance,
                LocateType.Singleton,
                out ServiceRegistrationEntity registeredEntity);

            Assert.That(registered, Is.True);
            Assert.That(registry.TryGet(
                typeof(ServiceA),
                out ServiceRegistrationEntity foundEntity), Is.True);
            Assert.That(foundEntity, Is.SameAs(registeredEntity));
            Assert.That(foundEntity.Instance, Is.SameAs(instance));
        }

        /// <summary> 同じ型の重複登録を拒否して先行Entityを維持する。 </summary>
        [Test]
        public void TryRegister_DuplicateType_KeepsExistingEntity()
        {
            var registry = new ServiceLocateRegistry();
            var first = new ServiceA();
            var second = new ServiceA();
            registry.TryRegister(
                typeof(ServiceA),
                first,
                LocateType.Locator,
                out ServiceRegistrationEntity firstEntity);

            bool registered = registry.TryRegister(
                typeof(ServiceA),
                second,
                LocateType.Singleton,
                out ServiceRegistrationEntity duplicateEntity);

            Assert.That(registered, Is.False);
            Assert.That(duplicateEntity, Is.Null);
            Assert.That(registry.TryGet(
                typeof(ServiceA),
                out ServiceRegistrationEntity foundEntity), Is.True);
            Assert.That(foundEntity, Is.SameAs(firstEntity));
            Assert.That(foundEntity.Instance, Is.SameAs(first));
        }

        /// <summary> 登録解除したEntityを無効化してRegistryから除去する。 </summary>
        [Test]
        public void TryRemove_RegisteredType_UnregistersEntity()
        {
            var registry = new ServiceLocateRegistry();
            registry.TryRegister(
                typeof(ServiceA),
                new ServiceA(),
                LocateType.Locator,
                out ServiceRegistrationEntity entity);

            bool removed = registry.TryRemove(
                typeof(ServiceA),
                out ServiceRegistrationEntity removedEntity);

            Assert.That(removed, Is.True);
            Assert.That(removedEntity, Is.SameAs(entity));
            Assert.That(entity.IsRegistered, Is.False);
            Assert.That(registry.Contains(typeof(ServiceA)), Is.False);
        }

        /// <summary> payload一覧は取得後のRegistry変更から独立したスナップショットになる。 </summary>
        [Test]
        public void GetInstancesSnapshot_AfterRegistryChange_RemainsUnchanged()
        {
            var registry = new ServiceLocateRegistry();
            registry.TryRegister(
                typeof(ServiceA),
                new ServiceA(),
                LocateType.Locator,
                out _);
            IReadOnlyDictionary<Type, object> snapshot =
                registry.GetInstancesSnapshot();

            registry.TryRegister(
                typeof(ServiceB),
                new ServiceB(),
                LocateType.Locator,
                out _);

            Assert.That(snapshot.Count, Is.EqualTo(1));
            Assert.That(snapshot.ContainsKey(typeof(ServiceA)), Is.True);
            Assert.That(snapshot.ContainsKey(typeof(ServiceB)), Is.False);
        }

        /// <summary> 2種類の登録待機callbackを登録成功時に1回だけ実行する。 </summary>
        [Test]
        public void InvokeWaitingActions_RegisteredCallbacks_InvokesOnce()
        {
            var registry = new ServiceLocateRegistry();
            var instance = new ServiceA();
            int actionCount = 0;
            ServiceA received = null;
            registry.RegisterWaitingAction<ServiceA>(() => actionCount++);
            registry.RegisterWaitingAction<ServiceA>(value => received = value);

            registry.InvokeWaitingActions(typeof(ServiceA), instance);
            registry.InvokeWaitingActions(typeof(ServiceA), instance);

            Assert.That(actionCount, Is.EqualTo(1));
            Assert.That(received, Is.SameAs(instance));
        }

        /// <summary> payload付き待機callbackを解除すると登録後も実行しない。 </summary>
        [Test]
        public void UnregisterWaitingAction_RegisteredAction_DoesNotInvoke()
        {
            var registry = new ServiceLocateRegistry();
            int invocationCount = 0;
            Action<ServiceA> action = _ => invocationCount++;
            registry.RegisterWaitingAction(action);

            registry.UnregisterWaitingAction(action);
            registry.InvokeWaitingActions(typeof(ServiceA), new ServiceA());

            Assert.That(invocationCount, Is.Zero);
        }

        /// <summary> ClearはEntityと未実行callbackをすべて無効化する。 </summary>
        [Test]
        public void Clear_RegistrationsAndCallbacks_RemovesAll()
        {
            var registry = new ServiceLocateRegistry();
            int invocationCount = 0;
            registry.TryRegister(
                typeof(ServiceA),
                new ServiceA(),
                LocateType.Locator,
                out ServiceRegistrationEntity entity);
            registry.RegisterWaitingAction<ServiceB>(() => invocationCount++);

            registry.Clear();
            registry.InvokeWaitingActions(typeof(ServiceB), new ServiceB());

            Assert.That(entity.IsRegistered, Is.False);
            Assert.That(registry.GetInstancesSnapshot(), Is.Empty);
            Assert.That(invocationCount, Is.Zero);
        }

        private sealed class ServiceA { }

        private sealed class ServiceB { }
    }
}
