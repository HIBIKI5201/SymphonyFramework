using System.Linq;

using NUnit.Framework;

using SymphonyFrameWork.Generators.Prototype;

using UnityEditor;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     Roslyn Source Generatorの生成結果と実行時分離を検証する。
    /// </summary>
    public sealed class RoslynGeneratorPrototypeTests
    {
        /// <summary> Generator DLLのUnityアセットパス。 </summary>
        private const string GENERATOR_ASSET_PATH =
            "Assets/SymphonyFrameWork/Generators/SymphonyFrameWork.Generators.dll";

        /// <summary> Generatorが出力する固定値。 </summary>
        private const string EXPECTED_MARKER = "Symphony Roslyn Generator Active";

        /// <summary>
        ///     Runtimeアセンブリ内の対象へ生成メンバーが追加される。
        /// </summary>
        [Test]
        public void GeneratedMarker_RuntimeTarget_ReturnsExpectedValue()
        {
            Assert.That(RoslynGeneratorPrototypeTarget.SymphonyGeneratedMarker, Is.EqualTo(EXPECTED_MARKER));
        }

        /// <summary>
        ///     Frameworkを参照するテストアセンブリ内の対象にも生成メンバーが追加される。
        /// </summary>
        [Test]
        public void GeneratedMarker_ReferencingAssemblyTarget_ReturnsExpectedValue()
        {
            Assert.That(RoslynGeneratorReferencingAssemblyTarget.SymphonyGeneratedMarker, Is.EqualTo(EXPECTED_MARKER));
        }

        /// <summary>
        ///     Generator DLLは実行時Pluginとしてどの対象にも含めない。
        /// </summary>
        [Test]
        public void GeneratorImporter_AllRuntimePlatforms_AreDisabled()
        {
            PluginImporter importer = AssetImporter.GetAtPath(GENERATOR_ASSET_PATH) as PluginImporter;

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.GetCompatibleWithAnyPlatform(), Is.False);
            Assert.That(importer.GetCompatibleWithEditor(), Is.False);
            Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64), Is.False);
            Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX), Is.False);
            Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.StandaloneLinux64), Is.False);
            Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.Android), Is.False);
            Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.iOS), Is.False);
        }

        /// <summary>
        ///     生成コードを含むRuntimeアセンブリはGenerator DLLを実行時参照しない。
        /// </summary>
        [Test]
        public void GeneratorAssembly_RuntimeTarget_DoesNotReferenceAnalyzer()
        {
            bool hasAnalyzerReference = typeof(RoslynGeneratorPrototypeTarget).Assembly
                .GetReferencedAssemblies()
                .Any(name => name.Name == "SymphonyFrameWork.Generators");

            Assert.That(hasAnalyzerReference, Is.False);
        }
    }

    /// <summary>
    ///     Framework参照アセンブリへAnalyzerが適用されることを検証する対象型。
    /// </summary>
    [GeneratePrototypeMarker]
    internal sealed partial class RoslynGeneratorReferencingAssemblyTarget
    {
    }
}
