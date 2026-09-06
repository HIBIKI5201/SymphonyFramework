using System;
using System.IO;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Editor;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary> ドキュメントページから同梱HTMLとGitHub URLを解決する処理を検証する。 </summary>
    public sealed class SymphonyDocumentPathResolverTests
    {
        /// <summary> 存在するHTMLは、Documentation~/Html配下の絶対パスとして返る。 </summary>
        [Test]
        public void TryResolveLocalPath_KnownPage_ReturnsHtmlUnderDocumentationFolder()
        {
            string root = CreateTemporaryRoot();

            try
            {
                string expected = Path.GetFullPath(
                    Path.Combine(root, "Documentation~/Html/Modules/SceneLoader.html"));
                Directory.CreateDirectory(Path.GetDirectoryName(expected));
                File.WriteAllText(expected, "<!DOCTYPE html>");

                bool resolved = SymphonyDocumentPathResolver.TryResolveLocalPath(
                    root,
                    SymphonyDocumentPageEnum.SceneLoader,
                    out string localPath);

                Assert.That(resolved, Is.True);
                Assert.That(Path.GetFullPath(localPath), Is.EqualTo(expected));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        /// <summary> HTMLが無ければfalseとnullを返し、例外にしない。 </summary>
        [Test]
        public void TryResolveLocalPath_MissingFile_ReturnsFalse()
        {
            string root = CreateTemporaryRoot();

            try
            {
                bool resolved = SymphonyDocumentPathResolver.TryResolveLocalPath(
                    root,
                    SymphonyDocumentPageEnum.SceneLoader,
                    out string localPath);

                Assert.That(resolved, Is.False);
                Assert.That(localPath, Is.Null);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        /// <summary> Frameworkルートを解決できない場合もfalseを返す。 </summary>
        [Test]
        public void TryResolveLocalPath_EmptyRoot_ReturnsFalse()
        {
            bool resolved = SymphonyDocumentPathResolver.TryResolveLocalPath(
                string.Empty,
                SymphonyDocumentPageEnum.SceneLoader,
                out string localPath);

            Assert.That(resolved, Is.False);
            Assert.That(localPath, Is.Null);
        }

        /// <summary> フォールバック先は、mainブランチ上の正本Markdownを指す。 </summary>
        [Test]
        public void ResolveFallbackUrl_KnownPage_PointsToMarkdownOnMain()
        {
            string url = SymphonyDocumentPathResolver.ResolveFallbackUrl(
                SymphonyDocumentPageEnum.SceneLoader);

            Assert.That(
                url,
                Is.EqualTo(
                    SymphonyDocumentPathResolver.REPOSITORY_URL
                    + "/blob/main/Documentation~/Modules/SceneLoader.md"));
        }

        /// <summary> 索引は、mainブランチ上の正本Markdownを指す。 </summary>
        [Test]
        public void ResolveFallbackUrl_IndexPage_PointsToIndexMarkdownOnMain()
        {
            string url = SymphonyDocumentPathResolver.ResolveFallbackUrl(
                SymphonyDocumentPageEnum.Index);

            Assert.That(
                url,
                Is.EqualTo(
                    SymphonyDocumentPathResolver.REPOSITORY_URL
                    + "/blob/main/Documentation~/index.md"));
        }

        /// <summary>
        ///     スペースを含むパスはパーセントエンコードされる。
        ///     文字列連結でURLを組むとブラウザが解釈できないため、この変換を経路として固定する。
        /// </summary>
        [Test]
        public void ToFileUrl_PathWithSpace_IsPercentEncoded()
        {
            string url = SymphonyDocumentPathResolver.ToFileUrl(
                Path.Combine(Path.GetTempPath(), "Sinfonia Studio", "index.html"));

            Assert.That(url, Does.StartWith("file:///"));
            Assert.That(url, Does.Contain("Sinfonia%20Studio"));
            Assert.That(url, Does.Not.Contain(" "));
        }

        /// <summary> enumへ値を足して対応表への追加を忘れた場合に落とす。 </summary>
        [Test]
        public void AllPages_HaveDocumentFileName()
        {
            foreach (SymphonyDocumentPageEnum page in
                     Enum.GetValues(typeof(SymphonyDocumentPageEnum)))
            {
                Assert.That(
                    SymphonyDocumentPathResolver.TryGetDocumentName(page, out string documentName),
                    Is.True,
                    $"{page} に対応する文書名がありません。");
                Assert.That(documentName, Is.Not.Empty);
            }
        }

        /// <summary>
        ///     全ページの同梱HTMLが実際に存在する。
        ///     文書を足してHTMLの再生成を忘れた場合、この検証で落ちる。
        /// </summary>
        [Test]
        public void AllPages_HaveGeneratedHtmlInPackage()
        {
            string frameworkRoot = SymphonyConstant.GetFrameworkAbsolutePath();

            foreach (SymphonyDocumentPageEnum page in
                     Enum.GetValues(typeof(SymphonyDocumentPageEnum)))
            {
                Assert.That(
                    SymphonyDocumentPathResolver.TryResolveLocalPath(frameworkRoot, page, out _),
                    Is.True,
                    $"{page} の同梱HTMLが見つかりません。"
                    + " python scripts/build_module_docs.py を実行してください。");
            }
        }

        /// <summary> テスト専用の一時ディレクトリを作る。 </summary>
        /// <returns> 作成したディレクトリの絶対パス。 </returns>
        private static string CreateTemporaryRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "SymphonyDocs_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
