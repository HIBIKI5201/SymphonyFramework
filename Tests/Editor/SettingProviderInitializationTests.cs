using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.Editor;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SettingsProviderのcallbackがpackage-wideな初期化を始めず、
    ///     整備の要求がOrchestratorの集約経路を通る契約を検証する。
    /// </summary>
    public sealed class SettingProviderInitializationTests
    {
        /// <summary>
        ///     発見用属性のcallbackから呼んではならない、package-wideな初期化の入口。
        /// </summary>
        /// <remarks>
        ///     <c>Documentation/CodeGuidelines.md</c> の「ライフサイクルと状態」が、
        ///     <c>MenuItem</c> / <c>SettingsProvider</c> / <c>CustomEditor</c> / <c>UxmlElement</c> の
        ///     callbackからpackage-wideな初期化を開始することを禁じている。
        /// </remarks>
        private static readonly string[] ForbiddenInitializationEntryPoints =
        {
            "SymphonyConfigManager.AllConfigCheck",
            "PackageInitializer.Initialize",
        };

        /// <summary> コメント行は検出対象から外す。規約の説明としてAPI名が出るため。 </summary>
        private static readonly Regex CommentLinePattern = new(@"^\s*(//|/\*|\*)", RegexOptions.Compiled);

        /// <summary> SettingsProviderの実装が、package-wideな初期化を直接開始しない。 </summary>
        [Test]
        public void SettingProviders_Source_DoesNotStartPackageWideInitialization()
        {
            string directory = Path.Combine(
                EditorSymphonyConstant.FRAMEWORK_PATH, "Editor", "SettingProvider");

            Assert.That(Directory.Exists(directory), Is.True, $"'{directory}' が見つかりません。");

            List<string> violations = new();

            foreach (string path in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(path);

                for (int index = 0; index < lines.Length; index++)
                {
                    if (CommentLinePattern.IsMatch(lines[index])) { continue; }

                    foreach (string entryPoint in ForbiddenInitializationEntryPoints)
                    {
                        if (!lines[index].Contains(entryPoint)) { continue; }

                        violations.Add($"{Path.GetFileName(path)}:{index + 1} {entryPoint}");
                    }
                }
            }

            Assert.That(
                violations,
                Is.Empty,
                "SettingsProviderのcallbackからpackage-wideな初期化を開始しないでください。"
                + $" 整備が必要な場合は {nameof(SymphonyEditorOrchestrator)}."
                + $"{nameof(SymphonyEditorOrchestrator.RequestPackageSetup)} へ委譲します。"
                + $"\n{string.Join("\n", violations)}");
        }

        /// <summary> 初期化済みのEditorでは、整備の要求を受け付ける。 </summary>
        [Test]
        public void RequestPackageSetup_AfterInitialization_ReturnsTrue()
        {
            Assert.That(SymphonyEditorOrchestrator.RequestPackageSetup(), Is.True);
        }

        /// <summary> 整備の要求を繰り返しても、例外にならず設定アセットが揃ったままになる。 </summary>
        [Test]
        public void RequestPackageSetup_CalledTwice_KeepsConfigAvailable()
        {
            Assert.That(() => SymphonyEditorOrchestrator.RequestPackageSetup(), Throws.Nothing);
            Assert.That(() => SymphonyEditorOrchestrator.RequestPackageSetup(), Throws.Nothing);

            Assert.That(SymphonyConfigLocator.GetConfig<SaveDataConfig>(), Is.Not.Null);
            Assert.That(SymphonyConfigLocator.GetConfig<DebugHUDConfig>(), Is.Not.Null);
        }
    }
}
