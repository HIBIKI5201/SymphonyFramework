using NUnit.Framework;

using SymphonyFrameWork.Editor;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary> パッケージ出力パイプラインの絞り込み、テンプレート、手順の実行を検証する。 </summary>
    public sealed class AssetStoreToolsPackagePipelineTests
    {
        /// <summary> 述語に合わないパスが出力対象から外れる。 </summary>
        [Test]
        public void FilterAssetPaths_Predicate_KeepsOnlyMatching()
        {
            AssetStoreToolsPackagePlanEntry entry = CreateEntry(
                "Assets/AssetStoreTools/Cri/Cri.asmdef",
                "Assets/AssetStoreTools/Cri/Icon.png",
                "Assets/AssetStoreTools/Cri/Runtime.cs");

            entry.FilterAssetPaths(path => path.EndsWith(".cs", StringComparison.Ordinal));

            CollectionAssert.AreEqual(
                new[] { "Assets/AssetStoreTools/Cri/Runtime.cs" },
                entry.AssetPaths);
        }

        /// <summary> 絞り込みでは出力対象が増えない。 </summary>
        [Test]
        public void FilterAssetPaths_AlwaysTruePredicate_DoesNotAddPaths()
        {
            AssetStoreToolsPackagePlanEntry entry = CreateEntry(
                "Assets/AssetStoreTools/Cri/Icon.png",
                "Assets/AssetStoreTools/Cri/Runtime.cs");
            int initialCount = entry.AssetPaths.Count;

            entry.FilterAssetPaths(_ => true);
            entry.FilterAssetPaths(_ => true);

            Assert.That(entry.AssetPaths.Count, Is.EqualTo(initialCount));
        }

        /// <summary> 絞り込みを行うとIsFilteredが立つ。 </summary>
        [Test]
        public void FilterAssetPaths_AfterCall_SetsIsFiltered()
        {
            AssetStoreToolsPackagePlanEntry entry = CreateEntry("Assets/AssetStoreTools/Cri/Runtime.cs");

            Assert.That(entry.IsFiltered, Is.False);

            entry.FilterAssetPaths(_ => true);

            Assert.That(entry.IsFiltered, Is.True);
        }

        /// <summary> 述語がnullなら例外を投げる。 </summary>
        [Test]
        public void FilterAssetPaths_NullPredicate_Throws()
        {
            AssetStoreToolsPackagePlanEntry entry = CreateEntry("Assets/AssetStoreTools/Cri/Runtime.cs");

            Assert.That(() => entry.FilterAssetPaths(null), Throws.ArgumentNullException);
        }

        /// <summary> 1件でも絞り込まれていれば計画全体が絞り込み済みとして扱われる。 </summary>
        [Test]
        public void IsFiltered_OneEntryFiltered_IsTrue()
        {
            AssetStoreToolsPackagePlanEntry filtered = CreateEntry("Assets/AssetStoreTools/A/A.cs");
            AssetStoreToolsPackagePlanEntry untouched = CreateEntry("Assets/AssetStoreTools/B/B.cs");
            AssetStoreToolsPackagePlan plan = CreatePlan(filtered, untouched);

            Assert.That(plan.IsFiltered, Is.False);

            filtered.FilterAssetPaths(_ => true);

            Assert.That(plan.IsFiltered, Is.True);
        }

        /// <summary> 使用中でなくても強制包含拡張子のアセットは残る。 </summary>
        [Test]
        public void IsUsedTarget_ForceIncludedExtension_IsTrueWithoutUsage()
        {
            bool result = AssetStoreToolsUsedDependenciesStrategy.IsUsedTarget(
                "Assets/AssetStoreTools/Cri/Cri.asmdef",
                new HashSet<string>(),
                new[] { ".asmdef" });

            Assert.That(result, Is.True);
        }

        /// <summary> 使用中アセットは強制包含拡張子でなくても残る。 </summary>
        [Test]
        public void IsUsedTarget_UsedAsset_IsTrue()
        {
            bool result = AssetStoreToolsUsedDependenciesStrategy.IsUsedTarget(
                "Assets/AssetStoreTools/Cri/Icon.png",
                new HashSet<string> { "Assets/AssetStoreTools/Cri/Icon.png" },
                new[] { ".asmdef" });

            Assert.That(result, Is.True);
        }

        /// <summary> 使用中でも強制包含でもないアセットは外れる。 </summary>
        [Test]
        public void IsUsedTarget_UnusedAsset_IsFalse()
        {
            bool result = AssetStoreToolsUsedDependenciesStrategy.IsUsedTarget(
                "Assets/AssetStoreTools/Cri/Icon.png",
                new HashSet<string>(),
                new[] { ".asmdef" });

            Assert.That(result, Is.False);
        }

        /// <summary> テンプレートはSingles、Used Dependencies、Create ZIPをこの順で持つ。 </summary>
        [Test]
        public void CreateTemplate_ContainsSinglesUsedDependenciesAndZipInOrder()
        {
            AssetStoreToolsPackagePipeline pipeline = AssetStoreToolsPackagePipeline.CreateTemplate();

            try
            {
                Assert.That(pipeline.Steps.Count, Is.EqualTo(3));
                Assert.That(pipeline.Steps[0], Is.InstanceOf<AssetStoreToolsSinglePackageStrategy>());
                Assert.That(pipeline.Steps[1], Is.InstanceOf<AssetStoreToolsUsedDependenciesStrategy>());
                Assert.That(pipeline.Steps[2], Is.InstanceOf<AssetStoreToolsCreateZipStrategy>());
            }
            finally
            {
                ScriptableObject.DestroyImmediate(pipeline);
            }
        }

        /// <summary> テンプレートには統合パッケージ出力を含めない。 </summary>
        [Test]
        public void CreateTemplate_DoesNotContainCombineStrategy()
        {
            AssetStoreToolsPackagePipeline pipeline = AssetStoreToolsPackagePipeline.CreateTemplate();

            try
            {
                Assert.That(
                    pipeline.Steps.Any(step => step is AssetStoreToolsCombinePackageStrategy),
                    Is.False);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(pipeline);
            }
        }

        /// <summary> サブクラスセレクターで未選択のままのnull要素は計画へ持ち込まれない。 </summary>
        [Test]
        public void SelectValidSteps_NullElements_AreRemoved()
        {
            var steps = new List<AssetStoreToolsPackageStepStrategy>
            {
                null,
                new AssetStoreToolsCreateZipStrategy(),
                null,
            };

            AssetStoreToolsPackageStepStrategy[] result =
                AssetStoreToolsPackagePipelineRunner.SelectValidSteps(steps);

            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0], Is.InstanceOf<AssetStoreToolsCreateZipStrategy>());
        }

        /// <summary> 手順がnullでも実行時に落ちない。 </summary>
        [Test]
        public void RunPlanSteps_NullSteps_DoesNotThrow()
        {
            AssetStoreToolsPackagePlan plan = CreatePlan(
                AssetStoreToolsPackagePipelineRunner.SelectValidSteps(null),
                CreateEntry("Assets/AssetStoreTools/A/A.cs"));

            Assert.That(() => AssetStoreToolsPackagePipelineRunner.RunPlanSteps(plan), Throws.Nothing);
        }

        /// <summary> 1つの手順が例外を投げても後続の手順は実行される。 </summary>
        [Test]
        public void RunPlanSteps_StepThrows_ContinuesToNextStep()
        {
            var throwing = new ThrowingPlanStepStub();
            var following = new RecordingPlanStepStub();
            AssetStoreToolsPackagePlan plan = CreatePlan(
                new AssetStoreToolsPackageStepStrategy[] { throwing, following },
                CreateEntry("Assets/AssetStoreTools/A/A.cs"));

            // 失敗した手順はLogErrorで記録される。期待するログとして消費する。
            LogAssert.Expect(LogType.Error, new Regex("手順の計画に失敗しました"));

            AssetStoreToolsPackagePipelineRunner.RunPlanSteps(plan);

            Assert.That(following.IsPlanned, Is.True);
        }

        /// <summary>
        ///     絶対パスをfile URIへ変換し、空白と番号記号を符号化する。
        /// </summary>
        [Test]
        public void BuildExportCompletionMessage_AbsolutePath_ContainsFileUri()
        {
            string exportFullPath = Path.Combine(Path.GetTempPath(), "Export Folder #1");

            string message = AssetStoreToolsPackagePipelineRunner.BuildExportCompletionMessage(
                exportFullPath,
                "Export Folder #1");

            Assert.That(message, Does.Contain("href=\"file:"));
            Assert.That(message, Does.Contain("Export%20Folder%20%231"));
        }

        /// <summary>
        ///     リンクの表示文字列には従来の相対パスを使用する。
        /// </summary>
        [Test]
        public void BuildExportCompletionMessage_LocalPath_DisplaysExistingPath()
        {
            string exportFullPath = Path.Combine(Path.GetTempPath(), "ExportedPackages", "Package");
            string exportLocalPath = Path.Combine("Assets", "ExportedPackages", "Package");

            string message = AssetStoreToolsPackagePipelineRunner.BuildExportCompletionMessage(
                exportFullPath,
                exportLocalPath);

            Assert.That(message, Does.Contain($">{exportLocalPath}</a>"));
        }

        /// <summary>
        ///     URI属性と表示パスのマークアップ文字をエスケープする。
        /// </summary>
        [Test]
        public void BuildExportCompletionMessage_MarkupCharacters_EscapesRichText()
        {
            string exportFullPath = Path.Combine(Path.GetTempPath(), "Export & Package");
            string exportLocalPath = "Export & <Package> \"Quoted\"";

            string message = AssetStoreToolsPackagePipelineRunner.BuildExportCompletionMessage(
                exportFullPath,
                exportLocalPath);

            Assert.That(message, Does.Contain("Export%20&amp;%20Package"));
            Assert.That(message, Does.Contain(">Export &amp; &lt;Package&gt; &quot;Quoted&quot;</a>"));
            Assert.That(message, Does.Not.Contain(exportLocalPath));
        }

        /// <summary>
        ///     絶対ファイルURIへ変換できない場合はプレーンテキストへフォールバックする。
        /// </summary>
        [Test]
        public void BuildExportCompletionMessage_InvalidFullPath_FallsBackToPlainText()
        {
            const string exportLocalPath = "Assets/ExportedPackages/Package";

            string message = AssetStoreToolsPackagePipelineRunner.BuildExportCompletionMessage(
                "relative/path",
                exportLocalPath);

            Assert.That(message, Does.Contain($"path : {exportLocalPath}"));
            Assert.That(message, Does.Not.Contain("<a href="));
        }

        /// <summary> DisplayNameを持たない手順は型名を表示名にする。 </summary>
        [Test]
        public void DisplayName_NotOverridden_IsTypeName()
        {
            var step = new RecordingPlanStepStub();

            Assert.That(step.DisplayName, Is.EqualTo(nameof(RecordingPlanStepStub)));
        }

        private abstract class PlanStepStubBase : AssetStoreToolsPackageStepStrategy
        {
        }

        /// <summary> Plan段階で必ず例外を投げるテスト用の手順。 </summary>
        private sealed class ThrowingPlanStepStub : PlanStepStubBase
        {
            /// <inheritdoc />
            protected internal override void Plan(AssetStoreToolsPackagePlan plan)
                => throw new InvalidOperationException("テスト用の失敗");
        }

        /// <summary> Plan段階が呼ばれたことを記録するテスト用の手順。 </summary>
        private sealed class RecordingPlanStepStub : PlanStepStubBase
        {
            /// <summary> Plan段階が呼ばれたかを示す。 </summary>
            public bool IsPlanned { get; private set; }

            /// <inheritdoc />
            protected internal override void Plan(AssetStoreToolsPackagePlan plan) => IsPlanned = true;
        }

        /// <summary> 指定したアセットパスを持つ出力単位を生成する。 </summary>
        private static AssetStoreToolsPackagePlanEntry CreateEntry(params string[] assetPaths)
            => new("Assets/AssetStoreTools/Cri", "Cri", 0, assetPaths);

        /// <summary> 手順を持たない計画を生成する。 </summary>
        private static AssetStoreToolsPackagePlan CreatePlan(
            params AssetStoreToolsPackagePlanEntry[] entries)
            => CreatePlan(Array.Empty<AssetStoreToolsPackageStepStrategy>(), entries);

        /// <summary> 指定した手順と出力単位を持つ計画を生成する。 </summary>
        private static AssetStoreToolsPackagePlan CreatePlan(
            IReadOnlyList<AssetStoreToolsPackageStepStrategy> steps,
            params AssetStoreToolsPackagePlanEntry[] entries)
            => new("Test", steps, entries, new[] { ".asmdef" });
    }
}
