using System;

using NUnit.Framework;

using SymphonyFramework.Services;

namespace SymphonyFramework.Tests
{
    /// <summary> ServiceRegistrationEntityの値と登録状態遷移を検証する。 </summary>
    public sealed class ServiceRegistrationEntityTests
    {
        /// <summary> constructorへ渡した登録情報を登録済み状態で保持する。 </summary>
        [Test]
        public void Constructor_ValidValues_StoresRegistration()
        {
            var instance = new object();

            var entity = new ServiceRegistrationEntity(
                typeof(object),
                instance,
                ServiceLifetimeEnum.Persistent);

            Assert.That(entity.ServiceType, Is.EqualTo(typeof(object)));
            Assert.That(entity.Instance, Is.SameAs(instance));
            Assert.That(entity.Lifetime, Is.EqualTo(ServiceLifetimeEnum.Persistent));
            Assert.That(entity.IsRegistered, Is.True);
        }

        /// <summary> Unregisterは状態を1回だけ解除する。 </summary>
        [Test]
        public void Unregister_CalledTwice_ChangesOnlyOnce()
        {
            var entity = new ServiceRegistrationEntity(
                typeof(object),
                new object(),
                ServiceLifetimeEnum.Scene);

            bool first = entity.Unregister();
            bool second = entity.Unregister();

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(entity.IsRegistered, Is.False);
        }

        /// <summary> 登録キーがnullならDomain不変条件違反として拒否する。 </summary>
        [Test]
        public void Constructor_NullServiceType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _ = new ServiceRegistrationEntity(
                    null,
                    new object(),
                    ServiceLifetimeEnum.Scene));
        }

        /// <summary> payloadがnullならDomain不変条件違反として拒否する。 </summary>
        [Test]
        public void Constructor_NullInstance_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _ = new ServiceRegistrationEntity(
                    typeof(object),
                    null,
                    ServiceLifetimeEnum.Scene));
        }
    }
}
