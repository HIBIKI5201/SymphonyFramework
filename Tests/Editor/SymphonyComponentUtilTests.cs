using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary> SymphonyComponentUtilの取得・追加・階層検索が起点を含めない契約を検証する。 </summary>
    public sealed class SymphonyComponentUtilTests
    {
        /// <summary> テストで生成したGameObject。TearDownでまとめて破棄する。 </summary>
        private readonly List<GameObject> _created = new();

        /// <summary> 生成したGameObjectを破棄する。 </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _created)
            {
                if (gameObject != null) { Object.DestroyImmediate(gameObject); }
            }

            _created.Clear();
        }

        /// <summary> 未追加のComponentは追加して返す。 </summary>
        [Test]
        public void GetOrAddComponent_NotAttached_AddsAndReturns()
        {
            GameObject target = Create("target");

            BoxCollider component = target.GetOrAddComponent<BoxCollider>();

            Assert.That(component, Is.Not.Null);
            Assert.That(target.GetComponent<BoxCollider>(), Is.SameAs(component));
        }

        /// <summary> 既存のComponentは追加せず再利用する。 </summary>
        [Test]
        public void GetOrAddComponent_AlreadyAttached_ReturnsExisting()
        {
            GameObject target = Create("target");
            BoxCollider existing = target.AddComponent<BoxCollider>();

            Assert.That(target.GetOrAddComponent<BoxCollider>(), Is.SameAs(existing));
            Assert.That(target.GetComponents<BoxCollider>().Length, Is.EqualTo(1));
        }

        /// <summary> 起点自身のComponentは子の検索結果に含めない。 </summary>
        [Test]
        public void GetComponentInChildrenExcludeSelf_OnlyOnSelf_ReturnsNull()
        {
            GameObject target = Create("target");
            target.AddComponent<BoxCollider>();

            Assert.That(target.transform.GetComponentInChildrenExcludeSelf<BoxCollider>(), Is.Null);
        }

        /// <summary> 子のComponentを返す。 </summary>
        [Test]
        public void GetComponentInChildrenExcludeSelf_OnChild_ReturnsChildComponent()
        {
            GameObject target = Create("target");
            GameObject child = Create("child");
            child.transform.SetParent(target.transform);
            BoxCollider expected = child.AddComponent<BoxCollider>();

            Assert.That(
                target.transform.GetComponentInChildrenExcludeSelf<BoxCollider>(), Is.SameAs(expected));
        }

        /// <summary> 孫のComponentも見つける。 </summary>
        [Test]
        public void GetComponentInChildrenExcludeSelf_OnGrandchild_ReturnsDescendantComponent()
        {
            GameObject target = Create("target");
            GameObject child = Create("child");
            GameObject grandchild = Create("grandchild");
            child.transform.SetParent(target.transform);
            grandchild.transform.SetParent(child.transform);
            BoxCollider expected = grandchild.AddComponent<BoxCollider>();

            Assert.That(
                target.transform.GetComponentInChildrenExcludeSelf<BoxCollider>(), Is.SameAs(expected));
        }

        /// <summary> 非アクティブな孫は、既定では検索しない。 </summary>
        [Test]
        public void GetComponentInChildrenExcludeSelf_InactiveGrandchild_IsSkippedByDefault()
        {
            GameObject target = Create("target");
            GameObject child = Create("child");
            GameObject grandchild = Create("grandchild");
            child.transform.SetParent(target.transform);
            grandchild.transform.SetParent(child.transform);
            grandchild.AddComponent<BoxCollider>();
            grandchild.SetActive(false);

            Assert.That(target.transform.GetComponentInChildrenExcludeSelf<BoxCollider>(), Is.Null);
        }

        /// <summary> 直下の非アクティブな子は、既定では検索しない。 </summary>
        [Test]
        public void GetComponentInChildrenExcludeSelf_InactiveDirectChild_IsSkippedByDefault()
        {
            GameObject target = Create("target");
            GameObject child = Create("child");
            child.transform.SetParent(target.transform);
            child.AddComponent<BoxCollider>();
            child.SetActive(false);

            Assert.That(
                target.transform.GetComponentInChildrenExcludeSelf<BoxCollider>(),
                Is.Null);
        }

        /// <summary> 指定すれば非アクティブな子も検索する。 </summary>
        [Test]
        public void GetComponentInChildrenExcludeSelf_IncludeInactive_FindsInactiveChild()
        {
            GameObject target = Create("target");
            GameObject child = Create("child");
            child.transform.SetParent(target.transform);
            BoxCollider expected = child.AddComponent<BoxCollider>();
            child.SetActive(false);

            Assert.That(
                target.transform.GetComponentInChildrenExcludeSelf<BoxCollider>(true),
                Is.SameAs(expected));
        }

        /// <summary> 起点自身のComponentは親の検索結果に含めない。 </summary>
        [Test]
        public void GetComponentInParents_OnlyOnSelf_ReturnsNull()
        {
            GameObject target = Create("target");
            target.AddComponent<BoxCollider>();

            Assert.That(target.transform.GetComponentInParents<BoxCollider>(), Is.Null);
        }

        /// <summary> 最も近い親のComponentを返す。 </summary>
        [Test]
        public void GetComponentInParents_MultipleAncestors_ReturnsNearest()
        {
            GameObject grandparent = Create("grandparent");
            GameObject parent = Create("parent");
            GameObject target = Create("target");
            parent.transform.SetParent(grandparent.transform);
            target.transform.SetParent(parent.transform);

            grandparent.AddComponent<BoxCollider>();
            BoxCollider nearest = parent.AddComponent<BoxCollider>();

            Assert.That(target.transform.GetComponentInParents<BoxCollider>(), Is.SameAs(nearest));
        }

        /// <summary> 親を辿っても見つからなければnullを返す。 </summary>
        [Test]
        public void GetComponentInParents_NoAncestorHasComponent_ReturnsNull()
        {
            GameObject parent = Create("parent");
            GameObject target = Create("target");
            target.transform.SetParent(parent.transform);

            Assert.That(target.transform.GetComponentInParents<BoxCollider>(), Is.Null);
        }

        /// <summary>
        ///     破棄対象として記録したGameObjectを生成する。
        /// </summary>
        /// <param name="name"> 生成するGameObjectの名前。 </param>
        /// <returns> 生成したGameObject。 </returns>
        private GameObject Create(string name)
        {
            GameObject created = new(name);
            _created.Add(created);
            return created;
        }
    }
}
