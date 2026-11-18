using System;

using NUnit.Framework;

using SymphonyFramework.Scenes;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     SuitePlanExceptionが検出した全異常を保持して伝える契約を検証する。
    /// </summary>
    public sealed class SuitePlanExceptionTests
    {
        /// <summary>
        ///     ブロック名と説明一覧をそのまま公開する。
        /// </summary>
        [Test]
        public void Constructor_ExposesValues()
        {
            SuitePlanException exception = new("TownBlock", new[] { "異常A", "異常B" });

            Assert.That(exception.SuiteName, Is.EqualTo("TownBlock"));
            Assert.That(exception.Descriptions, Is.EqualTo(new[] { "異常A", "異常B" }));
        }

        /// <summary>
        ///     メッセージへブロック名、件数、全説明を含める。
        /// </summary>
        [Test]
        public void Message_ContainsBlockNameAndAllDescriptions()
        {
            SuitePlanException exception = new("TownBlock", new[] { "異常A", "異常B" });

            Assert.That(exception.Message, Does.Contain("TownBlock"));
            Assert.That(exception.Message, Does.Contain("2件"));
            Assert.That(exception.Message, Does.Contain("異常A"));
            Assert.That(exception.Message, Does.Contain("異常B"));
        }

        /// <summary>
        ///     渡した一覧を後から書き換えても例外の内容は変わらない。
        /// </summary>
        [Test]
        public void Descriptions_SourceMutatedAfterConstruction_IsUnaffected()
        {
            string[] source = { "異常A" };
            SuitePlanException exception = new("TownBlock", source);

            source[0] = "変更後";

            Assert.That(exception.Descriptions, Is.EqualTo(new[] { "異常A" }));
        }

        /// <summary>
        ///     説明一覧が無い場合は生成できない。
        /// </summary>
        [Test]
        public void Constructor_NullDescriptions_Throws()
        {
            Assert.That(
                () => new SuitePlanException("TownBlock", null),
                Throws.TypeOf<ArgumentNullException>());
        }
    }
}
