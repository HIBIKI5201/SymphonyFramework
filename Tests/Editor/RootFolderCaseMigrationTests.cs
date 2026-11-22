using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using SymphonyFramework.Editor;
using SymphonyFramework.SaveData;

using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Tests
{
    /// <summary> 6.x以前の表記の生成物を移す計画と、設定に保存される型の移行属性を検証する。 </summary>
    public sealed class RootFolderCaseMigrationTests
    {
        /// <summary> 旧表記のフォルダは、一時名を経由する2段階で現在の表記へ移す。 </summary>
        [Test]
        public void PlanFolderMoves_LegacyFolder_ReturnsTwoStepMove()
        {
            RootFolderCaseMigration.MigrationPlan plan = RootFolderCaseMigration.PlanFolderMoves(
                "Assets/Resources",
                new[] { "Other", "SymphonyFrameWork" });

            Assert.That(plan.HasConflict, Is.False);
            Assert.That(
                plan.Steps.Select(step => (step.From, step.To)).ToArray(),
                Is.EqualTo(new[]
                {
                    ("Assets/Resources/SymphonyFrameWork", "Assets/Resources/SymphonyFramework__case_migration"),
                    ("Assets/Resources/SymphonyFramework__case_migration", "Assets/Resources/SymphonyFramework"),
                }));
        }

        /// <summary> 旧表記のフォルダが無ければ何もしない。 </summary>
        [Test]
        public void PlanFolderMoves_NoLegacyFolder_ReturnsEmpty()
        {
            RootFolderCaseMigration.MigrationPlan plan = RootFolderCaseMigration.PlanFolderMoves(
                "Assets/Resources",
                new[] { "SymphonyFramework" });

            Assert.That(plan.HasConflict, Is.False);
            Assert.That(plan.Steps, Is.Empty);
        }

        /// <summary> 旧表記と現在の表記が別に存在する場合は、移さず衝突として返す。 </summary>
        [Test]
        public void PlanFolderMoves_BothCasesAsDistinctFolders_ReturnsConflict()
        {
            RootFolderCaseMigration.MigrationPlan plan = RootFolderCaseMigration.PlanFolderMoves(
                "Assets/Resources",
                new[] { "SymphonyFrameWork", "SymphonyFramework" });

            Assert.That(plan.HasConflict, Is.True);
            Assert.That(plan.Steps, Is.Empty);
            Assert.That(plan.ConflictLegacyPath, Is.EqualTo("Assets/Resources/SymphonyFrameWork"));
            Assert.That(plan.ConflictCurrentPath, Is.EqualTo("Assets/Resources/SymphonyFramework"));
        }

        /// <summary> 前回の移行が途中で止まり一時名だけが残っている場合は、現在の表記へ移し直す。 </summary>
        [Test]
        public void PlanFolderMoves_LeftoverTemporaryFolder_MovesToCurrentName()
        {
            RootFolderCaseMigration.MigrationPlan plan = RootFolderCaseMigration.PlanFolderMoves(
                "Assets/Resources",
                new[] { "SymphonyFramework__case_migration" });

            Assert.That(plan.HasConflict, Is.False);
            Assert.That(
                plan.Steps.Select(step => (step.From, step.To)).ToArray(),
                Is.EqualTo(new[]
                {
                    ("Assets/Resources/SymphonyFramework__case_migration", "Assets/Resources/SymphonyFramework"),
                }));
        }

        /// <summary> 一時名と旧表記が両方ある場合は、どちらが正か決められないため衝突として返す。 </summary>
        [Test]
        public void PlanFolderMoves_TemporaryAndLegacyFolders_ReturnsConflict()
        {
            RootFolderCaseMigration.MigrationPlan plan = RootFolderCaseMigration.PlanFolderMoves(
                "Assets/Resources",
                new[] { "SymphonyFrameWork", "SymphonyFramework__case_migration" });

            Assert.That(plan.HasConflict, Is.True);
            Assert.That(plan.Steps, Is.Empty);
            Assert.That(plan.ConflictLegacyPath, Is.EqualTo("Assets/Resources/SymphonyFrameWork"));
            Assert.That(plan.ConflictCurrentPath, Is.EqualTo("Assets/Resources/SymphonyFramework__case_migration"));
        }

        /// <summary> 生成asmdefのファイル名を、一時名を経由して現在の表記へ改名する。 </summary>
        [Test]
        public void PlanFileRenames_GeneratedAsmdef_RenamesToCurrentName()
        {
            RootFolderCaseMigration.MigrationPlan plan = RootFolderCaseMigration.PlanFileRenames(new[]
            {
                "Assets/Scripts/SymphonyFramework/SceneListEnum.cs",
                "Assets/Scripts/SymphonyFramework/SymphonyFrameWork.Enum.asmdef",
            });

            Assert.That(
                plan.Steps.Select(step => (step.From, step.To)).ToArray(),
                Is.EqualTo(new[]
                {
                    ("Assets/Scripts/SymphonyFramework/SymphonyFrameWork.Enum.asmdef",
                        "Assets/Scripts/SymphonyFramework/SymphonyFramework.Enum.asmdef__case_migration"),
                    ("Assets/Scripts/SymphonyFramework/SymphonyFramework.Enum.asmdef__case_migration",
                        "Assets/Scripts/SymphonyFramework/SymphonyFramework.Enum.asmdef"),
                }));
        }

        /// <summary> 生成物の本文の旧表記を現在の表記へ置き換える。 </summary>
        [Test]
        public void ReplaceLegacyName_GeneratedText_ReplacesAssemblyNameAndUsing()
        {
            const string text = "{ \"name\": \"SymphonyFrameWork.Enum\" }\nusing SymphonyFrameWork.System;";

            string replaced = RootFolderCaseMigration.ReplaceLegacyName(text);

            Assert.That(
                replaced,
                Is.EqualTo("{ \"name\": \"SymphonyFramework.Enum\" }\nusing SymphonyFramework.System;"));
        }

        /// <summary> 設定に保存されるローダーは、6.xの名前空間とアセンブリ名からの移行属性を持つ。 </summary>
        [TestCase(typeof(JsonUtilitySaveDataLoaderStrategy))]
        [TestCase(typeof(NewtonsoftSaveDataLoaderStrategy))]
        public void SaveDataLoaders_HaveMovedFromLegacyAssembly(Type loaderType)
        {
            CustomAttributeData attribute = loaderType
                .GetCustomAttributesData()
                .Single(data => data.AttributeType == typeof(MovedFromAttribute));
            IList<CustomAttributeTypedArgument> arguments = attribute.ConstructorArguments;

            Assert.That(arguments[1].Value, Is.EqualTo("SymphonyFrameWork.System.SaveSystem"));
            Assert.That(arguments[2].Value, Is.EqualTo("SymphonyFrameWork"));
        }

        /// <summary> パイプラインアセットに保存される手順は、6.xの名前空間とアセンブリ名からの移行属性を持つ。 </summary>
        [TestCase(typeof(AssetStoreToolsCombinePackageStrategy))]
        [TestCase(typeof(AssetStoreToolsCreateZipStrategy))]
        [TestCase(typeof(AssetStoreToolsSinglePackageStrategy))]
        [TestCase(typeof(AssetStoreToolsUsedDependenciesStrategy))]
        public void PackagePipelineStrategies_HaveMovedFromLegacyAssembly(Type strategyType)
        {
            CustomAttributeData attribute = strategyType
                .GetCustomAttributesData()
                .Single(data => data.AttributeType == typeof(MovedFromAttribute));
            IList<CustomAttributeTypedArgument> arguments = attribute.ConstructorArguments;

            Assert.That(arguments[1].Value, Is.EqualTo("SymphonyFrameWork.Editor"));
            Assert.That(arguments[2].Value, Is.EqualTo("SymphonyFrameWork.Editor"));
        }
    }
}
