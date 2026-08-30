using NUnit.Framework;

using SymphonyFrameWork.System.SceneBlock;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SceneBlockEntryが値をそのまま公開し、依存の未指定を空として扱う契約を検証する。
    /// </summary>
    public sealed class SceneBlockEntryTests
    {
        /// <summary>
        ///     生成時の値をそのまま公開する。
        /// </summary>
        [Test]
        public void Constructor_ExposesValues()
        {
            SceneBlockEntry entry = new("Town_Props", new[] { "Town_Base" }, 5, true);

            Assert.That(entry.SceneName, Is.EqualTo("Town_Props"));
            Assert.That(entry.DependsOn, Is.EqualTo(new[] { "Town_Base" }));
            Assert.That(entry.Priority, Is.EqualTo(5));
            Assert.That(entry.IsPersistent, Is.True);
        }

        /// <summary>
        ///     依存を指定しない場合は空の一覧になる。
        /// </summary>
        [Test]
        public void DependsOn_NotSpecified_IsEmpty()
        {
            SceneBlockEntry entry = new("Town_Base");

            Assert.That(entry.DependsOn, Is.Empty);
            Assert.That(entry.Priority, Is.Zero);
            Assert.That(entry.IsPersistent, Is.False);
        }

        /// <summary>
        ///     引数無しで生成した場合も依存の取得で例外にならない。
        /// </summary>
        [Test]
        public void DependsOn_DefaultConstructed_IsEmpty()
        {
            SceneBlockEntry entry = new();

            Assert.That(entry.DependsOn, Is.Empty);
        }

        /// <summary>
        ///     渡した依存一覧を後から書き換えてもエントリへ影響しない。
        /// </summary>
        [Test]
        public void DependsOn_SourceMutatedAfterConstruction_IsUnaffected()
        {
            string[] source = { "Town_Base" };
            SceneBlockEntry entry = new("Town_Props", source);

            source[0] = "Changed";

            Assert.That(entry.DependsOn, Is.EqualTo(new[] { "Town_Base" }));
        }
    }
}
