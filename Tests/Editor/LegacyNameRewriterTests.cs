using System;
using System.Linq;

using NUnit.Framework;

using SymphonyFramework.Editor;

namespace SymphonyFramework.Tests
{
    /// <summary> 6.x の名前を 7.0.0 の名前へ書き換える規則を、文字列の入出力で検証する。 </summary>
    public sealed class LegacyNameRewriterTests
    {
        /// <summary> 改名した型は、型名を置き換えて新しい名前空間を using する。 </summary>
        [Test]
        public void Rewrite_RenamedType_ReplacesNameAndUsing()
        {
            const string source =
                "using SymphonyFrameWork.System.SceneBlock;\n"
                + "using UnityEngine;\n"
                + "\n"
                + "public sealed class Sample : MonoBehaviour\n"
                + "{\n"
                + "    [SerializeField] private SceneBlockAsset _block;\n"
                + "}\n";

            LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, CreateMap());

            Assert.That(result.IsChanged, Is.True);
            Assert.That(
                result.Text,
                Is.EqualTo(
                    "using SymphonyFramework.Scenes;\n"
                    + "using UnityEngine;\n"
                    + "\n"
                    + "public sealed class Sample : MonoBehaviour\n"
                    + "{\n"
                    + "    [SerializeField] private SuiteAsset _block;\n"
                    + "}\n"));
            Assert.That(result.AddedUsings, Is.EqualTo(new[] { "SymphonyFramework.Scenes" }));
            Assert.That(result.RemovedUsings, Is.EqualTo(new[] { "SymphonyFrameWork.System.SceneBlock" }));
        }

        /// <summary> 名前空間だけ移った型は、型名を変えずに using だけを差し替える。 </summary>
        [Test]
        public void Rewrite_NamespaceOnlyMove_AddsNewUsingAndRemovesOldUsing()
        {
            const string source =
                "using SymphonyFrameWork.Debugger.Logger;\n"
                + "class A { void M() { SymphonyDebugLogger.LogDirect(\"x\", LogKindEnum.Warning); } }\n";

            LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, CreateMap());

            Assert.That(
                result.Text,
                Is.EqualTo(
                    "using SymphonyFramework.Debugger.Logger;\n"
                    + "class A { void M() { SymphonyDebugLogger.LogDirect(\"x\", LogKindEnum.Warning); } }\n"));
        }

        /// <summary> 属性は Attribute を除いた短縮名でも書き換える。 </summary>
        [Test]
        public void Rewrite_AttributeShortName_Replaces()
        {
            const string source =
                "using SymphonyFrameWork.Attribute;\n"
                + "class A { [SubclassSelector] public object Value; [OldMarkerAttribute] public int Other; }\n";

            LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, CreateMap());

            Assert.That(
                result.Text,
                Is.EqualTo(
                    "using SymphonyFramework.Attribute;\n"
                    + "class A { [SubclassSelector] public object Value; [NewMarkerAttribute] public int Other; }\n"));
        }

        /// <summary> 完全修飾で書いた旧名は、新しい完全修飾名へ置き換える。 </summary>
        [Test]
        public void Rewrite_FullyQualifiedName_Replaces()
        {
            const string source =
                "class A { SymphonyFrameWork.System.SceneBlock.SceneBlockAsset _a; global::SymphonyFrameWork.System.SceneBlock.SceneBlockInfo _b; }\n";

            LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, CreateMap());

            Assert.That(
                result.Text,
                Is.EqualTo(
                    "class A { SymphonyFramework.Scenes.SuiteAsset _a; global::SymphonyFramework.Scenes.SuiteInfo _b; }\n"));
            Assert.That(result.AddedUsings, Is.Empty);
        }

        /// <summary> 転送はそのまま残し、使い続ける旧 using も残す。 </summary>
        [Test]
        public void Rewrite_ForwarderStillUsed_KeepsOldUsing()
        {
            const string source =
                "using SymphonyFrameWork.System.SceneBlock;\n"
                + "class A { SceneBlockAsset _a; void M() { SceneBlockLoader.LoadAsync(_a); } }\n";

            LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, CreateMap());

            Assert.That(
                result.Text,
                Is.EqualTo(
                    "using SymphonyFrameWork.System.SceneBlock;\n"
                    + "using SymphonyFramework.Scenes;\n"
                    + "class A { SuiteAsset _a; void M() { SceneBlockLoader.LoadAsync(_a); } }\n"));
            Assert.That(result.RemainingForwarders, Is.EqualTo(new[] { "SceneBlockLoader" }));
            Assert.That(result.RemovedUsings, Is.Empty);
        }

        /// <summary> 使い続ける転送と同じ名前の型を持つ名前空間は using せず、global:: 付きで書く。 </summary>
        [Test]
        public void Rewrite_SameNameAsForwarder_UsesGlobalQualifiedName()
        {
            const string source =
                "using SymphonyFrameWork.System.SaveSystem;\n"
                + "class Data : SaveDataContent { }\n"
                + "class A { void M() { foreach (SaveDataEntryInfo entry in SaveStore.GetEntries()) { } } }\n";

            LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, CreateMap());

            Assert.That(
                result.Text,
                Is.EqualTo(
                    "using SymphonyFrameWork.System.SaveSystem;\n"
                    + "class Data : SaveDataContent { }\n"
                    + "class A { void M() { foreach (global::SymphonyFramework.SaveData.SaveDataEntryInfo entry in SaveStore.GetEntries()) { } } }\n"));
            Assert.That(result.AddedUsings, Is.Empty);
        }

        /// <summary> 旧名前空間から移った拡張メソッドを呼んでいれば、新しい名前空間を using する。 </summary>
        [Test]
        public void Rewrite_ExtensionMethodNamespace_AddsUsing()
        {
            const string source =
                "using SymphonyFrameWork.Debugger.Logger;\n"
                + "class A { void M(UnityEngine.Component c) { c.LogAndCheckComponentNull(); } }\n";

            LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, CreateMap());

            Assert.That(result.AddedUsings, Is.EqualTo(new[] { "SymphonyFramework.Debugger.Logger" }));
            Assert.That(result.Text, Does.StartWith("using SymphonyFramework.Debugger.Logger;\n"));
            Assert.That(result.Text, Does.Not.Contain("SymphonyFrameWork"));
        }

        /// <summary> 旧名前空間の中で書いたコードは、using が無くても外側の旧名前空間の型を書き換える。 </summary>
        [Test]
        public void Rewrite_InsideLegacyNamespace_AddsUsingAtTop()
        {
            const string source =
                "namespace SymphonyFrameWork.System.SceneBlock.Samples\n"
                + "{\n"
                + "    class A { SceneBlockAsset _a; }\n"
                + "}\n";

            LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, CreateMap());

            Assert.That(
                result.Text,
                Is.EqualTo(
                    "using SymphonyFramework.Scenes;\n"
                    + "namespace SymphonyFrameWork.System.SceneBlock.Samples\n"
                    + "{\n"
                    + "    class A { SuiteAsset _a; }\n"
                    + "}\n"));
        }

        /// <summary> コメント、文字列、メンバーアクセス、宣言の名前は書き換えない。 </summary>
        [Test]
        public void Rewrite_CommentsStringsAndMemberAccess_AreUntouched()
        {
            const string source =
                "using SymphonyFrameWork.System.SceneBlock;\n"
                + "// SceneBlockAsset in a comment\n"
                + "/* SceneBlockAsset */\n"
                + "class A\n"
                + "{\n"
                + "    string s = \"SceneBlockAsset\";\n"
                + "    string v = @\"SceneBlockAsset \"\"quoted\"\"\";\n"
                + "    string r = \"\"\"SceneBlockAsset\"\"\";\n"
                + "    char c = '\\'';\n"
                + "    void M(Holder h) { h.SceneBlockAsset = null; }\n"
                + "}\n"
                + "class SceneBlockAsset { }\n";

            LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, CreateMap());

            // 旧型名は1か所も型として使われていないため、旧 using を消すだけになる。
            Assert.That(result.Text, Is.EqualTo(source.Substring(source.IndexOf('\n') + 1)));
            Assert.That(result.Replacements, Is.Empty);
        }

        /// <summary> 旧名が無ければ本文を変えない。 </summary>
        [Test]
        public void Rewrite_NoLegacyName_ReturnsUnchanged()
        {
            const string source = "using SymphonyFramework.Scenes;\nclass A { SuiteAsset _a; }\n";

            LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, CreateMap());

            Assert.That(result.IsChanged, Is.False);
            Assert.That(result.Text, Is.SameAs(source));
        }

        /// <summary> 足す using の改行は、元の本文の改行に合わせる。 </summary>
        [Test]
        public void Rewrite_PreservesLineEndings()
        {
            const string source =
                "using SymphonyFrameWork.System.SceneBlock;\r\n"
                + "using SymphonyFrameWork.Debugger.Logger;\r\n"
                + "class A { SceneBlockAsset _a; LogKindEnum _k; }\r\n";

            LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, CreateMap());

            Assert.That(
                result.Text,
                Is.EqualTo(
                    "using SymphonyFramework.Debugger.Logger;\r\n"
                    + "using SymphonyFramework.Scenes;\r\n"
                    + "class A { SuiteAsset _a; LogKindEnum _k; }\r\n"));
            Assert.That(result.Text.Replace("\r\n", string.Empty), Does.Not.Contain("\n"));
        }

        /// <summary> テスト用の対応表。実際の型から作らず、規則だけを検証する。 </summary>
        private static LegacyNameMap CreateMap()
        {
            LegacyNameMap.Entry[] entries =
            {
                new("SymphonyFrameWork.System.SceneBlock", "SceneBlockAsset", "SymphonyFramework.Scenes", "SuiteAsset"),
                new("SymphonyFrameWork.System.SceneBlock", "SceneBlockInfo", "SymphonyFramework.Scenes", "SuiteInfo"),
                new("SymphonyFrameWork.Debugger.Logger", "SymphonyDebugLogger", "SymphonyFramework.Debugger.Logger", "SymphonyDebugLogger"),
                new("SymphonyFrameWork.Debugger.Logger", "LogKindEnum", "SymphonyFramework.Debugger.Logger", "LogKindEnum"),
                new("SymphonyFrameWork.Attribute", "SubclassSelectorAttribute", "SymphonyFramework.Attribute", "SubclassSelectorAttribute"),
                new("SymphonyFrameWork.Attribute", "OldMarkerAttribute", "SymphonyFramework.Attribute", "NewMarkerAttribute"),
                new("SymphonyFrameWork.System.SaveSystem", "SaveDataEntryInfo", "SymphonyFramework.SaveData", "SaveDataEntryInfo"),
            };
            (string, string)[] forwarders =
            {
                ("SymphonyFrameWork.System.SceneBlock", "SceneBlockLoader"),
                ("SymphonyFrameWork.System.SaveSystem", "SaveStore"),
                ("SymphonyFrameWork.System.SaveSystem", "SaveDataContent"),
            };
            (string, string)[] publicTypes =
            {
                ("SymphonyFramework.Scenes", "SuiteAsset"),
                ("SymphonyFramework.Scenes", "Suite"),
                ("SymphonyFramework.SaveData", "SaveDataContent"),
                ("SymphonyFramework.SaveData", "SaveDataEntryInfo"),
                ("SymphonyFramework.Debugger.Logger", "SymphonyDebugLogger"),
            };
            LegacyNameMap.ExtensionEntry[] extensions =
            {
                new("SymphonyFrameWork.Debugger.Logger", "SymphonyFramework.Debugger.Logger", new[] { "LogAndCheckComponentNull" }),
            };

            return new LegacyNameMap(entries, forwarders, publicTypes, extensions);
        }
    }
}
