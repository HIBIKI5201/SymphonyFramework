using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using SymphonyFrameWork.Core;

namespace SymphonyFrameWork.Tests
{
    /// <summary> Frameworkが直接使用するUnity提供パッケージのmanifest宣言を検証する。 </summary>
    public sealed class PackageDependencyTests
    {
        /// <summary> 直接依存が検証環境と同じバージョンで宣言されている。 </summary>
        [Test]
        public void PackageManifest_DirectUnityDependencies_AreDeclaredAtTestedVersions()
        {
            IReadOnlyDictionary<string, string> expectedDependencies = new Dictionary<string, string>
            {
                ["com.unity.addressables"] = "2.9.1",
                ["com.unity.inputsystem"] = "1.18.0",
                ["com.unity.nuget.newtonsoft-json"] = "3.2.2",
                ["com.unity.test-framework"] = "1.6.0",
                ["com.unity.modules.audio"] = "1.0.0",
                ["com.unity.modules.imgui"] = "1.0.0",
                ["com.unity.modules.jsonserialize"] = "1.0.0",
                ["com.unity.modules.uielements"] = "1.0.0",
            };

            string manifestPath = Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "package.json");
            JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
            JObject dependencies = (JObject)manifest["dependencies"];

            Assert.That(dependencies, Is.Not.Null);
            foreach (KeyValuePair<string, string> expected in expectedDependencies)
            {
                Assert.That(
                    dependencies.Value<string>(expected.Key),
                    Is.EqualTo(expected.Value),
                    $"package.jsonへ直接依存'{expected.Key}'の検証版を明示してください。");
            }
        }
    }
}
