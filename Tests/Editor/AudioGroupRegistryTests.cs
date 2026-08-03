using System;

using NUnit.Framework;

using SymphonyFrameWork.System;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     AudioGroupRegistryの保持と構築済み記録の契約を検証する。
    ///     Unity APIに依存しないため、EditModeで直接検証できる。
    /// </summary>
    public sealed class AudioGroupRegistryTests
    {
        /// <summary> 指定名の検証用エントリを作る。 </summary>
        /// <param name="groupName"> グループ名。 </param>
        /// <returns> 検証用のエントリ。 </returns>
        private static AudioGroupEntity CreateEntity(string groupName) =>
            new(groupName, null, null, $"{groupName}Volume", 0f);

        /// <summary> 生成直後は未構築で空である。 </summary>
        [Test]
        public void InitialState_IsNotBuiltAndEmpty()
        {
            var registry = new AudioGroupRegistry();

            Assert.That(registry.IsBuilt, Is.False);
            Assert.That(registry.Count, Is.Zero);
        }

        /// <summary> 登録したエントリをグループ名で取得できる。 </summary>
        [Test]
        public void TryAdd_ThenTryGet_ReturnsEntity()
        {
            var registry = new AudioGroupRegistry();
            AudioGroupEntity entity = CreateEntity("BGM");

            Assert.That(registry.TryAdd(entity), Is.True);
            Assert.That(registry.TryGet("BGM", out AudioGroupEntity found), Is.True);
            Assert.That(found, Is.SameAs(entity));
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        /// <summary> 同名の再登録は拒否され、先に登録したものが残る。 </summary>
        [Test]
        public void TryAdd_DuplicateName_KeepsFirst()
        {
            var registry = new AudioGroupRegistry();
            AudioGroupEntity first = CreateEntity("BGM");
            registry.TryAdd(first);

            Assert.That(registry.TryAdd(CreateEntity("BGM")), Is.False);

            registry.TryGet("BGM", out AudioGroupEntity found);
            Assert.That(found, Is.SameAs(first));
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        /// <summary> 未登録名の検索は失敗する。 </summary>
        [Test]
        public void TryGet_UnknownName_ReturnsFalse()
        {
            var registry = new AudioGroupRegistry();

            Assert.That(registry.TryGet("SE", out AudioGroupEntity found), Is.False);
            Assert.That(found, Is.Null);
        }

        /// <summary> 空のグループ名の検索は例外にせず失敗として扱う。 </summary>
        [Test]
        public void TryGet_EmptyName_ReturnsFalse()
        {
            var registry = new AudioGroupRegistry();

            Assert.That(registry.TryGet(null, out _), Is.False);
            Assert.That(registry.TryGet(string.Empty, out _), Is.False);
        }

        /// <summary> nullの登録は拒否される。 </summary>
        [Test]
        public void TryAdd_Null_Throws()
        {
            var registry = new AudioGroupRegistry();

            Assert.Throws<ArgumentNullException>(() => registry.TryAdd(null));
        }

        /// <summary> 構築済みとして記録できる。 </summary>
        [Test]
        public void MarkBuilt_SetsIsBuilt()
        {
            var registry = new AudioGroupRegistry();

            registry.MarkBuilt();

            Assert.That(registry.IsBuilt, Is.True);
        }

        /// <summary>
        ///     **全消去で構築済み記録も戻る。**
        ///     Play Modeをまたいで再構築されるため、ここが戻らないと
        ///     2回目のPlayでAudioSourceが作られなくなる。
        /// </summary>
        [Test]
        public void Clear_ResetsEntriesAndBuiltFlag()
        {
            var registry = new AudioGroupRegistry();
            registry.TryAdd(CreateEntity("BGM"));
            registry.MarkBuilt();

            registry.Clear();

            Assert.That(registry.Count, Is.Zero);
            Assert.That(registry.IsBuilt, Is.False);
            Assert.That(registry.TryGet("BGM", out _), Is.False);
        }
    }
}
