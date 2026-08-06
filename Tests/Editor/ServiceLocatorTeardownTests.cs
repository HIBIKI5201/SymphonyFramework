using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.System.ServiceLocate;

namespace SymphonyFrameWork.Tests
{
    /// <summary> 未初期化のService Locatorに対する公開APIの契約を検証する。 </summary>
    public sealed class ServiceLocatorTeardownTests
    {
        /// <summary> 各テストの開始時にLocatorを未初期化状態にする。 </summary>
        [SetUp]
        public void SetUp()
        {
            ServiceLocator.ResetRuntimeState();
            Assert.That(
                ServiceLocator.IsInitialized,
                Is.False,
                "前提としてLocatorが未初期化である必要がある。");
        }

        /// <summary> 他のテストへstatic状態を持ち越さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            ServiceLocator.ResetRuntimeState();
        }

        /// <summary> 未初期化の解除は例外にならずfalseを返す。 </summary>
        [Test]
        public void UnregisterInstance_NotInitialized_ReturnsFalse()
        {
            var instance = new AlphaService();

            Assert.That(ServiceLocator.UnregisterInstance(instance), Is.False);
            Assert.That(ServiceLocator.UnregisterInstance(typeof(AlphaService)), Is.False);
            Assert.That(ServiceLocator.UnregisterInstance<AlphaService>(), Is.False);
        }

        /// <summary> 未初期化の破棄は例外にならずfalseを返す。 </summary>
        [Test]
        public void DestroyInstance_NotInitialized_ReturnsFalse()
        {
            var instance = new AlphaService();

            Assert.That(ServiceLocator.DestroyInstance(instance), Is.False);
            Assert.That(ServiceLocator.DestroyInstance<AlphaService>(), Is.False);
        }

        /// <summary> 未初期化の登録確認は例外にならずfalseを返す。 </summary>
        [Test]
        public void IsExistInstance_NotInitialized_ReturnsFalse()
        {
            var instance = new AlphaService();

            Assert.That(ServiceLocator.IsExistInstance<AlphaService>(), Is.False);
            Assert.That(ServiceLocator.IsExistInstance(instance), Is.False);
            Assert.That(ServiceLocator.IsExistInstance(typeof(AlphaService)), Is.False);
        }

        /// <summary> 未初期化の取得は例外にならずnullとfalseを返す。 </summary>
        [Test]
        public void GetInstance_NotInitialized_ReturnsNull()
        {
            Assert.That(ServiceLocator.GetInstance<AlphaService>(), Is.Null);

            bool found = ServiceLocator.TryGetInstance(out AlphaService result);

            Assert.That(found, Is.False);
            Assert.That(result, Is.Null);
        }

        /// <summary> 未初期化の登録情報照会は空一覧とfalseを返す。 </summary>
        [Test]
        public void GetRegistrationInfos_NotInitialized_ReturnsEmpty()
        {
            IReadOnlyList<ServiceRegistrationInfo> registrationInfos =
                ServiceLocator.GetRegistrationInfos();

            // Is.EmptyではなくHas.Countで確認するのは、初期化済みと同じ具象型
            // （Array.AsReadOnlyのReadOnlyCollection）で返っていることも同時に見るため。
            // 素の配列を返すとCountプロパティが公開されず、この表明が落ちる。
            Assert.That(registrationInfos, Has.Count.EqualTo(0));

            bool found = ServiceLocator.TryGetRegistrationInfo(
                typeof(AlphaService),
                out ServiceRegistrationInfo registrationInfo);

            Assert.That(found, Is.False);
            Assert.That(registrationInfo, Is.EqualTo(default(ServiceRegistrationInfo)));
        }

        /// <summary> 状態を足す登録操作は未初期化を例外で拒否する。 </summary>
        [Test]
        public void RegisterInstance_NotInitialized_Throws()
        {
            var instance = new AlphaService();

            Assert.Throws<SymphonyNotInitializedException>(() =>
                ServiceLocator.RegisterInstance(instance));
            Assert.Throws<SymphonyNotInitializedException>(() =>
                ServiceLocator.RegisterInstance(typeof(AlphaService), instance));
            Assert.Throws<SymphonyNotInitializedException>(() =>
                ServiceLocator.RegisterInstanceWithAutoDispose(instance));
            Assert.Throws<SymphonyNotInitializedException>(() =>
                ServiceLocator.RegisterInstanceWithAutoDispose(typeof(AlphaService), instance));
        }

        /// <summary> 必須取得と登録待機は未初期化を例外で拒否する。 </summary>
        [Test]
        public void GetRequiredInstance_NotInitialized_Throws()
        {
            Assert.Throws<SymphonyNotInitializedException>(() =>
                ServiceLocator.GetRequiredInstance<AlphaService>());

            // 待機系はAwaitableを返すが、未初期化判定は同期部分にあるため呼び出しだけで投げる。
            Assert.Throws<SymphonyNotInitializedException>(() =>
                ServiceLocator.GetInstanceAsync<AlphaService>());
            Assert.Throws<SymphonyNotInitializedException>(() =>
                ServiceLocator.TryGetInstanceAsync<AlphaService>());
            Assert.Throws<SymphonyNotInitializedException>(() =>
                ServiceLocator.RegisterAfterLocate<AlphaService>(() => { }));
            Assert.Throws<SymphonyNotInitializedException>(() =>
                ServiceLocator.RegisterAfterLocate<AlphaService>(_ => { }));
        }

        /// <summary> 引数検証は未初期化判定より先に行う。 </summary>
        [Test]
        public void UnregisterInstance_NotInitializedWithNullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ServiceLocator.UnregisterInstance((Type)null));
            Assert.Throws<ArgumentNullException>(() =>
                ServiceLocator.IsExistInstance((Type)null));
            Assert.Throws<ArgumentNullException>(() =>
                ServiceLocator.TryGetRegistrationInfo(null, out _));
        }

        private sealed class AlphaService { }
    }
}
