using SymphonyFrameWork.Editor;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     UXML参照のパス解決とGUID抽出を検証する。
    /// </summary>
    internal sealed class UxmlSourceReferenceTests
    {
        #region 外部向けAPI

        /// <summary>
        ///     project URIをAssetsから始まるパスへ解決する。
        /// </summary>
        [Test]
        public void Resolve_ProjectUri_ReturnsAssetPath()
        {
            bool result = UxmlSourceReference.Resolve("Assets/UI/Root.uxml",
                "project://database/Assets/UI/Main%20Style.uss?fileID=1&guid=abcd#Style",
                out string path, out string error);
            Assert.That(result, Is.True);
            Assert.That(path, Is.EqualTo("Assets/UI/Main Style.uss"));
            Assert.That(error, Is.Null);
        }

        /// <summary>
        ///     相対パスと区切りを参照元から正規化する。
        /// </summary>
        [TestCase("../Shared/A.uss", "Assets/Shared/A.uss")]
        [TestCase("./A.uxml", "Assets/UI/A.uxml")]
        [TestCase("..\\Shared\\A.uss", "Assets/Shared/A.uss")]
        [TestCase("Assets/A.uss", "Assets/A.uss")]
        public void Resolve_RelativeOrAssetPath_ReturnsNormalizedPath(string source, string expected)
        {
            Assert.That(UxmlSourceReference.Resolve("Assets/UI/Root.uxml", source,
                out string path, out string error), Is.True, error);
            Assert.That(path, Is.EqualTo(expected));
        }

        /// <summary>
        ///     対象外のパスをエラーとして返す。
        /// </summary>
        [TestCase("../../Outside.uxml")]
        [TestCase("../../../Outside.uxml")]
        [TestCase("project://database/Packages/example/A.uss")]
        [TestCase("project://database/Assets/../../Assets/A.uss")]
        [TestCase("https://example.com/A.uss")]
        [TestCase("C:/Other/A.uss")]
        [TestCase("/Other/A.uss")]
        [TestCase("Assets/A.png")]
        [TestCase("")]
        [TestCase(null)]
        public void Resolve_InvalidSource_ReturnsError(string source)
        {
            Assert.That(UxmlSourceReference.Resolve("Assets/UI/Root.uxml", source,
                out string path, out string error), Is.False);
            Assert.That(path, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        /// <summary>
        ///     キーの大文字小文字を問わずGUIDを取り出す。
        /// </summary>
        [TestCase("guid=abCD", "abCD")]
        [TestCase("GUID=ABcd", "ABcd")]
        [TestCase("guid=", "")]
        public void TryGetGuid_QueryContainsGuid_ReturnsValue(string query, string expected)
        {
            Assert.That(UxmlSourceReference.TryGetGuid("Assets/A.uss?fileID=1&" + query + "#Style",
                out string guid), Is.True);
            Assert.That(guid, Is.EqualTo(expected));
        }

        /// <summary>
        ///     パスやフラグメントにある文字列をGUIDと誤認しない。
        /// </summary>
        [TestCase("Assets/A.uss")]
        [TestCase("Assets/A.uss?fileID=1#guid=abcd")]
        [TestCase("Assets/guid=abcd.uss")]
        [TestCase(null)]
        public void TryGetGuid_NoGuidQuery_ReturnsFalse(string source)
        {
            Assert.That(UxmlSourceReference.TryGetGuid(source, out string guid), Is.False);
            Assert.That(guid, Is.Null);
        }

        #endregion
    }
}
