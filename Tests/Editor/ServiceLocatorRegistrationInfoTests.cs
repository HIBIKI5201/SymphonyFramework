using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.System.ServiceLocate;

using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary> ServiceLocatorの公開登録情報Queryを検証する。 </summary>
    public sealed class ServiceLocatorRegistrationInfoTests
    {
        /// <summary> 各テスト用のService HostでFacadeを初期化する。 </summary>
        [SetUp]
        public void SetUp()
        {
            _hostObject = new GameObject("Service Locator Test Host");
            ServiceHostComponent host =
                _hostObject.AddComponent<ServiceHostComponent>();
            ServiceLocator.Initialize(host);
        }

        /// <summary> static状態とテスト用GameObjectを解放する。 </summary>
        [TearDown]
        public void TearDown()
        {
            ServiceLocator.ResetRuntimeState();
            if (_hostObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_hostObject);
            }
        }

        private GameObject _hostObject;

        /// <summary> 登録一覧を型名順の公開Infoとして返す。 </summary>
        [Test]
        public void GetRegistrationInfos_RegisteredServices_ReturnsSortedInfos()
        {
            var zebra = new ZebraService();
            var alpha = new AlphaService();
            ServiceLocator.RegisterInstance(
                typeof(ZebraService),
                zebra,
                LocateTypeEnum.Locator);
            ServiceLocator.RegisterInstance(
                typeof(AlphaService),
                alpha,
                LocateTypeEnum.Singleton);

            IReadOnlyList<ServiceRegistrationInfo> registrationInfos =
                ServiceLocator.GetRegistrationInfos();

            Assert.That(registrationInfos, Has.Count.EqualTo(2));
            Assert.That(registrationInfos[0].ServiceType, Is.EqualTo(typeof(AlphaService)));
            Assert.That(registrationInfos[0].Instance, Is.SameAs(alpha));
            Assert.That(registrationInfos[0].LocateType, Is.EqualTo(LocateTypeEnum.Singleton));
            Assert.That(registrationInfos[1].ServiceType, Is.EqualTo(typeof(ZebraService)));
            Assert.That(registrationInfos[1].Instance, Is.SameAs(zebra));
        }

        /// <summary> 点検索は登録値を返し、未登録型とnull入力を契約どおり処理する。 </summary>
        [Test]
        public void TryGetRegistrationInfo_RegisteredMissingAndNullTypes_UsesTryContract()
        {
            var instance = new AlphaService();
            ServiceLocator.RegisterInstance(instance, LocateTypeEnum.Locator);

            bool found = ServiceLocator.TryGetRegistrationInfo(
                typeof(AlphaService),
                out ServiceRegistrationInfo registrationInfo);
            bool missing = ServiceLocator.TryGetRegistrationInfo(
                typeof(ZebraService),
                out ServiceRegistrationInfo missingInfo);

            Assert.That(found, Is.True);
            Assert.That(registrationInfo.Instance, Is.SameAs(instance));
            Assert.That(missing, Is.False);
            Assert.That(missingInfo, Is.EqualTo(default(ServiceRegistrationInfo)));
            Assert.Throws<ArgumentNullException>(() =>
                ServiceLocator.TryGetRegistrationInfo(null, out _));
        }

        private sealed class AlphaService { }

        private sealed class ZebraService { }
    }
}
