using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFramework.Exceptions;
using SymphonyFramework.Services;

namespace SymphonyFramework.Tests
{
    /// <summary> 未初期化のPartituraに対する公開APIの契約を検証する。 </summary>
    public sealed class PartituraTeardownTests
    {
        /// <summary> 各テストの開始時にPartituraを未初期化状態にする。 </summary>
        [SetUp]
        public void SetUp()
        {
            Partitura.ResetRuntimeState();
            Assert.That(
                Partitura.IsInitialized,
                Is.False,
                "前提としてPartituraが未初期化である必要がある。");
        }

        /// <summary> 他のテストへstatic状態を持ち越さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            Partitura.ResetRuntimeState();
        }

        /// <summary> 未初期化の解除は例外にならずfalseを返す。 </summary>
        [Test]
        public void Unregister_NotInitialized_ReturnsFalse()
        {
            var instance = new AlphaService();

            Assert.That(Partitura.Unregister(instance), Is.False);
            Assert.That(Partitura.Unregister(typeof(AlphaService)), Is.False);
            Assert.That(Partitura.Unregister<AlphaService>(), Is.False);
        }

        /// <summary> 未初期化の破棄は例外にならずfalseを返す。 </summary>
        [Test]
        public void Destroy_NotInitialized_ReturnsFalse()
        {
            var instance = new AlphaService();

            Assert.That(Partitura.Destroy(instance), Is.False);
            Assert.That(Partitura.Destroy<AlphaService>(), Is.False);
        }

        /// <summary> 未初期化の登録確認は例外にならずfalseを返す。 </summary>
        [Test]
        public void IsRegistered_NotInitialized_ReturnsFalse()
        {
            var instance = new AlphaService();

            Assert.That(Partitura.IsRegistered<AlphaService>(), Is.False);
            Assert.That(Partitura.IsRegistered(instance), Is.False);
            Assert.That(Partitura.IsRegistered(typeof(AlphaService)), Is.False);
        }

        /// <summary> 未初期化の任意取得は例外にならずfalseを返す。 </summary>
        [Test]
        public void TryGet_NotInitialized_ReturnsFalse()
        {
            bool found = Partitura.TryGet(out AlphaService result);

            Assert.That(found, Is.False);
            Assert.That(result, Is.Null);
        }

        /// <summary> 未初期化の登録情報照会は空一覧とfalseを返す。 </summary>
        [Test]
        public void GetRegistrationInfos_NotInitialized_ReturnsEmpty()
        {
            IReadOnlyList<ServiceRegistrationInfo> registrationInfos =
                Partitura.GetRegistrationInfos();

            // Is.EmptyではなくHas.Countで確認するのは、初期化済みと同じ具象型
            // （Array.AsReadOnlyのReadOnlyCollection）で返っていることも同時に見るため。
            // 素の配列を返すとCountプロパティが公開されず、この表明が落ちる。
            Assert.That(registrationInfos, Has.Count.EqualTo(0));

            bool found = Partitura.TryGetRegistrationInfo(
                typeof(AlphaService),
                out ServiceRegistrationInfo registrationInfo);

            Assert.That(found, Is.False);
            Assert.That(registrationInfo, Is.EqualTo(default(ServiceRegistrationInfo)));
        }

        /// <summary> 状態を足す登録操作は未初期化を例外で拒否する。 </summary>
        [Test]
        public void Register_NotInitialized_Throws()
        {
            var instance = new AlphaService();

            Assert.Throws<SymphonyNotInitializedException>(() =>
                Partitura.Register(instance));
            Assert.Throws<SymphonyNotInitializedException>(() =>
                Partitura.Register(typeof(AlphaService), instance));
            Assert.Throws<SymphonyNotInitializedException>(() =>
                Partitura.RegisterWithAutoDispose(instance));
            Assert.Throws<SymphonyNotInitializedException>(() =>
                Partitura.RegisterWithAutoDispose(typeof(AlphaService), instance));
        }

        /// <summary> 必須取得と登録待機は未初期化を例外で拒否する。 </summary>
        [Test]
        public void Get_NotInitialized_Throws()
        {
            Assert.Throws<SymphonyNotInitializedException>(() =>
                Partitura.Get<AlphaService>());

            // 待機系はAwaitableを返すが、未初期化判定は同期部分にあるため呼び出しだけで投げる。
            Assert.Throws<SymphonyNotInitializedException>(() =>
                Partitura.GetAsync<AlphaService>());
            Assert.Throws<SymphonyNotInitializedException>(() =>
                Partitura.TryGetAsync<AlphaService>());
            Assert.Throws<SymphonyNotInitializedException>(() =>
                Partitura.WhenRegistered<AlphaService>(_ => { }));
        }

        /// <summary> 引数検証は未初期化判定より先に行う。 </summary>
        [Test]
        public void Unregister_NotInitializedWithNullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Partitura.Unregister((Type)null));
            Assert.Throws<ArgumentNullException>(() =>
                Partitura.IsRegistered((Type)null));
            Assert.Throws<ArgumentNullException>(() =>
                Partitura.TryGetRegistrationInfo(null, out _));
        }

        private sealed class AlphaService { }
    }
}
