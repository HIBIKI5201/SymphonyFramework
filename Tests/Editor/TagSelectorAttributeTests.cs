using System;
using System.Reflection;

using NUnit.Framework;

using SymphonyFrameWork.Attribute;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     TagSelectorAttributeがタグフィルター名の指定を公開する契約を検証する。
    /// </summary>
    public sealed class TagSelectorAttributeTests
    {
        /// <summary>
        ///     引数無しで生成した場合はフィルターを持たない。
        /// </summary>
        [Test]
        public void Constructor_WithoutArgument_HasNoFilter()
        {
            TagSelectorAttribute attribute = new();

            Assert.That(attribute.FilterMethodName, Is.Null);
        }

        /// <summary>
        ///     渡したフィルター名をそのまま公開する。
        /// </summary>
        [Test]
        public void Constructor_WithFilterName_ExposesFilterName()
        {
            TagSelectorAttribute attribute = new("IsSelectable");

            Assert.That(attribute.FilterMethodName, Is.EqualTo("IsSelectable"));
        }

        /// <summary>
        ///     フィールドへ1つだけ付けられる属性として宣言されている。
        /// </summary>
        [Test]
        public void AttributeUsage_TargetsFieldWithoutMultiple()
        {
            AttributeUsageAttribute usage = typeof(TagSelectorAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            Assert.That(usage, Is.Not.Null);
            Assert.That(usage.ValidOn, Is.EqualTo(AttributeTargets.Field));
            Assert.That(usage.AllowMultiple, Is.False);
        }
    }
}
