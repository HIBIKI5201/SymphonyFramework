using System;

using NUnit.Framework;

using SymphonyFramework.Services;

namespace SymphonyFramework.Tests
{
    /// <summary> PartituraRegistryの登録、検索、待機callbackを検証する。 </summary>
    public sealed class PartituraRegistryTests
    {
        /// <summary> 登録したEntityを型で取得できる。 </summary>
        [Test]
        public void TryRegister_ValidInstance_CanBeFound()
        {
            var registry = new PartituraRegistry();
            var instance = new ServiceA();

            bool registered = registry.TryRegister(
                typeof(ServiceA),
                instance,
                ServiceLifetimeEnum.Persistent,
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
            var registry = new PartituraRegistry();
            var first = new ServiceA();
            var second = new ServiceA();
            registry.TryRegister(
                typeof(ServiceA),
                first,
                ServiceLifetimeEnum.Scene,
                out ServiceRegistrationEntity firstEntity);

            bool registered = registry.TryRegister(
                typeof(ServiceA),
                second,
                ServiceLifetimeEnum.Persistent,
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
            var registry = new PartituraRegistry();
            registry.TryRegister(
                typeof(ServiceA),
                new ServiceA(),
                ServiceLifetimeEnum.Scene,
                out ServiceRegistrationEntity entity);

            bool removed = registry.TryRemove(
                typeof(ServiceA),
                out ServiceRegistrationEntity removedEntity);

            Assert.That(removed, Is.True);
            Assert.That(removedEntity, Is.SameAs(entity));
            Assert.That(entity.IsRegistered, Is.False);
            Assert.That(registry.Contains(typeof(ServiceA)), Is.False);
        }

        /// <summary> 2種類の登録待機callbackを登録成功時に1回だけ実行する。 </summary>
        [Test]
        public void InvokeWaitingActions_RegisteredCallbacks_InvokesOnce()
        {
            var registry = new PartituraRegistry();
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
            var registry = new PartituraRegistry();
            int invocationCount = 0;
            Action<ServiceA> action = _ => invocationCount++;
            registry.RegisterWaitingAction(action);

            registry.UnregisterWaitingAction(action);
            registry.InvokeWaitingActions(typeof(ServiceA), new ServiceA());

            Assert.That(invocationCount, Is.Zero);
        }

        /// <summary> 引数なし待機callbackを解除すると登録後も実行しない。 </summary>
        [Test]
        public void UnregisterWaitingAction_RegisteredParameterlessAction_DoesNotInvoke()
        {
            var registry = new PartituraRegistry();
            int invocationCount = 0;
            Action action = () => invocationCount++;
            registry.RegisterWaitingAction<ServiceA>(action);

            registry.UnregisterWaitingAction<ServiceA>(action);
            registry.InvokeWaitingActions(typeof(ServiceA), new ServiceA());

            Assert.That(invocationCount, Is.Zero);
        }

        /// <summary> 引数なしcallbackが失敗してもpayload付きcallbackを1回実行する。 </summary>
        [Test]
        public void InvokeWaitingActions_ParameterlessActionThrows_InvokesPayloadActionOnce()
        {
            var registry = new PartituraRegistry();
            int payloadInvocationCount = 0;
            registry.RegisterWaitingAction<ServiceA>(
                () => throw new InvalidOperationException("Callback failed."));
            registry.RegisterWaitingAction<ServiceA>(
                _ => payloadInvocationCount++);

            Assert.Throws<InvalidOperationException>(() =>
                registry.InvokeWaitingActions(
                    typeof(ServiceA),
                    new ServiceA()));
            registry.InvokeWaitingActions(typeof(ServiceA), new ServiceA());

            Assert.That(payloadInvocationCount, Is.EqualTo(1));
        }

        /// <summary> ClearはEntityと未実行callbackをすべて無効化する。 </summary>
        [Test]
        public void Clear_RegistrationsAndCallbacks_RemovesAll()
        {
            var registry = new PartituraRegistry();
            int invocationCount = 0;
            registry.TryRegister(
                typeof(ServiceA),
                new ServiceA(),
                ServiceLifetimeEnum.Scene,
                out ServiceRegistrationEntity entity);
            registry.RegisterWaitingAction<ServiceB>(() => invocationCount++);

            registry.Clear();
            registry.InvokeWaitingActions(typeof(ServiceB), new ServiceB());

            Assert.That(entity.IsRegistered, Is.False);
            Assert.That(registry.Entities, Is.Empty);
            Assert.That(invocationCount, Is.Zero);
        }

        private sealed class ServiceA { }

        private sealed class ServiceB { }
    }
}
