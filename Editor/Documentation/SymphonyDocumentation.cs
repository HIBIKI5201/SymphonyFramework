using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;

using UnityEditor;

using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Frameworkのドキュメントをブラウザで開く。
    /// </summary>
    /// <remarks> 同梱HTMLが見つからない場合はGitHub上の正本Markdownへ切り替える。 </remarks>
    public static class SymphonyDocumentation
    {
        #region 外部向けAPI

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
            // 未定義のページは誤ったURLを開かず、呼び出し側が特定できるエラーとして記録する。
            if (!SymphonyDocumentPathResolver.TryGetDocumentName(page, out _))
            {
                SymphonyDebugLogger.LogDirect(
                    $"[{SymphonyConstant.SYMPHONY_FRAMEWORK}] "
                    + $"ドキュメントページ '{page}' に対応する文書がありません。", LogKindEnum.Error);
                return;
            }

            // Package導入とAssets直置きのどちらでも、実際に検出したFrameworkルートから探す。
            string frameworkRoot = SymphonyConstant.GetFrameworkAbsolutePath();

            // オフラインでも参照できる同梱HTMLを優先し、存在する場合はそこで完了する。
            if (SymphonyDocumentPathResolver.TryResolveLocalPath(frameworkRoot, page, out string localPath))
            {
                Application.OpenURL(SymphonyDocumentPathResolver.ToFileUrl(localPath));
                return;
            }

            // 同梱HTMLを解決できない環境では、利用者が文書へ到達できるGitHubの正本へ切り替える。
            string fallbackUrl = SymphonyDocumentPathResolver.ResolveFallbackUrl(page);

            SymphonyDebugLogger.LogDirect(
                $"[{SymphonyConstant.SYMPHONY_FRAMEWORK}] "
                + $"同梱ドキュメントが見つかりません。GitHubの正本を開きます: {fallbackUrl}", LogKindEnum.Warning);

            Application.OpenURL(fallbackUrl);
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     ドキュメントの索引をブラウザで開く。
        /// </summary>
        [MenuItem(SymphonyConstant.WINDOW_MENU_PATH + MENU_NAME, priority = 1)]
        private static void OpenIndex()
        {
            Open(SymphonyDocumentPageEnum.Index);
        }

        #endregion
    }
}
