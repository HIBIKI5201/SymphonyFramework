using SymphonyFrameWork.Core;

using UnityEditor;

using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Frameworkのドキュメントをブラウザで開く。
    ///     パッケージへ同梱したHTMLを開き、見つからない場合はGitHub上の正本Markdownへ切り替える。
    /// </summary>
    public static class SymphonyDocumentation
    {
        /// <summary> ドキュメント表示のメニュー名。 </summary>
        public const string MENU_NAME = "Documentation";

        /// <summary>
        ///     指定したページをブラウザで開く。
        /// </summary>
        /// <param name="page"> 開くドキュメントページ。 </param>
        /// <remarks>
        ///     例外を投げない。Editorの補助機能であり、ドキュメントを開けないことで
        ///     利用側の作業を止めないため、失敗はConsoleへのログで通知する。
        /// </remarks>
        public static void Open(SymphonyDocumentPageEnum page)
        {
            if (!SymphonyDocumentPathResolver.TryGetDocumentName(page, out _))
            {
                Debug.LogError(
                    $"[{SymphonyConstant.SYMPHONY_FRAMEWORK}] "
                    + $"ドキュメントページ '{page}' に対応する文書がありません。");
                return;
            }

            string frameworkRoot = SymphonyConstant.GetFrameworkAbsolutePath();

            if (SymphonyDocumentPathResolver.TryResolveLocalPath(frameworkRoot, page, out string localPath))
            {
                Application.OpenURL(SymphonyDocumentPathResolver.ToFileUrl(localPath));
                return;
            }

            string fallbackUrl = SymphonyDocumentPathResolver.ResolveFallbackUrl(page);

            Debug.LogWarning(
                $"[{SymphonyConstant.SYMPHONY_FRAMEWORK}] "
                + $"同梱ドキュメントが見つかりません。GitHubの正本を開きます: {fallbackUrl}");

            Application.OpenURL(fallbackUrl);
        }

        /// <summary>
        ///     ドキュメントの索引をブラウザで開く。
        /// </summary>
        [MenuItem(SymphonyConstant.WINDOW_MENU_PATH + MENU_NAME, priority = 1)]
        private static void OpenIndex()
        {
            Open(SymphonyDocumentPageEnum.Index);
        }
    }
}
