using NUnit.Framework;

using SymphonyFramework.Services;

using UnityEngine;

namespace SymphonyFramework.Tests
{
    /// <summary> PartituraReferenceのキャッシュの扱いを検証する。 </summary>
    public sealed class PartituraReferenceTests
    {
        /// <summary> 各テスト用のService HostでFacadeを初期化する。 </summary>
        [SetUp]
        public void SetUp()
        {
            _hostObject = new GameObject("Symphony Locate Object Test Host");
            ServiceHostComponent host = _hostObject.AddComponent<ServiceHostComponent>();
            Partitura.Initialize(host);
        }

        /// <summary> static状態とテスト用GameObjectを解放する。 </summary>
        [TearDown]
        public void TearDown()
        {
            Partitura.ResetRuntimeState();
            DestroyIfAlive(_first);
            DestroyIfAlive(_second);
            DestroyIfAlive(_hostObject);
        }

        private GameObject _hostObject;
        private GameObject _first;
        private GameObject _second;

        /// <summary> 登録済みインスタンスを取得してキャッシュする。 </summary>
        [Test]
        public void Get_Registered_ReturnsRegisteredInstance()
        {
            _first = new GameObject("First");
            Partitura.Register(_first);
            var locate = new PartituraReference<GameObject>();

            Assert.That(locate.Get(), Is.SameAs(_first));
        }

        /// <summary> キャッシュが破棄済みなら、Partituraの現在の登録を取り直す。 </summary>
        [Test]
        public void Get_CachedInstanceDestroyed_ReturnsCurrentRegistration()
        {
            _first = new GameObject("First");
            Partitura.Register(_first);
            var locate = new PartituraReference<GameObject>();
            locate.Get();

            Partitura.Unregister(_first);
            Object.DestroyImmediate(_first);
            _second = new GameObject("Second");
            Partitura.Register(_second);

            Assert.That(locate.Get(), Is.SameAs(_second));
        }

        /// <summary> キャッシュが破棄済みで登録も無ければ、取得に失敗する。 </summary>
        [Test]
        public void TryGet_CachedInstanceDestroyedAndNotRegistered_ReturnsFalse()
        {
            _first = new GameObject("First");
            var locate = new PartituraReference<GameObject>(_first);

            Object.DestroyImmediate(_first);
            bool found = locate.TryGet(out GameObject result);

            Assert.That(found, Is.False);
            Assert.That(ReferenceEquals(result, null), Is.True);
        }

        /// <summary> 破棄済みの初期キャッシュは使わず、Partituraから取得する。 </summary>
        [Test]
        public void Get_InitialCacheDestroyed_ReturnsRegisteredInstance()
        {
            _first = new GameObject("First");
            var locate = new PartituraReference<GameObject>(_first);
            Object.DestroyImmediate(_first);
            _second = new GameObject("Second");
            Partitura.Register(_second);

            Assert.That(locate.Get(), Is.SameAs(_second));
        }

        /// <summary>
        ///     破棄されていなければGameObjectを破棄する。
        /// </summary>
        /// <param name="target"> 破棄するGameObject。 </param>
        private static void DestroyIfAlive(GameObject target)
        {
            if (target != null) { Object.DestroyImmediate(target); }
        }
    }
}
