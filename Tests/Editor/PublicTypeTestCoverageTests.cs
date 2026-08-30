using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using SymphonyFrameWork.Core;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     公開型のテスト網羅を、増やす方向にしか動かないよう固定する。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Issue #104（全機能へのテスト追加）は1つのRoundで終わる規模ではない。
    ///         **一度に埋めるのではなく、埋め残しを明示して増やせないようにする。**
    ///     </para>
    ///     <para>
    ///         判定は「公開型 <c>X</c> に対して <c>Tests/Editor/XTests.cs</c> があるか」で行う。
    ///         型名とテストファイル名の対応という粗い基準だが、**新しい公開型をテスト無しで
    ///         追加したときに必ず落ちる**ことがこの検査の目的である。
    ///     </para>
    ///     <para>
    ///         テストを書いたら <see cref="UntestedPublicTypes" /> から行を消す。
    ///         **行を足してはならない。**足さないと通らない変更は、テストを書くべき変更である。
    ///     </para>
    /// </remarks>
    public sealed class PublicTypeTestCoverageTests
    {
        /// <summary>
        ///     この検査を導入した時点でテストが無かった公開型。
        /// </summary>
        /// <remarks>
        ///     **この一覧は減らすためにある。** Issue #104 の残作業そのものである。
        /// </remarks>
        private static readonly string[] UntestedPublicTypes =
        {
            "AssemblyDefinitionData",
            "AssemblyGenerator",
            "AssetProtectionModeEnum",
            "AssetStoreToolsCombinePackageStrategy",
            "AssetStoreToolsCreateZipStrategy",
            "AssetStoreToolsPackageExportContext",
            "AssetStoreToolsPackagePlan",
            "AssetStoreToolsPackagePlanEntry",
            "AssetStoreToolsPackageStepStrategy",
            "AssetStoreToolsPackageWindow",
            "AssetStoreToolsPackager",
            "AssetStoreToolsPackagerData",
            "AssetStoreToolsPackagerProvider",
            "AssetStoreToolsSinglePackageStrategy",
            "AssetStoreToolsStandardStrategy",
            "AssetStoreToolsUsedDependenciesStrategy",
            "AudioConfigDrawer",
            "AudioManager",
            "AutoEnumGenerator",
            "AutoEnumGeneratorConfig",
            "AutoEnumGeneratorWindow",
            "DisplayTextAttribute",
            "DisplayTextDecoratorDrawer",
            "EditorSymphonyConstant",
            "EnumGenerator",
            "FolderGenerator",
            "IGameObject",
            "IInitializeAsync",
            "IInjectable",
            "IPausable",
            "InitializeTypeEnum",
            "LoadTypeEnum",
            "LocateTypeEnum",
            "LogKindEnum",
            "PackageDirectoryInfo",
            "PackageModeEnum",
            "PauseManager",
            "PauseWindow",
            "PlayerPrefsSaveDataLoaderStrategy",
            "ReadOnlyAttribute",
            "ReadOnlyDrawer",
            "SaveDataContent",
            "SaveDataDebugState",
            "SaveDataEntryInfo",
            "SaveDataLoaderStrategy",
            "SaveDataOperationEnum",
            "SaveDataOperationException",
            "SaveDataSettingProvider",
            "SaveDataWindow",
            "SaveStore",
            "SceneInitializationException",
            "SceneLoadConfigDrawer",
            "SceneLoadStateEnum",
            "SceneLoadWindow",
            "SceneLoader",
            "SceneNameSelectorDrawer",
            "ServiceInjector",
            "ServiceLocateComponent",
            "ServiceLocateWindow",
            "ServiceLocator",
            "ServiceNotRegisteredException",
            "SubclassSelectorDrawer",
            "SymphonyAdministrator",
            "SymphonyAssetProtector",
            "SymphonyConfigLocator",
            "SymphonyConfigManager",
            "SymphonyConstant",
            "SymphonyDebugLogger",
            "SymphonyDocumentPageEnum",
            "SymphonyDocumentation",
            "SymphonyEditorConfigLocator",
            "SymphonyLocateObject",
            "SymphonyNotInitializedException",
            "SymphonyPackageLoader",
            "SymphonyStopWatch",
            "SymphonyTween",
            "SymphonyUserSettingConfig",
            "SymphonyVisualElement",
            "TagSelectorDrawer",
            "TagsAndLayersPostProcessor",
            "TagsAndLayersSettingData",
        };

        /// <summary> 公開型の宣言。 </summary>
        private static readonly Regex PublicTypePattern = new(
            @"^\s*public\s+(?:static\s+|sealed\s+|abstract\s+|readonly\s+|partial\s+)*"
            + @"(?:class|struct|interface|enum)\s+(?<name>\w+)",
            RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary> 新しい公開型が、テスト無しで追加されていない。 </summary>
        [Test]
        public void PublicTypes_WithoutTests_AreOnlyTheKnownBacklog()
        {
            HashSet<string> tested = TestedTypeNames();

            List<string> unexpected = PublicTypeNames()
                .Where(name => !tested.Contains(name))
                .Where(name => Array.IndexOf(UntestedPublicTypes, name) < 0)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.That(
                unexpected,
                Is.Empty,
                "テストの無い公開型が増えています。Tests/Editor/<型名>Tests.cs を追加してください。"
                + " 検証手段が無い場合だけ、理由を添えてこのテストの一覧へ足します。"
                + Environment.NewLine + string.Join(Environment.NewLine, unexpected));
        }

        /// <summary> 一覧の型にテストを書いたら、一覧から消す。 </summary>
        [Test]
        public void UntestedBacklog_TypesWithTests_AreRemovedFromList()
        {
            HashSet<string> tested = TestedTypeNames();

            List<string> resolved = UntestedPublicTypes
                .Where(tested.Contains)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.That(
                resolved,
                Is.Empty,
                "テストが書かれた型が残作業の一覧に残っています。UntestedPublicTypes から消してください。"
                + Environment.NewLine + string.Join(Environment.NewLine, resolved));
        }

        /// <summary> 一覧に、既に存在しない型が残っていない。 </summary>
        [Test]
        public void UntestedBacklog_RemovedTypes_AreNotLeftBehind()
        {
            HashSet<string> declared = PublicTypeNames();

            List<string> stale = UntestedPublicTypes
                .Where(name => !declared.Contains(name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.That(
                stale,
                Is.Empty,
                "存在しない型が残作業の一覧に残っています。UntestedPublicTypes から消してください。"
                + Environment.NewLine + string.Join(Environment.NewLine, stale));
        }

        /// <summary>
        ///     パッケージ本体が宣言する公開型の名前を集める。
        /// </summary>
        /// <returns> 公開型の名前。 </returns>
        private static HashSet<string> PublicTypeNames()
        {
            HashSet<string> names = new(StringComparer.Ordinal);

            foreach (string area in new[] { "Runtime", "Core", "Editor" })
            {
                string root = Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, area);

                Assert.That(Directory.Exists(root), Is.True, $"'{root}' が見つかりません。");

                foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    foreach (Match match in PublicTypePattern.Matches(File.ReadAllText(path)))
                    {
                        names.Add(match.Groups["name"].Value);
                    }
                }
            }

            return names;
        }

        /// <summary>
        ///     テストファイルの名前から、テスト対象の型名を集める。
        /// </summary>
        /// <returns> テストがある型の名前。 </returns>
        private static HashSet<string> TestedTypeNames()
        {
            const string suffix = "Tests";
            string root = Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "Tests", "Editor");

            Assert.That(Directory.Exists(root), Is.True, $"'{root}' が見つかりません。");

            HashSet<string> names = new(StringComparer.Ordinal);

            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string name = Path.GetFileNameWithoutExtension(path);

                if (name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    names.Add(name[..^suffix.Length]);
                }
            }

            return names;
        }
    }
}
