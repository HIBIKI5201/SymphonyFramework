using NUnit.Framework;

using SymphonyFrameWork.Editor;

using System.Collections.Generic;

namespace SymphonyFrameWork.Tests
{
    /// <summary> パッケージ化設定の正規化と、強制包含拡張子の一致判定を検証する。 </summary>
    public sealed class AssetStoreToolsPackagerConfigTests
    {
        /// <summary> 拡張子の先頭にドットが無ければ補う。 </summary>
        [Test]
        public void Normalize_ExtensionWithoutDot_AddsDot()
        {
            var config = new AssetStoreToolsPackagerConfig
            {
                ForceIncludeExtensions = new List<string> { "cs" },
            };

            AssetStoreToolsPackagerConfig normalized = config.Normalize();

            CollectionAssert.AreEqual(new[] { ".cs" }, normalized.ForceIncludeExtensions);
        }

        /// <summary> 拡張子の前後の空白を落とす。 </summary>
        [Test]
        public void Normalize_ExtensionWithSpaces_TrimsSpaces()
        {
            var config = new AssetStoreToolsPackagerConfig
            {
                ForceIncludeExtensions = new List<string> { " .dll " },
            };

            AssetStoreToolsPackagerConfig normalized = config.Normalize();

            CollectionAssert.AreEqual(new[] { ".dll" }, normalized.ForceIncludeExtensions);
        }

        /// <summary> 空文字と空白のみの要素を捨てる。 </summary>
        [Test]
        public void Normalize_EmptyElements_AreRemoved()
        {
            var config = new AssetStoreToolsPackagerConfig
            {
                ForceIncludeExtensions = new List<string> { ".cs", string.Empty, "  " },
            };

            AssetStoreToolsPackagerConfig normalized = config.Normalize();

            CollectionAssert.AreEqual(new[] { ".cs" }, normalized.ForceIncludeExtensions);
        }

        /// <summary> フィールドが欠落したJSONを読んでも空一覧として扱う。 </summary>
        [Test]
        public void Normalize_NullLists_BecomeEmptyLists()
        {
            var config = new AssetStoreToolsPackagerConfig
            {
                IgnoredDirectories = null,
                ForceIncludeExtensions = null,
            };

            AssetStoreToolsPackagerConfig normalized = config.Normalize();

            Assert.That(normalized.IgnoredDirectories, Is.Empty);
            Assert.That(normalized.ForceIncludeExtensions, Is.Empty);
        }

        /// <summary> 除外フォルダ名も空白と空要素を落とす。 </summary>
        [Test]
        public void Normalize_IgnoredDirectories_TrimsAndRemovesEmpty()
        {
            var config = new AssetStoreToolsPackagerConfig
            {
                IgnoredDirectories = new List<string> { " Demigiant ", string.Empty },
            };

            AssetStoreToolsPackagerConfig normalized = config.Normalize();

            CollectionAssert.AreEqual(new[] { "Demigiant" }, normalized.IgnoredDirectories);
        }

        /// <summary> 正規化は元のインスタンスを変更しない。 </summary>
        [Test]
        public void Normalize_DoesNotMutateSource()
        {
            var config = new AssetStoreToolsPackagerConfig
            {
                ForceIncludeExtensions = new List<string> { "cs", string.Empty },
            };

            config.Normalize();

            CollectionAssert.AreEqual(new[] { "cs", string.Empty }, config.ForceIncludeExtensions);
        }

        /// <summary> 既定値は除外フォルダが空で、強制包含拡張子を11件持つ。 </summary>
        [Test]
        public void CreateDefault_HasElevenExtensions()
        {
            AssetStoreToolsPackagerConfig config = AssetStoreToolsPackagerConfig.CreateDefault();

            Assert.That(config.IgnoredDirectories, Is.Empty);
            Assert.That(config.ForceIncludeExtensions.Count, Is.EqualTo(11));
            Assert.That(config.ForceIncludeExtensions, Contains.Item(".cs"));
            Assert.That(config.ForceIncludeExtensions, Contains.Item(".asmdef"));
            Assert.That(config.ForceIncludeExtensions, Contains.Item(".asmref"));
        }

        /// <summary> 一覧に含まれる拡張子で一致する。 </summary>
        [Test]
        public void HasForceIncludeExtension_MatchingExtension_IsTrue()
        {
            bool result = AssetStoreToolsPackager.HasForceIncludeExtension(
                "Assets/AssetStoreTools/Cri/Cri.asmdef",
                new[] { ".asmdef" });

            Assert.That(result, Is.True);
        }

        /// <summary> 拡張子の大文字小文字は無視する。 </summary>
        [Test]
        public void HasForceIncludeExtension_DifferentCase_IsTrue()
        {
            bool result = AssetStoreToolsPackager.HasForceIncludeExtension(
                "Assets/AssetStoreTools/Cri/Cri.ASMDEF",
                new[] { ".asmdef" });

            Assert.That(result, Is.True);
        }

        /// <summary> 一覧に無い拡張子では一致しない。 </summary>
        [Test]
        public void HasForceIncludeExtension_OtherExtension_IsFalse()
        {
            bool result = AssetStoreToolsPackager.HasForceIncludeExtension(
                "Assets/AssetStoreTools/Cri/Icon.png",
                new[] { ".asmdef" });

            Assert.That(result, Is.False);
        }

        /// <summary> 拡張子を持たないパスでは一致しない。 </summary>
        [Test]
        public void HasForceIncludeExtension_NoExtension_IsFalse()
        {
            bool result = AssetStoreToolsPackager.HasForceIncludeExtension(
                "Assets/AssetStoreTools/Cri",
                new[] { ".asmdef" });

            Assert.That(result, Is.False);
        }

        /// <summary> 一覧が空なら強制包含は働かない。 </summary>
        [Test]
        public void HasForceIncludeExtension_EmptyList_IsFalse()
        {
            bool result = AssetStoreToolsPackager.HasForceIncludeExtension(
                "Assets/AssetStoreTools/Cri/Cri.asmdef",
                new string[0]);

            Assert.That(result, Is.False);
        }
    }
}
