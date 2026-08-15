using System;

using SymphonyFrameWork.Core;

using NUnit.Framework;

using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SymphonyLazyObjectが遅延生成し、Unity側の破棄を検知して作り直す契約を検証する。
    /// </summary>
    public sealed class SymphonyLazyObjectTests
    {
        /// <summary> 生成処理が無ければ構築できない。 </summary>
        [Test]
        public void Constructor_NullFactory_Throws()
        {
            Assert.That(
                () => new SymphonyLazyObject<GameObject>(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary> 構築しただけでは生成しない。 </summary>
        [Test]
        public void Constructor_BeforeAccess_DoesNotCreate()
        {
            SymphonyLazyObject<GameObject> lazy = new(CountedCreate, DestroyImmediately);

            Assert.That(_created, Is.Zero);
            Assert.That(lazy.IsAlive, Is.False);

            lazy.Destroy();
        }

        /// <summary> 初回アクセスで生成し、2回目は同じ対象を返す。 </summary>
        [Test]
        public void Value_AccessedTwice_CreatesOnce()
        {
            SymphonyLazyObject<GameObject> lazy = new(CountedCreate, DestroyImmediately);

            GameObject first = lazy.Value;
            GameObject second = lazy.Value;

            Assert.That(_created, Is.EqualTo(1));
            Assert.That(second, Is.SameAs(first));
            Assert.That(lazy.IsAlive, Is.True);

            lazy.Destroy();
        }

        /// <summary> 未生成のうちは取得に失敗する。 </summary>
        [Test]
        public void TryGetValue_BeforeCreation_ReturnsFalseWithoutCreating()
        {
            SymphonyLazyObject<GameObject> lazy = new(CountedCreate, DestroyImmediately);

            Assert.That(lazy.TryGetValue(out GameObject value), Is.False);
            Assert.That(value, Is.Null);
            Assert.That(_created, Is.Zero);
        }

        /// <summary> 生成済みなら取得できる。 </summary>
        [Test]
        public void TryGetValue_AfterCreation_ReturnsInstance()
        {
            SymphonyLazyObject<GameObject> lazy = new(() => new GameObject("lazy"), DestroyImmediately);
            GameObject expected = lazy.Value;

            Assert.That(lazy.TryGetValue(out GameObject value), Is.True);
            Assert.That(value, Is.SameAs(expected));

            lazy.Destroy();
        }

        /// <summary> Unity側で破棄されたら、次のアクセスで作り直す。 </summary>
        [Test]
        public void Value_AfterExternalDestroy_RecreatesInstance()
        {
            SymphonyLazyObject<GameObject> lazy = new(CountedCreate, DestroyImmediately);

            GameObject first = lazy.Value;
            UnityEngine.Object.DestroyImmediate(first);

            Assert.That(lazy.IsAlive, Is.False);
            Assert.That(lazy.TryGetValue(out GameObject _), Is.False);

            GameObject recreated = lazy.Value;

            Assert.That(_created, Is.EqualTo(2));
            Assert.That(recreated, Is.Not.SameAs(first));

            lazy.Destroy();
        }

        /// <summary> 破棄処理を指定した場合はそちらを使う。 </summary>
        [Test]
        public void Destroy_WithDestroyer_UsesProvidedDestroyer()
        {
            int destroyed = 0;
            SymphonyLazyObject<GameObject> lazy = new(
                () => new GameObject("lazy"),
                target =>
                {
                    destroyed++;
                    UnityEngine.Object.DestroyImmediate(target);
                });

            _ = lazy.Value;
            lazy.Destroy();

            Assert.That(destroyed, Is.EqualTo(1));
            Assert.That(lazy.IsAlive, Is.False);
        }

        /// <summary> 未生成の状態で破棄しても破棄処理を呼ばない。 </summary>
        [Test]
        public void Destroy_BeforeCreation_DoesNotInvokeDestroyer()
        {
            int destroyed = 0;
            SymphonyLazyObject<GameObject> lazy = new(
                () => new GameObject("lazy"), _ => destroyed++);

            lazy.Destroy();

            Assert.That(destroyed, Is.Zero);
        }

        /// <summary> 破棄した後も、アクセスすれば作り直せる。 </summary>
        [Test]
        public void Value_AfterDestroy_CreatesAgain()
        {
            SymphonyLazyObject<GameObject> lazy = new(CountedCreate, DestroyImmediately);

            _ = lazy.Value;
            lazy.Destroy();
            _ = lazy.Value;

            Assert.That(_created, Is.EqualTo(2));

            lazy.Destroy();
        }

        /// <summary>
        ///     Edit Modeでも使える破棄処理。
        /// </summary>
        /// <remarks>
        ///     既定の破棄処理は <c>Object.Destroy</c> で、Edit Modeでは使えずエラーになる。
        ///     この型はRuntime向けだが、Edit Modeから使う場合は破棄処理の指定が要る。
        /// </remarks>
        /// <param name="target"> 破棄する対象。 </param>
        private static void DestroyImmediately(GameObject target) => UnityEngine.Object.DestroyImmediate(target);

        /// <summary> CountedCreateが呼ばれた回数。 </summary>
        private int _created;

        /// <summary> 生成回数の記録を毎テストで初期化する。 </summary>
        [SetUp]
        public void SetUp() => _created = 0;

        /// <summary>
        ///     生成回数を数えながらGameObjectを作る。
        /// </summary>
        /// <returns> 生成したGameObject。 </returns>
        private GameObject CountedCreate()
        {
            _created++;
            return new GameObject("lazy");
        }
    }
}
