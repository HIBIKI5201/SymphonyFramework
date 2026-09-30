using SymphonyFrameWork.Editor;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     フォントの各検証規則をUnityオブジェクトなしで検証する。
    /// </summary>
    internal sealed class FontSnapshotTests
    {
        #region 外部向けAPI

        /// <summary>
        ///     正常なDynamicフォントは通過する。
        /// </summary>
        [Test]
        public void CollectErrors_ValidDynamicFont_ReturnsNoErrors()
        {
            Assert.That(CreateValidSnapshot().CollectErrors("", true), Is.Empty);
        }

        /// <summary>
        ///     Text Settingsのビルド時消去を検出する。
        /// </summary>
        [Test]
        public void CollectErrors_TextSettingsClearsOnBuild_ReturnsError()
        {
            FontSnapshot snapshot = CreateValidSnapshot();
            snapshot.IsTextSettingsClearOnBuild = true;
            Assert.That(snapshot.CollectErrors("", false), Has.Count.EqualTo(1));
            Assert.That(snapshot.CollectErrors("", false)[0], Does.Contain("Text Settings"));
        }

        /// <summary>
        ///     Staticの既定フォントでは、Text Settingsのビルド時消去を問題にしない。
        /// </summary>
        [Test]
        public void CollectErrors_StaticFontWithTextSettingsClear_ReturnsNoErrors()
        {
            FontSnapshot snapshot = CreateValidSnapshot();
            snapshot.PopulationMode = 0;
            snapshot.IsTextSettingsClearOnBuild = true;
            Assert.That(snapshot.CollectErrors("", false), Is.Empty);
        }

        /// <summary>
        ///     未設定の既定フォント以降の判定を省略する。
        /// </summary>
        [Test]
        public void CollectErrors_MissingDefaultFont_SkipsFontRules()
        {
            FontSnapshot snapshot = new();
            Assert.That(snapshot.CollectErrors("日本", true), Has.Count.EqualTo(1));
            Assert.That(snapshot.CollectErrors("日本", true)[0], Does.Contain("Default Font Asset"));
        }

        /// <summary>
        ///     Dynamicフォントのビルド時消去を検出する。
        /// </summary>
        [TestCase(1)]
        [TestCase(2)]
        public void CollectErrors_DynamicClearsOnBuild_ReturnsError(int populationMode)
        {
            FontSnapshot snapshot = CreateValidSnapshot();
            snapshot.PopulationMode = populationMode;
            snapshot.IsFontClearOnBuild = true;
            Assert.That(snapshot.CollectErrors("", false), Has.Count.EqualTo(1));
            Assert.That(snapshot.CollectErrors("", false)[0], Does.Contain("Clear Dynamic Data"));
        }

        /// <summary>
        ///     Multi Atlasは設定で要求した場合だけ検査する。
        /// </summary>
        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void CollectErrors_MultiAtlasDisabled_RespectsRequirement(bool required, int count)
        {
            FontSnapshot snapshot = CreateValidSnapshot();
            snapshot.IsMultiAtlasEnabled = false;
            Assert.That(snapshot.CollectErrors("", required), Has.Count.EqualTo(count));
        }

        /// <summary>
        ///     Dynamicフォントの元フォント欠落を検出する。
        /// </summary>
        [Test]
        public void CollectErrors_DynamicWithoutSource_ReturnsError()
        {
            FontSnapshot snapshot = CreateValidSnapshot();
            snapshot.HasSourceFont = false;
            Assert.That(snapshot.CollectErrors("", false), Has.Count.EqualTo(1));
            Assert.That(snapshot.CollectErrors("", false)[0], Does.Contain("Source Font File"));
        }

        /// <summary>
        ///     既定フォントのビルド依存の欠落を検出する。
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void CollectErrors_DefaultFontNotDependency_ReturnsError(int populationMode)
        {
            FontSnapshot snapshot = CreateValidSnapshot();
            snapshot.PopulationMode = populationMode;
            snapshot.IsDefaultFontDependency = false;
            Assert.That(snapshot.CollectErrors("", false), Has.Count.EqualTo(1));
            Assert.That(snapshot.CollectErrors("", false)[0], Does.Contain("既定フォント"));
        }

        /// <summary>
        ///     元フォントのビルド依存の欠落を検出する。
        /// </summary>
        [Test]
        public void CollectErrors_SourceNotDependency_ReturnsError()
        {
            FontSnapshot snapshot = CreateValidSnapshot();
            snapshot.IsSourceFontDependency = false;
            Assert.That(snapshot.CollectErrors("", false), Has.Count.EqualTo(1));
            Assert.That(snapshot.CollectErrors("", false)[0], Does.Contain("Source Font File"));
        }

        /// <summary>
        ///     StaticフォントへDynamic専用の規則を適用しない。
        /// </summary>
        [Test]
        public void CollectErrors_StaticFont_SkipsDynamicRules()
        {
            FontSnapshot snapshot = new()
            {
                HasDefaultFont = true,
                PopulationMode = 0,
                IsFontClearOnBuild = true,
                IsDefaultFontDependency = true,
            };
            Assert.That(snapshot.CollectErrors("", true), Is.Empty);
        }

        /// <summary>
        ///     DynamicOSは元フォントと依存の規則を適用しない。
        /// </summary>
        [Test]
        public void CollectErrors_DynamicOsWithoutDependencies_SkipsSourceAndDependencyRules()
        {
            FontSnapshot snapshot = new() { HasDefaultFont = true, PopulationMode = 2 };
            Assert.That(snapshot.CollectErrors("", false), Is.Empty);
        }

        /// <summary>
        ///     必須文字が空なら文字検査を行わない。
        /// </summary>
        [TestCase("")]
        [TestCase(null)]
        public void CollectErrors_EmptyRequiredCharacters_SkipsCharacterCheck(string requiredCharacters)
        {
            Assert.That(CreateValidSnapshot().CollectErrors(requiredCharacters, false), Is.Empty);
        }

        /// <summary>
        ///     DynamicフォントはCharacter Tableか元フォントにあれば通過する。
        /// </summary>
        [Test]
        public void CollectErrors_DynamicCharactersSplitAcrossSources_ReturnsNoErrors()
        {
            FontSnapshot snapshot = CreateValidSnapshot();
            snapshot.Characters = new uint[] { '日' };
            snapshot.SourceCharacters = new uint[] { '本' };
            Assert.That(snapshot.CollectErrors("日本", false), Is.Empty);
        }

        /// <summary>
        ///     Staticフォントは元フォントにだけある文字を利用できない。
        /// </summary>
        [Test]
        public void CollectErrors_StaticCharactersOnlyInSource_ReturnsMissingCharacters()
        {
            FontSnapshot snapshot = CreateValidSnapshot();
            snapshot.PopulationMode = 0;
            snapshot.SourceCharacters = new uint[] { '日' };
            Assert.That(snapshot.CollectErrors("日日", false), Has.Count.EqualTo(1));
            Assert.That(snapshot.CollectErrors("日日", false)[0], Does.Contain("日 (U+65E5)"));
        }

        /// <summary>
        ///     サロゲートペアはCharacter TableのUnicodeとして判定する。
        /// </summary>
        [Test]
        public void CollectErrors_SupplementaryCharacterInTable_ReturnsNoErrors()
        {
            FontSnapshot snapshot = CreateValidSnapshot();
            snapshot.Characters = new uint[] { 0x1F600 };
            Assert.That(snapshot.CollectErrors("\U0001F600", false), Is.Empty);
        }

        /// <summary>
        ///     複数の設定不備を一度に返す。
        /// </summary>
        [Test]
        public void CollectErrors_MultipleInvalidSettings_ReturnsAllErrors()
        {
            FontSnapshot snapshot = new()
            {
                HasDefaultFont = true,
                PopulationMode = 1,
                IsTextSettingsClearOnBuild = true,
                IsFontClearOnBuild = true,
            };
            Assert.That(snapshot.CollectErrors("日", true), Has.Count.EqualTo(6));
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     全設定規則を満たすDynamicフォントの値を作る。
        /// </summary>
        private static FontSnapshot CreateValidSnapshot() => new()
        {
            HasDefaultFont = true,
            PopulationMode = 1,
            IsMultiAtlasEnabled = true,
            HasSourceFont = true,
            IsDefaultFontDependency = true,
            IsSourceFontDependency = true,
        };

        #endregion
    }
}
