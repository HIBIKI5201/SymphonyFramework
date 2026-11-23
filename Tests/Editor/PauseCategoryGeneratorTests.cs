using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using NUnit.Framework;

using SymphonyFramework.Core;
using SymphonyFramework.Editor;

using UnityEditor;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFramework.Tests
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
        ///     生成するソースはIPausableを継承したinterfaceである。
        /// </summary>
        [Test]
        public void BuildSource_DeclaresInterfaceInheritingIPausable()
        {
            string source = PauseCategoryGenerator.BuildSource("IGameplayPausable");

            Assert.That(
                source,
                Does.Contain("public interface IGameplayPausable : IPausable"));
            Assert.That(source, Does.Contain("using SymphonyFramework.Pause;"));
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
        ///     生成先は自動生成enumのフォルダの下にあり、専用のAssembly Definitionを持たない。
        /// </summary>
        /// <remarks>
        ///     **自動生成enumのアセンブリに含まれる。** そのアセンブリがSymphonyFrameworkを参照するため、
        ///     カテゴリーはIPausableを継承できる。SymphonyFramework側は生成物を参照しない。
        /// </remarks>
        [Test]
        public void GeneratePath_IsInsideEnumFolder()
        {
            Assert.That(
                PauseCategoryGenerator.GeneratePath,
                Is.EqualTo(EditorSymphonyConstant.ENUM_PATH + "/PauseCategory"));
        }

        /// <summary> 6.19.0以前の専用asmdefの名前を、6.15の旧表記も含めて移行対象にする。 </summary>
        [Test]
        public void LegacyAssemblyNames_IncludeBothCasings()
        {
            Assert.That(
                PauseCategoryGenerator.LegacyAssemblyNames,
                Is.EquivalentTo(new[] { "SymphonyFramework.PauseCategory", "SymphonyFrameWork.PauseCategory" }));
        }

        /// <summary> 旧生成物のusingと継承先を、新しい名前空間とIPausableへ書き換える。 </summary>
        [TestCase("using SymphonyFramework.System;")]
        [TestCase("using SymphonyFrameWork.System;")]
        public void RewriteLegacySource_OldSource_UsesFermataNames(string legacyUsing)
        {
            string legacy = legacyUsing + "\n\n"
                            + "///     このinterfaceを実装した対象は、PauseManager.SetPause&lt;IUiPausable&gt;(true)\n"
                            + "public interface IUiPausable : PauseManager.IPausable\n{\n}\n";

            string migrated = PauseCategoryGenerator.RewriteLegacySource(legacy);

            Assert.That(migrated, Does.StartWith("using SymphonyFramework.Pause;"));
            Assert.That(migrated, Does.Contain("public interface IUiPausable : IPausable"));
            Assert.That(migrated, Does.Contain("Fermata.SetPause&lt;IUiPausable&gt;"));
            Assert.That(migrated, Does.Not.Contain("PauseManager"));
        }

        /// <summary> 新しい生成物は書き換えない。 </summary>
        [Test]
        public void RewriteLegacySource_CurrentSource_IsUnchanged()
        {
            string source = PauseCategoryGenerator.BuildSource("IUiPausable");

            Assert.That(PauseCategoryGenerator.RewriteLegacySource(source), Is.EqualTo(source));
        }

        /// <summary> Framework本体のasmdefは自動生成enumのアセンブリを参照しない。 </summary>
        /// <remarks>
        ///     **参照は生成物からFrameworkへの片方向である。** 逆向きを残すと、ポーズカテゴリーが
        ///     IPausableを継承するためにFrameworkを参照したときに循環する。
        /// </remarks>
        [Test]
        public void FrameworkAsmdef_DoesNotReferenceGeneratedAssembly()
        {
            string json = File.ReadAllText(EditorSymphonyConstant.FRAMEWORK_PATH + "/SymphonyFramework.asmdef");
            AssemblyDefinitionData asmdef = JsonUtility.FromJson<AssemblyDefinitionData>(json);

            foreach (string reference in asmdef.references)
            {
                string path = AssetDatabase.GUIDToAssetPath(reference.Replace("GUID:", string.Empty));
                Assert.That(path, Does.Not.EndWith(".Enum.asmdef"), $"'{reference}' は自動生成enumのasmdefです。");
            }
        }
    }
}
