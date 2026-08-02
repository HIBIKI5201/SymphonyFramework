using System;

using NUnit.Framework;

using SymphonyFrameWork.System.ServiceLocate;

namespace SymphonyFrameWork.Tests
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
                LocateType.Singleton);

            Assert.That(entity.ServiceType, Is.EqualTo(typeof(object)));
            Assert.That(entity.Instance, Is.SameAs(instance));
            Assert.That(entity.LocateType, Is.EqualTo(LocateType.Singleton));
            Assert.That(entity.IsRegistered, Is.True);
        }

        /// <summary> Unregisterは状態を1回だけ解除する。 </summary>
        [Test]
        public void Unregister_CalledTwice_ChangesOnlyOnce()
        {
            var entity = new ServiceRegistrationEntity(
                typeof(object),
                new object(),
                LocateType.Locator);

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
                    LocateType.Locator));
        }

        /// <summary> payloadがnullならDomain不変条件違反として拒否する。 </summary>
        [Test]
        public void Constructor_NullInstance_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _ = new ServiceRegistrationEntity(
                    typeof(object),
                    null,
                    LocateType.Locator));
        }
    }
}
