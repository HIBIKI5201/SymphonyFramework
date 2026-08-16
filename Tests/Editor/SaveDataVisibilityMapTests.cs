using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.Editor;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     セーブデータ型の既定値と上書き値の解決規則を検証する。
    /// </summary>
    public sealed class SaveDataVisibilityMapTests
    {
        /// <summary>
        ///     上書きが無い通常型は管理対象になる。
        /// </summary>
        [Test]
        public void IsManaged_UnknownType_DefaultsToTrue()
        {
            SaveDataVisibilityMap map = CreateMap("Product.Type", false, new Dictionary<string, bool>());

            Assert.That(map.IsManaged("Product.Type"), Is.True);
        }

        /// <summary>
        ///     上書きが無いテストAssembly型は管理対象外になる。
        /// </summary>
        [Test]
        public void IsManaged_TestAssemblyType_DefaultsToFalse()
        {
            SaveDataVisibilityMap map = CreateMap("Tests.Type", true, new Dictionary<string, bool>());

            Assert.That(map.IsManaged("Tests.Type"), Is.False);
        }

        /// <summary>
        ///     テストAssembly型でもtrueの上書きを優先する。
        /// </summary>
        [Test]
        public void IsManaged_OverriddenTestAssemblyType_UsesOverride()
        {
            Dictionary<string, bool> overrides = new() { ["Tests.Type"] = true };
            SaveDataVisibilityMap map = CreateMap("Tests.Type", true, overrides);

            Assert.That(map.IsManaged("Tests.Type"), Is.True);
        }

        /// <summary>
        ///     通常型でもfalseの上書きを優先する。
        /// </summary>
        [Test]
        public void IsManaged_OverriddenNormalType_UsesOverride()
        {
            Dictionary<string, bool> overrides = new() { ["Product.Type"] = false };
            SaveDataVisibilityMap map = CreateMap("Product.Type", false, overrides);

            Assert.That(map.IsManaged("Product.Type"), Is.False);
        }

        /// <summary>
        ///     カタログに無い型は保存済み上書きがあっても管理対象外になる。
        /// </summary>
        [Test]
        public void IsManaged_TypeNotInCatalog_ReturnsFalse()
        {
            Dictionary<string, bool> overrides = new() { ["Missing.Type"] = true };
            SaveDataVisibilityMap map = CreateMap("Product.Type", false, overrides);

            Assert.That(map.IsManaged("Missing.Type"), Is.False);
        }

        /// <summary>
        ///     1件の型カタログから管理対象解決表を作る。
        /// </summary>
        /// <param name="typeFullName"> 型の完全名。 </param>
        /// <param name="isTestAssembly"> テストAssembly型の場合はtrue。 </param>
        /// <param name="overrides"> 明示的な上書き値。 </param>
        /// <returns> 検証対象の管理対象解決表。 </returns>
        private static SaveDataVisibilityMap CreateMap(
            string typeFullName,
            bool isTestAssembly,
            IReadOnlyDictionary<string, bool> overrides)
        {
            KeyValuePair<string, bool>[] catalogEntries =
            {
                new(typeFullName, isTestAssembly),
            };
            return new SaveDataVisibilityMap(catalogEntries, overrides);
        }
    }
}
