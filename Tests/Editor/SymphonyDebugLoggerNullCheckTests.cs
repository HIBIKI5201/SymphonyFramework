using System.Text.RegularExpressions;

using SymphonyFrameWork.Debugger.Logger;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary> SymphonyDebugLoggerのnull診断APIが、真のnullと破棄済み参照の双方を扱える契約を検証する。 </summary>
    public sealed class SymphonyDebugLoggerNullCheckTests
    {
        /// <summary> 真のnullでも例外にならず、未代入である旨の警告を出す。 </summary>
        [Test]
        public void CheckComponentNull_NullReference_LogsWarningWithoutException()
        {
            BoxCollider component = null;

            LogAssert.Expect(LogType.Warning, new Regex(@"BoxCollider is null\. \(unassigned\)"));

#pragma warning disable CS0618 // 非推奨APIの挙動そのものを検証する。
            Assert.That(() => component.CheckComponentNull(), Throws.Nothing);
#pragma warning restore CS0618
        }

        /// <summary> 破棄済みのComponentでも例外にならず、破棄済みである旨の警告を出す。 </summary>
        [Test]
        public void CheckComponentNull_DestroyedComponent_LogsWarningWithoutException()
        {
            BoxCollider component = CreateDestroyedComponent();

            LogAssert.Expect(LogType.Warning, new Regex(@"BoxCollider is null\. \(destroyed\)"));

#pragma warning disable CS0618 // 非推奨APIの挙動そのものを検証する。
            Assert.That(() => component.CheckComponentNull(), Throws.Nothing);
#pragma warning restore CS0618
        }

        /// <summary> 生存しているComponentでは警告を出さない。 </summary>
        [Test]
        public void CheckComponentNull_AliveComponent_DoesNotLog()
        {
            GameObject owner = new("CheckComponentNullAlive");

            try
            {
#pragma warning disable CS0618 // 非推奨APIの挙動そのものを検証する。
                Assert.That(() => owner.AddComponent<BoxCollider>().CheckComponentNull(), Throws.Nothing);
#pragma warning restore CS0618
            }
            finally { Object.DestroyImmediate(owner); }

            LogAssert.NoUnexpectedReceived();
        }

        /// <summary> 真のnullをtrueとして報告する。 </summary>
        [Test]
        public void LogAndCheckComponentNull_NullReference_ReturnsTrue()
        {
            BoxCollider component = null;

            LogAssert.Expect(LogType.Warning, new Regex(@"BoxCollider</b> is null"));

            Assert.That(component.LogAndCheckComponentNull(), Is.True);
        }

        /// <summary> 破棄済みのUnityオブジェクトをtrueとして報告する。 </summary>
        [Test]
        public void LogAndCheckComponentNull_DestroyedComponent_ReturnsTrue()
        {
            BoxCollider component = CreateDestroyedComponent();

            LogAssert.Expect(LogType.Warning, new Regex(@"BoxCollider</b> is null"));

            Assert.That(component.LogAndCheckComponentNull(), Is.True);
        }

        /// <summary> 生存しているComponentをfalseとして報告する。 </summary>
        [Test]
        public void LogAndCheckComponentNull_AliveComponent_ReturnsFalse()
        {
            GameObject owner = new("LogAndCheckComponentNullAlive");

            try { Assert.That(owner.AddComponent<BoxCollider>().LogAndCheckComponentNull(), Is.False); }
            finally { Object.DestroyImmediate(owner); }

            LogAssert.NoUnexpectedReceived();
        }

        /// <summary> Unityオブジェクトでない参照は、破棄の概念を持たないため参照比較で判定する。 </summary>
        [Test]
        public void LogAndCheckComponentNull_PlainObject_ReturnsFalse()
        {
            Assert.That("symphony".LogAndCheckComponentNull(), Is.False);

            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        ///     破棄済みのUnityオブジェクト参照を作る。
        /// </summary>
        /// <remarks>
        ///     Componentは単体で破棄できないため、所有するGameObjectごと破棄する。
        /// </remarks>
        /// <returns> マネージド参照は生きているが破棄済みのComponent。 </returns>
        private static BoxCollider CreateDestroyedComponent()
        {
            GameObject owner = new("DestroyedComponentOwner");
            BoxCollider component = owner.AddComponent<BoxCollider>();

            Object.DestroyImmediate(owner);

            return component;
        }
    }
}
