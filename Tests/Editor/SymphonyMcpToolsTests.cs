using System;

using System.IO;

using SymphonyFrameWork.Editor.Debugger;

using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary> 未初期化時のSymphonyMcpToolsが有効なJSONを返すことを検証する。 </summary>
    public sealed class SymphonyMcpToolsTests
    {
        /// <summary> Service Locatorが未初期化でも例外なく有効なJSONを返すことを検証する。 </summary>
        [Test]
        public void GetServiceLocatorJson_WhenUninitialized_ReturnsValidJson()
        {
            AssertUninitializedJson(SymphonyMcpTools.GetServiceLocatorJson);
        }

        /// <summary> Scene Loaderが未初期化でも例外なく有効なJSONを返すことを検証する。 </summary>
        [Test]
        public void GetSceneLoaderJson_WhenUninitialized_ReturnsValidJson()
        {
            JObject result = AssertUninitializedJson(SymphonyMcpTools.GetSceneLoaderJson);

            Assert.That(result["scenes"], Is.Empty);
            Assert.That(result["activeSceneName"].Type, Is.EqualTo(JTokenType.Null));
        }

        /// <summary> Save Data Registryが未初期化でも例外なく有効なJSONを返すことを検証する。 </summary>
        [Test]
        public void GetSaveDataJson_WhenUninitialized_ReturnsValidJson()
        {
            AssertUninitializedJson(SymphonyMcpTools.GetSaveDataJson);
        }

        /// <summary> Pause Managerが未初期化でも例外なく有効なJSONを返すことを検証する。 </summary>
        [Test]
        public void GetPauseJson_WhenUninitialized_ReturnsValidJson()
        {
            AssertUninitializedJson(SymphonyMcpTools.GetPauseJson);
        }

        /// <summary>
        ///     ログファイルが無くても空の有効なJSONを返す。
        /// </summary>
        [Test]
        public void ReadLogFileJson_MissingFile_ReturnsEmptyResult()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Log.txt");

            JObject result = JObject.Parse(SymphonyMcpTools.ReadLogFileJson(path, 200));

            Assert.That(result.Value<bool>("exists"), Is.False);
            Assert.That(result.Value<int>("totalLineCount"), Is.Zero);
            Assert.That(result.Value<int>("returnedLineCount"), Is.Zero);
            Assert.That(result.Value<bool>("truncated"), Is.False);
            Assert.That(result["lines"], Is.Empty);
        }

        /// <summary>
        ///     上限を超えるログでは直近の行を元の順序で返す。
        /// </summary>
        [Test]
        public void ReadLogFileJson_MoreLinesThanLimit_ReturnsLatestLinesInOrder()
        {
            string directoryPath = CreateTemporaryDirectory();
            string path = Path.Combine(directoryPath, "Log.txt");

            try
            {
                File.WriteAllLines(path, new[] { "line1", "line2", "line3", "line4", "line5" });

                JObject result = JObject.Parse(SymphonyMcpTools.ReadLogFileJson(path, 3));

                Assert.That(result.Value<bool>("exists"), Is.True);
                Assert.That(result.Value<int>("totalLineCount"), Is.EqualTo(5));
                Assert.That(result.Value<int>("returnedLineCount"), Is.EqualTo(3));
                Assert.That(result.Value<bool>("truncated"), Is.True);
                Assert.That(
                    result["lines"].ToObject<string[]>(),
                    Is.EqualTo(new[] { "line3", "line4", "line5" }));
            }
            finally
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }

        /// <summary>
        ///     取得上限が範囲外でも例外を投げずエラーJSONを返す。
        /// </summary>
        [TestCase(0)]
        [TestCase(1001)]
        public void ReadLogFileJson_LimitOutsideRange_ReturnsErrorJson(int maxLines)
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Log.txt");
            string json = null;

            Assert.DoesNotThrow(() => json = SymphonyMcpTools.ReadLogFileJson(path, maxLines));

            JObject result = JObject.Parse(json);
            Assert.That(result.Value<string>("error"), Is.Not.Empty);
        }

        /// <summary>
        ///     引用符を含むログ行を壊さずJSONへ変換する。
        /// </summary>
        [Test]
        public void ReadLogFileJson_LogContainingQuotes_ReturnsValidJson()
        {
            string directoryPath = CreateTemporaryDirectory();
            string path = Path.Combine(directoryPath, "Log.txt");
            const string expectedLine = "message: \"quoted\"";

            try
            {
                File.WriteAllText(path, expectedLine);

                JObject result = null;
                Assert.DoesNotThrow(
                    () => result = JObject.Parse(SymphonyMcpTools.ReadLogFileJson(path, 1)));

                Assert.That(result["lines"].ToObject<string[]>(), Is.EqualTo(new[] { expectedLine }));
            }
            finally
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }

        /// <summary> 呼び出しが例外を投げず、未初期化を示すJSONを返すことを検証する。 </summary>
        /// <param name="getJson"> 検証対象のJSON取得処理。 </param>
        private static JObject AssertUninitializedJson(Func<string> getJson)
        {
            string json = null;
            Assert.DoesNotThrow(() => json = getJson());

            JObject result = null;
            Assert.DoesNotThrow(() => result = JObject.Parse(json));
            Assert.That(result.Value<bool>("initialized"), Is.False);
            return result;
        }

        /// <summary>
        ///     テスト専用の一時ディレクトリを作成する。
        /// </summary>
        /// <returns> 作成したディレクトリの絶対パス。 </returns>
        private static string CreateTemporaryDirectory()
        {
            string directoryPath = Path.Combine(
                Path.GetTempPath(),
                "SymphonyMcpTools_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }
    }
}
