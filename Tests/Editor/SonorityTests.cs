using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFramework.Audio;
using SymphonyFramework.Core;
using SymphonyFramework.Exceptions;

using UnityEngine;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     Sonorityの初期化状態、引数検証、未登録グループの扱いを検証する。
    /// </summary>
    /// <remarks>
    ///     7.0.0（#225）で、<c>GetSource</c> を必須取得に、<c>TryGetSource</c> を任意取得にした。
    ///     AudioMixerを割り当てないConfigで初期化し、すべてのグループ名を未登録として扱う。
    /// </remarks>
    public sealed class SonorityTests
    {
        /// <summary> 未初期化の音量変更は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void SetVolume_NotInitialized_Throws()
        {
            Assert.That(
                () => Sonority.SetVolume("BGM", 0.5f),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary> 未初期化の必須取得は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void GetSource_NotInitialized_Throws()
        {
            Assert.That(
                () => Sonority.GetSource("BGM"),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary> 未初期化の任意取得も、設定漏れを隠さないよう拒否する。 </summary>
        [Test]
        public void TryGetSource_NotInitialized_Throws()
        {
            Assert.That(
                () => Sonority.TryGetSource("BGM", out _),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary> Configに無いグループの必須取得は、設定漏れとして例外にする。 </summary>
        [Test]
        public void GetSource_UnknownGroup_ThrowsArgumentException()
        {
            Initialize();

            Assert.That(
                () => Sonority.GetSource("BGM"),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary> 空のグループ名の必須取得は、呼び出し誤りとして例外にする。 </summary>
        [Test]
        public void GetSource_EmptyName_ThrowsArgumentException()
        {
            Initialize();

            Assert.That(
                () => Sonority.GetSource(string.Empty),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary> 取得できないグループ名の任意取得は、例外にせずfalseを返す。 </summary>
        [TestCase("BGM")]
        [TestCase("")]
        [TestCase(null)]
        public void TryGetSource_UnresolvableName_ReturnsFalse(string name)
        {
            Initialize();

            Assert.That(Sonority.TryGetSource(name, out AudioSource source), Is.False);
            Assert.That(source, Is.Null);
        }

        /// <summary> 0から1の範囲外の音量は拒否する。 </summary>
        [TestCase(-0.1f)]
        [TestCase(1.1f)]
        public void SetVolume_OutOfRange_Throws(float value)
        {
            Initialize();

            Assert.That(
                () => Sonority.SetVolume("BGM", value),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary> Configに無いグループの音量変更は、反映先が無いため何もしない。 </summary>
        [Test]
        public void SetVolume_UnknownGroup_DoesNothing()
        {
            Initialize();

            Assert.That(() => Sonority.SetVolume("BGM", 0.5f), Throws.Nothing);
        }

        /// <summary> テストごとに未初期化の状態から始める。 </summary>
        [SetUp]
        public void SetUp()
        {
            Sonority.ResetRuntimeState();
        }

        /// <summary> テストで初期化した状態とGameObjectを残さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            Sonority.ResetRuntimeState();
            _factory?.Dispose();
            _factory = null;
        }

        private ObjectFactory _factory;

        /// <summary> AudioMixerを割り当てないConfigで初期化する。 </summary>
        private void Initialize()
        {
            _factory = new ObjectFactory();
            Sonority.Initialize(null, _factory);
        }

        /// <summary> 生成したGameObjectをテスト後に破棄するFactory。 </summary>
        private sealed class ObjectFactory : ISystemObjectFactory, IDisposable
        {
            private readonly List<GameObject> _objects = new();

            /// <inheritdoc />
            public GameObject CreateObject(string name)
            {
                GameObject gameObject = new(name);
                _objects.Add(gameObject);
                return gameObject;
            }

            /// <inheritdoc />
            public T CreateComponent<T>(string name) where T : Component =>
                CreateObject(name).AddComponent<T>();

            /// <summary> 残っているGameObjectを破棄する。 </summary>
            public void Dispose()
            {
                foreach (GameObject gameObject in _objects)
                {
                    if (gameObject) { UnityEngine.Object.DestroyImmediate(gameObject); }
                }

                _objects.Clear();
            }
        }
    }
}
