using System;
using System.Reflection;

using NUnit.Framework;

using SymphonyFrameWork.Attribute;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SubclassSelectorAttributeが候補条件とフィルター名の指定を公開する契約を検証する。
    /// </summary>
    public sealed class SubclassSelectorAttributeTests
    {
        /// <summary>
        ///     引数無しで生成した場合はMonoBehaviourを含めずフィルターも持たない。
        /// </summary>
        [Test]
        public void Constructor_WithoutArgument_ExcludesMonoAndHasNoFilter()
        {
            SubclassSelectorAttribute attribute = new();

            Assert.That(attribute.IsIncludeMono(), Is.False);
            Assert.That(attribute.FilterMethodName, Is.Null);
        }

        /// <summary>
        ///     既存の位置指定はMonoBehaviourの許可条件として解釈される。
        /// </summary>
        [Test]
        public void Constructor_PositionalIncludeMono_KeepsPreviousMeaning()
        {
            SubclassSelectorAttribute attribute = new(true);

            Assert.That(attribute.IsIncludeMono(), Is.True);
            Assert.That(attribute.FilterMethodName, Is.Null);
        }

        /// <summary>
        ///     候補条件とフィルター名を同時に指定できる。
        /// </summary>
        [Test]
        public void Constructor_WithFilterName_ExposesBothValues()
        {
            SubclassSelectorAttribute attribute = new(true, "IsSelectable");

            Assert.That(attribute.IsIncludeMono(), Is.True);
            Assert.That(attribute.FilterMethodName, Is.EqualTo("IsSelectable"));
        }

        /// <summary>
        ///     フィルター名だけを名前付き引数で指定できる。
        /// </summary>
        [Test]
        public void Constructor_NamedFilterNameOnly_KeepsIncludeMonoDefault()
        {
            SubclassSelectorAttribute attribute = new(filterMethodName: "IsSelectable");

            Assert.That(attribute.IsIncludeMono(), Is.False);
            Assert.That(attribute.FilterMethodName, Is.EqualTo("IsSelectable"));
        }

        /// <summary>
        ///     フィールドへ1つだけ付けられる属性として宣言されている。
        /// </summary>
        [Test]
        public void AttributeUsage_TargetsFieldWithoutMultiple()
        {
            AttributeUsageAttribute usage = typeof(SubclassSelectorAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            Assert.That(usage, Is.Not.Null);
            Assert.That(usage.ValidOn, Is.EqualTo(AttributeTargets.Field));
            Assert.That(usage.AllowMultiple, Is.False);
        }
    }
}
