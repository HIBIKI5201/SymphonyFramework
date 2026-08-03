using System;

using NUnit.Framework;

using SymphonyFrameWork.System;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     PausableRegistryの契約を検証する。
    ///     Unity APIに依存しないため、EditModeで直接検証できる。
    /// </summary>
    public sealed class PausableRegistryTests
    {
        /// <summary> 検証用のポーズ対象。 </summary>
        private sealed class TestPausable : PauseManager.IPausable
        {
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }

            public void Pause() => PauseCount++;
            public void Resume() => ResumeCount++;
        }

        /// <summary> 生成直後は登録が無い。 </summary>
        [Test]
        public void InitialState_IsEmpty()
        {
            var registry = new PausableRegistry();

            Assert.That(registry.Count, Is.Zero);
        }

        /// <summary> 初回登録は成功し、件数が増える。 </summary>
        [Test]
        public void TryRegister_FirstTime_Succeeds()
        {
            var registry = new PausableRegistry();

            bool registered = registry.TryRegister(new TestPausable(), _ => { });

            Assert.That(registered, Is.True);
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        /// <summary> 同じ対象の重複登録は拒否され、件数が増えない。 </summary>
        [Test]
        public void TryRegister_Duplicate_IsRejected()
        {
            var registry = new PausableRegistry();
            var pausable = new TestPausable();
            registry.TryRegister(pausable, _ => { });

            bool registered = registry.TryRegister(pausable, _ => { });

            Assert.That(registered, Is.False);
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        /// <summary> 解除では登録時と同じ処理が返る。購読解除に同一の参照が必要なため。 </summary>
        [Test]
        public void TryUnregister_ReturnsRegisteredHandler()
        {
            var registry = new PausableRegistry();
            var pausable = new TestPausable();
            Action<bool> registeredHandler = _ => { };
            registry.TryRegister(pausable, registeredHandler);

            bool unregistered = registry.TryUnregister(pausable, out Action<bool> handler);

            Assert.That(unregistered, Is.True);
            Assert.That(handler, Is.SameAs(registeredHandler));
            Assert.That(registry.Count, Is.Zero);
        }

        /// <summary> 未登録の解除は失敗する。 </summary>
        [Test]
        public void TryUnregister_NotRegistered_ReturnsFalse()
        {
            var registry = new PausableRegistry();

            Assert.That(registry.TryUnregister(new TestPausable(), out _), Is.False);
        }

        /// <summary> 全消去で件数が0になる。 </summary>
        [Test]
        public void Clear_RemovesAllRegistrations()
        {
            var registry = new PausableRegistry();
            registry.TryRegister(new TestPausable(), _ => { });
            registry.TryRegister(new TestPausable(), _ => { });

            registry.Clear();

            Assert.That(registry.Count, Is.Zero);
        }

        /// <summary> nullを渡した操作は拒否される。 </summary>
        [Test]
        public void NullArguments_Throw()
        {
            var registry = new PausableRegistry();

            Assert.Throws<ArgumentNullException>(() => registry.TryRegister(null, _ => { }));
            Assert.Throws<ArgumentNullException>(() => registry.TryRegister(new TestPausable(), null));
            Assert.Throws<ArgumentNullException>(() => registry.TryUnregister(null, out _));
        }
    }
}
