using System.Collections.Generic;
using System.Text.RegularExpressions;

using NUnit.Framework;

using SymphonyFrameWork.Editor;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     PauseCategoryGeneratorの、生成する名前とソースを決める部分を検証する。
    /// </summary>
    /// <remarks>
    ///     **ファイルの書き出しとAssembly Definitionの生成は検証しない。**
    ///     AssetDatabaseを伴い、利用側プロジェクトへ実際に書き込むためである。
    ///     そちらは人がUnityで確認する。
    /// </remarks>
    public sealed class PauseCategoryGeneratorTests
    {
        /// <summary> カテゴリー名はIとPausableで挟んだinterface名になる。 </summary>
        [Test]
        public void ResolveInterfaceName_ValidName_WrapsWithIAndPausable()
        {
            Assert.That(
                PauseCategoryGenerator.ResolveInterfaceName("Gameplay"),
                Is.EqualTo("IGameplayPausable"));
        }

        /// <summary> 前後の空白は落とす。 </summary>
        [Test]
        public void ResolveInterfaceName_Padded_IsTrimmed()
        {
            Assert.That(
                PauseCategoryGenerator.ResolveInterfaceName("  Ui  "),
                Is.EqualTo("IUiPausable"));
        }

        /// <summary> 空の名前は生成対象にしない。 </summary>
        [Test]
        public void ResolveInterfaceName_Blank_IsExcluded()
        {
            Assert.That(PauseCategoryGenerator.ResolveInterfaceName(null), Is.Null);
            Assert.That(PauseCategoryGenerator.ResolveInterfaceName(string.Empty), Is.Null);
            Assert.That(PauseCategoryGenerator.ResolveInterfaceName("   "), Is.Null);
        }

        /// <summary>
        ///     識別子として使えない名前は除外し、その事実を警告で知らせる。
        /// </summary>
        /// <remarks>
        ///     黙って消すと、設定したのに生成されない理由が利用者に分からない。
        /// </remarks>
        [Test]
        public void ResolveInterfaceName_InvalidIdentifier_IsExcludedWithWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("識別子として使えない"));

            Assert.That(PauseCategoryGenerator.ResolveInterfaceName("1Gameplay"), Is.Null);
        }

        /// <summary> 記号を含む名前も除外する。 </summary>
        [Test]
        public void ResolveInterfaceName_ContainsSymbol_IsExcludedWithWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("識別子として使えない"));

            Assert.That(PauseCategoryGenerator.ResolveInterfaceName("Game Play"), Is.Null);
        }

        /// <summary> 設定した順序を保って解決する。 </summary>
        [Test]
        public void ResolveInterfaceNames_KeepsConfiguredOrder()
        {
            IReadOnlyList<string> names =
                PauseCategoryGenerator.ResolveInterfaceNames(new[] { "Ui", "Gameplay" });

            Assert.That(names, Is.EqualTo(new[] { "IUiPausable", "IGameplayPausable" }));
        }

        /// <summary>
        ///     同じinterface名へ解決される候補は1件だけ残す。
        /// </summary>
        /// <remarks>
        ///     重複したまま生成すると、同名の型が2つできてコンパイルできない。
        /// </remarks>
        [Test]
        public void ResolveInterfaceNames_Duplicates_AreCollapsed()
        {
            IReadOnlyList<string> names =
                PauseCategoryGenerator.ResolveInterfaceNames(new[] { "Ui", " Ui ", "Ui" });

            Assert.That(names, Is.EqualTo(new[] { "IUiPausable" }));
        }

        /// <summary> 使えない候補を含んでいても、残りは生成対象になる。 </summary>
        [Test]
        public void ResolveInterfaceNames_InvalidCandidate_DoesNotDropOthers()
        {
            LogAssert.Expect(LogType.Warning, new Regex("識別子として使えない"));

            IReadOnlyList<string> names =
                PauseCategoryGenerator.ResolveInterfaceNames(new[] { "Gameplay", "1Bad", "Ui" });

            Assert.That(names, Is.EqualTo(new[] { "IGameplayPausable", "IUiPausable" }));
        }

        /// <summary> nullの一覧は空として扱う。 </summary>
        [Test]
        public void ResolveInterfaceNames_Null_IsEmpty()
        {
            Assert.That(PauseCategoryGenerator.ResolveInterfaceNames(null), Is.Empty);
        }

        /// <summary>
        ///     生成するソースはPauseManager.IPausableを継承したinterfaceである。
        /// </summary>
        [Test]
        public void BuildSource_DeclaresInterfaceInheritingIPausable()
        {
            string source = PauseCategoryGenerator.BuildSource("IGameplayPausable");

            Assert.That(
                source,
                Does.Contain("public interface IGameplayPausable : PauseManager.IPausable"));
            Assert.That(source, Does.Contain("using SymphonyFrameWork.System;"));
        }

        /// <summary>
        ///     生成するソースは名前空間を持たない。
        /// </summary>
        /// <remarks>
        ///     自動生成enumと同じく、利用側がusingなしで実装できる形にする。
        /// </remarks>
        [Test]
        public void BuildSource_HasNoNamespace()
        {
            string source = PauseCategoryGenerator.BuildSource("IGameplayPausable");

            Assert.That(source, Does.Not.Contain("namespace "));
        }

        /// <summary> 生成先は生成ディレクトリ配下のinterface名のファイルになる。 </summary>
        [Test]
        public void GetFilePath_IsUnderGeneratePath()
        {
            string path = PauseCategoryGenerator.GetFilePath("IGameplayPausable");

            Assert.That(
                path,
                Is.EqualTo($"{PauseCategoryGenerator.GeneratePath}/IGameplayPausable.cs"));
        }

        /// <summary>
        ///     生成先は自動生成enumとは別のディレクトリである。
        /// </summary>
        /// <remarks>
        ///     **同じアセンブリへは置けない。** カテゴリーはPauseManager.IPausableを継承するため
        ///     SymphonyFrameWorkの参照が要るが、SymphonyFrameWork側がSymphonyFrameWork.Enumを
        ///     参照しているため循環する。
        /// </remarks>
        [Test]
        public void GeneratePath_IsSeparatedFromEnumAssembly()
        {
            Assert.That(
                PauseCategoryGenerator.GeneratePath,
                Does.EndWith("/PauseCategory"));
            Assert.That(
                PauseCategoryGenerator.ASSEMBLY_NAME,
                Is.EqualTo("SymphonyFrameWork.PauseCategory"));
        }
    }
}
