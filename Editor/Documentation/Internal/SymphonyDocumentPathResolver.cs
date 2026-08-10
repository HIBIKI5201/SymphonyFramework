using System;
using System.Collections.Generic;
using System.IO;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     ドキュメントページから、同梱HTMLの実ファイルパスとGitHub上のURLを解決する。
    ///     Unity APIへ触れない純粋なロジックとして分離し、EditModeテストの対象にする。
    /// </summary>
    internal static class SymphonyDocumentPathResolver
    {
        /// <summary> Frameworkルートから見た、生成HTMLの基準ディレクトリ。 </summary>
        internal const string HTML_DIRECTORY = "Documentation~/Html";

        /// <summary> Frameworkルートから見た、正本Markdownの基準ディレクトリ。 </summary>
        internal const string MARKDOWN_DIRECTORY = "Documentation~";

        /// <summary> 同梱HTMLが無い場合に開くリポジトリのURL。 </summary>
        internal const string REPOSITORY_URL = "https://github.com/HIBIKI5201/SymphonyFramework";

        /// <summary>
        ///     フォールバック先のブランチ。
        ///     リポジトリにバージョンタグが存在しないため、導入バージョンでは固定できない。
        /// </summary>
        internal const string FALLBACK_BRANCH = "main";

        /// <summary>
        ///     ページと文書名の対応を返す。
        ///     値は<c>Documentation~/</c>からの相対パスで、拡張子を含まない。
        /// </summary>
        /// <param name="page"> 対象のドキュメントページ。 </param>
        /// <param name="documentName"> 対応する文書名。未定義の値ならnull。 </param>
        /// <returns> 対応する文書名がある場合はtrue。 </returns>
        internal static bool TryGetDocumentName(SymphonyDocumentPageEnum page, out string documentName) =>
            DOCUMENT_NAMES.TryGetValue(page, out documentName);

        /// <summary>
        ///     同梱HTMLの実ファイルパスを解決する。
        /// </summary>
        /// <param name="frameworkRoot"> Frameworkルートの絶対パス。 </param>
        /// <param name="page"> 対象のドキュメントページ。 </param>
        /// <param name="localPath"> 存在する場合はHTMLの絶対パス。存在しない場合はnull。 </param>
        /// <returns> HTMLが存在する場合はtrue。 </returns>
        internal static bool TryResolveLocalPath(
            string frameworkRoot,
            SymphonyDocumentPageEnum page,
            out string localPath)
        {
            localPath = null;

            if (string.IsNullOrEmpty(frameworkRoot))
            {
                return false;
            }

            if (!TryGetDocumentName(page, out string documentName))
            {
                return false;
            }

            string candidate = Path.GetFullPath(
                Path.Combine(frameworkRoot, HTML_DIRECTORY, documentName + ".html"));

            if (!File.Exists(candidate))
            {
                return false;
            }

            localPath = candidate;
            return true;
        }

        /// <summary>
        ///     同梱HTMLが無い場合に開く、GitHub上の正本MarkdownのURLを返す。
        /// </summary>
        /// <param name="page"> 対象のドキュメントページ。 </param>
        /// <returns> GitHubのURL。未定義の値ならリポジトリのトップ。 </returns>
        internal static string ResolveFallbackUrl(SymphonyDocumentPageEnum page)
        {
            if (page == SymphonyDocumentPageEnum.Index)
            {
                // 索引はHTML専用で正本のMarkdownが無いため、モジュール文書の一覧を開く。
                return $"{REPOSITORY_URL}/tree/{FALLBACK_BRANCH}/{MARKDOWN_DIRECTORY}/Modules";
            }

            if (!TryGetDocumentName(page, out string documentName))
            {
                return REPOSITORY_URL;
            }

            return $"{REPOSITORY_URL}/blob/{FALLBACK_BRANCH}/{MARKDOWN_DIRECTORY}/{documentName}.md";
        }

        /// <summary>
        ///     実ファイルパスを、ブラウザへ渡せる<c>file:</c>のURLへ変換する。
        /// </summary>
        /// <param name="absolutePath"> 変換する絶対パス。 </param>
        /// <returns> パーセントエンコード済みのURL。 </returns>
        /// <remarks>
        ///     パッケージの配置先にはスペースを含むことがある（<c>C:\Sinfonia Studio\...</c>など）。
        ///     文字列連結で組み立てるとブラウザがURLを解釈できないため、<see cref="Uri" />へ委ねる。
        /// </remarks>
        internal static string ToFileUrl(string absolutePath) =>
            new Uri(Path.GetFullPath(absolutePath)).AbsoluteUri;

        /// <summary>
        ///     ページと文書名の対応。
        ///     <see cref="SymphonyDocumentPageEnum" />へ値を足したらここへも追加する。
        ///     追加漏れは<c>SymphonyDocumentPathResolverTests</c>が検出する。
        /// </summary>
        private static readonly IReadOnlyDictionary<SymphonyDocumentPageEnum, string> DOCUMENT_NAMES =
            new Dictionary<SymphonyDocumentPageEnum, string>
            {
                [SymphonyDocumentPageEnum.Index] = "index",
                [SymphonyDocumentPageEnum.ServiceLocator] = "Modules/ServiceLocator",
                [SymphonyDocumentPageEnum.SceneLoader] = "Modules/SceneLoader",
                [SymphonyDocumentPageEnum.SaveDataSystem] = "Modules/SaveDataSystem",
                [SymphonyDocumentPageEnum.AudioManager] = "Modules/AudioManager",
                [SymphonyDocumentPageEnum.PauseManager] = "Modules/PauseManager",
                [SymphonyDocumentPageEnum.Debug] = "Modules/Debug",
                [SymphonyDocumentPageEnum.Utility] = "Modules/Utility",
                [SymphonyDocumentPageEnum.InspectorAttributes] = "Modules/InspectorAttributes",
                [SymphonyDocumentPageEnum.AutoEnumGenerator] = "Modules/AutoEnumGenerator",
                [SymphonyDocumentPageEnum.AssetStoreToolsPackager] = "Modules/AssetStoreToolsPackager",
                [SymphonyDocumentPageEnum.ProjectStructureTools] = "Modules/ProjectStructureTools",
                [SymphonyDocumentPageEnum.EditorTools] = "EditorTools",
            };
    }
}
