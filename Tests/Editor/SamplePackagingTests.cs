using System;
using System.IO;
using System.Linq;

using SymphonyFrameWork.Core;

using NUnit.Framework;

using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     サンプルがUnityのインポート対象外に置かれ、パッケージ本体のアセンブリへ入らない契約を検証する。
    /// </summary>
    /// <remarks>
    ///     チルダ無しの <c>Samples/</c> に置くと、asmdefが無い限りサンプルが
    ///     <c>SymphonyFrameWork</c> アセンブリへ取り込まれ、Package Managerからのインポートと
    ///     二重定義になる。利用側プロジェクトのコンパイルが通らなくなるため、配置自体を検査する。
    /// </remarks>
    public sealed class SamplePackagingTests
    {
        /// <summary> Unityのインポート対象外となるサンプルの置き場。 </summary>
        private const string SAMPLES_DIRECTORY_NAME = "Samples~";

        /// <summary> サンプルは末尾チルダのフォルダにだけ存在する。 </summary>
        [Test]
        public void SampleDirectory_PackageLayout_UsesTildeSuffixOnly()
        {
            Assert.That(
                Directory.Exists(Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, SAMPLES_DIRECTORY_NAME)),
                Is.True,
                $"'{SAMPLES_DIRECTORY_NAME}' が見つかりません。");

            Assert.That(
                Directory.Exists(Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "Samples")),
                Is.False,
                "チルダ無しの 'Samples' はUnityのインポート対象になり、"
                + "サンプルがSymphonyFrameWorkアセンブリへ取り込まれます。");
        }

        /// <summary> 末尾チルダのフォルダは.metaを持たない。 </summary>
        [Test]
        public void SampleDirectory_TildeSuffix_HasNoMetaFile()
        {
            Assert.That(
                File.Exists(Path.Combine(
                    EditorSymphonyConstant.FRAMEWORK_PATH, $"{SAMPLES_DIRECTORY_NAME}.meta")),
                Is.False);

            Assert.That(
                File.Exists(Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "Samples.meta")),
                Is.False);
        }

        /// <summary> package.jsonが宣言するサンプルのパスが、すべて実在する。 </summary>
        [Test]
        public void PackageManifest_SamplePaths_AllExist()
        {
            string manifestPath = Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "package.json");

            Assert.That(File.Exists(manifestPath), Is.True, $"'{manifestPath}' が見つかりません。");

            PackageManifest manifest = JsonUtility.FromJson<PackageManifest>(File.ReadAllText(manifestPath));

            Assert.That(manifest.samples, Is.Not.Null.And.Not.Empty, "samplesの宣言がありません。");

            foreach (SampleEntry sample in manifest.samples)
            {
                Assert.That(
                    sample.path,
                    Does.StartWith($"{SAMPLES_DIRECTORY_NAME}/"),
                    $"'{sample.displayName}' のpathが '{SAMPLES_DIRECTORY_NAME}/' で始まっていません。");

                Assert.That(
                    Directory.Exists(Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, sample.path)),
                    Is.True,
                    $"'{sample.displayName}' のpath '{sample.path}' が実在しません。");
            }
        }

        /// <summary> サンプルの型がパッケージ本体のアセンブリへ含まれていない。 </summary>
        [Test]
        public void FrameworkAssembly_SampleTypes_AreNotShipped()
        {
            Type[] shipped = typeof(SymphonyConstant).Assembly.GetTypes()
                .Concat(typeof(Debugger.Logger.SymphonyDebugLogger).Assembly.GetTypes())
                .Where(type => type.Namespace != null
                    && type.Namespace.StartsWith("SymphonyFrameWork.Samples", StringComparison.Ordinal))
                .ToArray();

            Assert.That(
                shipped,
                Is.Empty,
                "サンプルの型が出荷アセンブリに含まれています: "
                + string.Join(", ", shipped.Select(type => type.FullName)));
        }

        /// <summary> package.jsonのsamples配列を読むための入れ物。 </summary>
        [Serializable]
        private sealed class PackageManifest
        {
            /// <summary> Package Managerへ宣言しているサンプル。 </summary>
            public SampleEntry[] samples;
        }

        /// <summary> samples配列の1要素。 </summary>
        [Serializable]
        private sealed class SampleEntry
        {
            /// <summary> Package Managerに表示する名前。 </summary>
            public string displayName;

            /// <summary> パッケージルートからの相対パス。 </summary>
            public string path;
        }
    }
}
