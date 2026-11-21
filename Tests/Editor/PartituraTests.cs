using NUnit.Framework;

using SymphonyFramework.Services;

using UnityEngine;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     Partituraの取得と、インスタンスを受け取るAPIの契約を検証する。
    /// </summary>
    /// <remarks>
    ///     7.0.0（#233）で、<c>Get</c> を必須取得に、インスタンスを受け取る
    ///     <c>Destroy</c> と <c>IsRegistered</c> を同一参照の判定にそろえた。
    /// </remarks>
    public sealed class PartituraTests
    {
        /// <summary> 各テスト用のService HostでFacadeを初期化する。 </summary>
        [SetUp]
        public void SetUp()
        {
            _hostObject = new GameObject("Partitura Test Host");
            ServiceHostComponent host = _hostObject.AddComponent<ServiceHostComponent>();
            Partitura.Initialize(host);
        }

        /// <summary> static状態とテスト用GameObjectを解放する。 </summary>
        [TearDown]
        public void TearDown()
        {
            Partitura.ResetRuntimeState();
            if (_hostObject != null) { Object.DestroyImmediate(_hostObject); }
            if (_target != null) { Object.DestroyImmediate(_target); }
        }

        private GameObject _hostObject;
        private GameObject _target;

        /// <summary> 登録済みの型はGetで取得できる。 </summary>
        [Test]
        public void Get_Registered_ReturnsInstance()
        {
            var registered = new ServiceA();
            Partitura.Register(registered);

            Assert.That(Partitura.Get<ServiceA>(), Is.SameAs(registered));
        }

        /// <summary> 未登録の型をGetするとServiceNotRegisteredExceptionを投げる。 </summary>
        [Test]
        public void Get_NotRegistered_ThrowsServiceNotRegisteredException()
        {
            Assert.Throws<ServiceNotRegisteredException>(() => Partitura.Get<ServiceA>());
        }

        /// <summary> 破棄済みのUnity Objectは未登録と同じ例外にする。 </summary>
        [Test]
        public void Get_DestroyedUnityObject_ThrowsServiceNotRegisteredException()
        {
            _target = new GameObject("Destroyed Target");
            Partitura.Register(_target);
            Object.DestroyImmediate(_target);

            Assert.Throws<ServiceNotRegisteredException>(() => Partitura.Get<GameObject>());
        }

        /// <summary> 別インスタンスを渡したDestroyは、登録を消さずfalseを返す。 </summary>
        [Test]
        public void Destroy_DifferentInstance_KeepsRegistration()
        {
            var registered = new ServiceA();
            Partitura.Register(registered);

            bool destroyed = Partitura.Destroy(new ServiceA());

            Assert.That(destroyed, Is.False);
            Assert.That(Partitura.Get<ServiceA>(), Is.SameAs(registered));
        }

        /// <summary> 登録と同じインスタンスを渡したDestroyは、登録を破棄する。 </summary>
        [Test]
        public void Destroy_SameInstance_RemovesRegistration()
        {
            var registered = new ServiceA();
            Partitura.Register(registered);

            bool destroyed = Partitura.Destroy(registered);

            Assert.That(destroyed, Is.True);
            Assert.That(Partitura.IsRegistered<ServiceA>(), Is.False);
        }

        /// <summary> インスタンスを渡したIsRegisteredは、同一参照のときだけtrueを返す。 </summary>
        [Test]
        public void IsRegistered_WithInstance_ComparesReference()
        {
            var registered = new ServiceA();
            Partitura.Register(registered);

            Assert.That(Partitura.IsRegistered(registered), Is.True);
            Assert.That(Partitura.IsRegistered(new ServiceA()), Is.False);
            Assert.That(Partitura.IsRegistered<ServiceA>(), Is.True);
        }

        /// <summary> 登録済みの型へのWhenRegisteredは、その場で登録インスタンスを渡す。 </summary>
        [Test]
        public void WhenRegistered_AlreadyRegistered_InvokesImmediately()
        {
            var registered = new ServiceA();
            Partitura.Register(registered);
            ServiceA received = null;

            Partitura.WhenRegistered<ServiceA>(instance => received = instance);

            Assert.That(received, Is.SameAs(registered));
        }

        /// <summary> 未登録の型へのWhenRegisteredは、登録時に呼ばれる。 </summary>
        [Test]
        public void WhenRegistered_RegisteredLater_InvokesOnRegister()
        {
            ServiceA received = null;
            Partitura.WhenRegistered<ServiceA>(instance => received = instance);
            Assert.That(received, Is.Null);

            var registered = new ServiceA();
            Partitura.Register(registered);

            Assert.That(received, Is.SameAs(registered));
        }

        /// <summary> Persistent登録のComponentはHostの子へ移る。 </summary>
        [Test]
        public void Register_Persistent_MovesComponentUnderHost()
        {
            _target = new GameObject("Lifetime Target");
            Transform persistent = _target.transform;
            Partitura.Register(persistent, ServiceLifetimeEnum.Persistent);

            Assert.That(persistent.parent, Is.SameAs(_hostObject.transform));
        }

        private sealed class ServiceA
        {
        }
    }
}
