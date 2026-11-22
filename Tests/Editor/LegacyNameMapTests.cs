using System.Linq;

using NUnit.Framework;

using SymphonyFramework.Editor;

namespace SymphonyFramework.Tests
{
    /// <summary> 読み込み済みのアセンブリから作る 6.x の名前の対応表を検証する。 </summary>
    public sealed class LegacyNameMapTests
    {
        /// <summary> 改名した型は、[MovedFrom] の旧名前空間と旧型名から引ける。 </summary>
        [Test]
        public void CreateFromLoadedAssemblies_RenamedType_IsMapped()
        {
            LegacyNameMap map = LegacyNameMap.CreateFromLoadedAssemblies();

            bool found = map.TryGetEntry("SymphonyFrameWork.System.SceneBlock", "SceneBlockAsset", out LegacyNameMap.Entry entry);

            Assert.That(found, Is.True);
            Assert.That(entry.NewNamespace, Is.EqualTo(typeof(SymphonyFramework.Scenes.SuiteAsset).Namespace));
            Assert.That(entry.NewName, Is.EqualTo(nameof(SymphonyFramework.Scenes.SuiteAsset)));
        }

        /// <summary> 属性は Attribute を除いた短縮名でも引ける。 </summary>
        [Test]
        public void CreateFromLoadedAssemblies_AttributeShortName_IsMapped()
        {
            LegacyNameMap map = LegacyNameMap.CreateFromLoadedAssemblies();

            bool found = map.TryGetEntry("SymphonyFrameWork.Attribute", "SubclassSelector", out LegacyNameMap.Entry entry);

            Assert.That(found, Is.True);
            Assert.That(entry.NewName, Is.EqualTo("SubclassSelector"));
            Assert.That(entry.NewNamespace, Is.EqualTo(typeof(SymphonyFramework.Attribute.SubclassSelectorAttribute).Namespace));
        }

        /// <summary> 旧名前空間に残した [Obsolete] の転送は、対応ではなく転送として持つ。 </summary>
        [Test]
        public void CreateFromLoadedAssemblies_Forwarder_IsNotRewriteTarget()
        {
            LegacyNameMap map = LegacyNameMap.CreateFromLoadedAssemblies();

            Assert.That(map.IsForwarder("SymphonyFrameWork.System.ServiceLocate", "ServiceLocator"), Is.True);
            Assert.That(map.TryGetEntry("SymphonyFrameWork.System.ServiceLocate", "ServiceLocator", out _), Is.False);
        }

        /// <summary> 旧名前空間から移った拡張メソッドを、新しい名前空間と合わせて持つ。 </summary>
        [Test]
        public void CreateFromLoadedAssemblies_ExtensionMethods_AreCollected()
        {
            LegacyNameMap map = LegacyNameMap.CreateFromLoadedAssemblies();

            Assert.That(
                map.GetExtensions("SymphonyFrameWork.Debugger.Logger"),
                Has.Some.Matches<LegacyNameMap.ExtensionEntry>(
                    extension => extension.MethodNames.Contains("LogAndCheckComponentNull")));
        }

        /// <summary> 6.x の名前空間の判定は、根の名前と、その下の名前空間だけを対象にする。 </summary>
        [TestCase("SymphonyFrameWork", true)]
        [TestCase("SymphonyFrameWork.System", true)]
        [TestCase("SymphonyFrameWorkExtra", false)]
        [TestCase("SymphonyFramework.Scenes", false)]
        [TestCase(null, false)]
        public void IsLegacyNamespace_ReturnsExpected(string typeNamespace, bool expected)
        {
            Assert.That(LegacyNameMap.IsLegacyNamespace(typeNamespace), Is.EqualTo(expected));
        }
    }
}
