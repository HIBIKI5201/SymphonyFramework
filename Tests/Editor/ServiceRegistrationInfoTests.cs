using NUnit.Framework;

using SymphonyFrameWork.System.ServiceLocate;

namespace SymphonyFrameWork.Tests
{
    /// <summary> ServiceRegistrationInfoの値と参照同一性による等値契約を検証する。 </summary>
    public sealed class ServiceRegistrationInfoTests
    {
        /// <summary> constructorへ渡した登録情報を保持する。 </summary>
        [Test]
        public void Constructor_ValidValues_StoresRegistration()
        {
            var instance = new ServiceA();

            var registrationInfo = new ServiceRegistrationInfo(
                typeof(ServiceA),
                instance,
                LocateTypeEnum.Singleton);

            Assert.That(registrationInfo.ServiceType, Is.EqualTo(typeof(ServiceA)));
            Assert.That(registrationInfo.Instance, Is.SameAs(instance));
            Assert.That(registrationInfo.LocateType, Is.EqualTo(LocateTypeEnum.Singleton));
        }

        /// <summary> 同じpayload参照と値を持つInfoは等値になる。 </summary>
        [Test]
        public void Equality_SameReferenceAndValues_ReturnsTrue()
        {
            var instance = new ServiceA();
            var left = new ServiceRegistrationInfo(
                typeof(ServiceA),
                instance,
                LocateTypeEnum.Locator);
            var right = new ServiceRegistrationInfo(
                typeof(ServiceA),
                instance,
                LocateTypeEnum.Locator);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        /// <summary> 値が等しい別payload参照は異なる登録として扱う。 </summary>
        [Test]
        public void Equality_DifferentPayloadReferences_ReturnsFalse()
        {
            var left = new ServiceRegistrationInfo(
                typeof(string),
                new string('a', 1),
                LocateTypeEnum.Locator);
            var right = new ServiceRegistrationInfo(
                typeof(string),
                new string('a', 1),
                LocateTypeEnum.Locator);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        /// <summary> 登録方式が異なるInfoは同じpayloadでも非等値になる。 </summary>
        [Test]
        public void Equality_DifferentLocateType_ReturnsFalse()
        {
            var instance = new ServiceA();
            var locator = new ServiceRegistrationInfo(
                typeof(ServiceA),
                instance,
                LocateTypeEnum.Locator);
            var singleton = new ServiceRegistrationInfo(
                typeof(ServiceA),
                instance,
                LocateTypeEnum.Singleton);

            Assert.That(locator, Is.Not.EqualTo(singleton));
        }

        private sealed class ServiceA { }
    }
}
