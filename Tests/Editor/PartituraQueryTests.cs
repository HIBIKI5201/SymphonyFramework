using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFramework.Services;

using UnityEngine;

namespace SymphonyFramework.Tests
{
    /// <summary> PartituraQueryのInfo／Dto変換とスナップショット性を検証する。 </summary>
    public sealed class PartituraQueryTests
    {
        /// <summary> 登録Info一覧を型の完全名のordinal昇順で返す。 </summary>
        [Test]
        public void GetInfos_MultipleServices_ReturnsOrdinalOrder()
        {
            var registry = new PartituraRegistry();
            registry.TryRegister(
                typeof(ZebraService),
                new ZebraService(),
                ServiceLifetimeEnum.Scene,
                out _);
            registry.TryRegister(
                typeof(AlphaService),
                new AlphaService(),
                ServiceLifetimeEnum.Persistent,
                out _);
            registry.TryRegister(
                typeof(MiddleService),
                new MiddleService(),
                ServiceLifetimeEnum.Scene,
                out _);
            var query = new PartituraQuery(registry);

            IReadOnlyList<ServiceRegistrationInfo> registrationInfos =
                query.GetInfos();

            Assert.That(
                new[]
                {
                    registrationInfos[0].ServiceType,
                    registrationInfos[1].ServiceType,
                    registrationInfos[2].ServiceType
                },
                Is.EqualTo(new[]
                {
                    typeof(AlphaService),
                    typeof(MiddleService),
                    typeof(ZebraService)
                }));
        }

        /// <summary> 点検索で登録値をInfoへ変換し、未登録型ではdefaultを返す。 </summary>
        [Test]
        public void TryGetInfo_RegisteredAndMissingTypes_ReturnsExpectedResults()
        {
            var registry = new PartituraRegistry();
            var instance = new AlphaService();
            registry.TryRegister(
                typeof(AlphaService),
                instance,
                ServiceLifetimeEnum.Persistent,
                out _);
            var query = new PartituraQuery(registry);

            bool found = query.TryGetInfo(
                typeof(AlphaService),
                out ServiceRegistrationInfo registrationInfo);
            bool missing = query.TryGetInfo(
                typeof(MiddleService),
                out ServiceRegistrationInfo missingInfo);

            Assert.That(found, Is.True);
            Assert.That(registrationInfo.ServiceType, Is.EqualTo(typeof(AlphaService)));
            Assert.That(registrationInfo.Instance, Is.SameAs(instance));
            Assert.That(registrationInfo.Lifetime, Is.EqualTo(ServiceLifetimeEnum.Persistent));
            Assert.That(missing, Is.False);
            Assert.That(missingInfo, Is.EqualTo(default(ServiceRegistrationInfo)));
        }

        /// <summary> payload取得と登録有無をRegistryから読み取る。 </summary>
        [Test]
        public void InstanceQueries_RegisteredType_ReturnsPayloadAndContains()
        {
            var registry = new PartituraRegistry();
            var instance = new AlphaService();
            registry.TryRegister(
                typeof(AlphaService),
                instance,
                ServiceLifetimeEnum.Scene,
                out _);
            var query = new PartituraQuery(registry);

            bool found = query.TryGetInstance(
                typeof(AlphaService),
                out object foundInstance);

            Assert.That(found, Is.True);
            Assert.That(foundInstance, Is.SameAs(instance));
            Assert.That(query.Contains(typeof(AlphaService)), Is.True);
            Assert.That(query.Contains(typeof(MiddleService)), Is.False);
        }

        /// <summary> 取得済みInfo／Dto一覧は後続のRegistry変更から独立している。 </summary>
        [Test]
        public void Snapshots_RegistryChangesAfterRead_PreservePreviousCounts()
        {
            var registry = new PartituraRegistry();
            registry.TryRegister(
                typeof(AlphaService),
                new AlphaService(),
                ServiceLifetimeEnum.Scene,
                out _);
            var query = new PartituraQuery(registry);
            IReadOnlyList<ServiceRegistrationInfo> previousInfos = query.GetInfos();
            IReadOnlyList<PartituraDto> previousDtos = query.GetDtos();

            registry.TryRegister(
                typeof(MiddleService),
                new MiddleService(),
                ServiceLifetimeEnum.Persistent,
                out _);

            Assert.That(previousInfos, Has.Count.EqualTo(1));
            Assert.That(previousDtos, Has.Count.EqualTo(1));
            Assert.That(query.GetInfos(), Has.Count.EqualTo(2));
            Assert.That(query.GetDtos(), Has.Count.EqualTo(2));
        }

        /// <summary> 通常objectの実行時型名をDtoのinstance名に使用する。 </summary>
        [Test]
        public void GetDtos_RegularObject_UsesRuntimeTypeName()
        {
            var registry = new PartituraRegistry();
            registry.TryRegister(
                typeof(AlphaService),
                new AlphaService(),
                ServiceLifetimeEnum.Scene,
                out _);
            var query = new PartituraQuery(registry);

            PartituraDto serviceDto = query.GetDtos()[0];

            Assert.That(serviceDto.ServiceTypeName, Is.EqualTo(nameof(AlphaService)));
            Assert.That(serviceDto.InstanceName, Is.EqualTo(nameof(AlphaService)));
            Assert.That(serviceDto.Lifetime, Is.EqualTo(ServiceLifetimeEnum.Scene));
        }

        /// <summary> 破棄済みUnity ObjectをDtoでは安全な表示文字列へ変換する。 </summary>
        [Test]
        public void GetDtos_DestroyedUnityObject_UsesDestroyedLabel()
        {
            var gameObject = new GameObject("Service Object");
            Transform instance = gameObject.transform;
            var registry = new PartituraRegistry();
            registry.TryRegister(
                typeof(Transform),
                instance,
                ServiceLifetimeEnum.Persistent,
                out _);
            UnityEngine.Object.DestroyImmediate(gameObject);
            var query = new PartituraQuery(registry);

            PartituraDto serviceDto = query.GetDtos()[0];

            Assert.That(serviceDto.InstanceName, Is.EqualTo("(Destroyed)"));
        }

        private sealed class AlphaService { }

        private sealed class MiddleService { }

        private sealed class ZebraService { }
    }
}
