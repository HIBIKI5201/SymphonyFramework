using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SymphonyFramework.Core;
using SymphonyFramework.Debugger.Logger;

using UnityEditor;
using UnityEditor.Compilation;

using UnityEngine;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     利用側スクリプトの 6.x の名前を、メニューから 7.0.0 の名前へ書き換える。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         書き換えの規則は <see cref="LegacyNameRewriter" /> にあり、ここはファイルの走査、確認のダイアログ、書き込みだけを持つ。
    ///         **ファイルを書き換えるのは、メニューから実行して確認したときだけにする。** 起動時は知らせるだけにとどめる。
    ///     </para>
    ///     <para>
    ///         対象は <c>Assets/</c> 以下の <c>.cs</c> で、パッケージ自身のフォルダは除く。
    ///         利用側のスクリプトにコンパイルエラーがあっても、このパッケージの Editor アセンブリは読み込まれるため実行できる。
    ///     </para>
    /// </remarks>
    internal static class LegacyNameMigration
    {
        #region 外部向けAPI

        /// <summary>
        ///     旧名を使うスクリプトがあれば、メニューの場所を警告ログで知らせる。1セッションに1回だけ行う。
        /// </summary>
        /// <remarks>
        ///     <see cref="SymphonyEditorOrchestrator" /> の初期化の最後に呼ぶ。
        ///     **失敗しても例外を投げず、初期化を止めない。** 知らせるだけの処理のためである。
        /// </remarks>
        internal static void NotifyIfNeeded()
        {
            if (SessionState.GetBool(NOTIFIED_SESSION_KEY, false)) { return; }

            SessionState.SetBool(NOTIFIED_SESSION_KEY, true);

            try
            {
                List<Candidate> candidates = FindCandidates(LegacyNameMap.CreateFromLoadedAssemblies());
                if (candidates.Count == 0) { return; }

                SymphonyDebugLogger.LogDirect(
                    $"[{nameof(LegacyNameMigration)}] 6.x の名前（SymphonyFrameWork）を使うスクリプトが {candidates.Count} 件あります。"
                    + $" メニュー「{MENU_PATH}」で 7.0.0 の名前へ書き換えられます。",
                    LogKindEnum.Warning);
            }
            catch (Exception exception)
            {
                SymphonyDebugLogger.LogDirect(
                    $"[{nameof(LegacyNameMigration)}] 6.x の名前を探せませんでした。"
                    + $" type: '{exception.GetType().FullName}', reason: '{exception.Message}'",
                    LogKindEnum.Warning);
            }
        }

        #endregion

        #region 内部処理

        /// <summary> メニュー項目の名前。 </summary>
        private const string MENU_NAME = "Migrate 6.x Names";

        /// <summary> メニュー項目のパス。 </summary>
        private const string MENU_PATH = SymphonyConstant.TOOL_MENU_PATH + MENU_NAME;

        /// <summary> 起動時に知らせたかを1セッションの間だけ覚えるキー。 </summary>
        private const string NOTIFIED_SESSION_KEY = "SymphonyFramework.LegacyNameMigration.Notified";

        /// <summary> このパッケージの Editor アセンブリ名。パッケージのフォルダを除くために使う。 </summary>
        private const string EDITOR_ASSEMBLY_NAME = "SymphonyFramework.Editor";

        /// <summary> UTF-8 の BOM。 </summary>
        private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };

        /// <summary>
        ///     書き換える候補1件。
        /// </summary>
        private sealed class Candidate
        {
            internal Candidate(string fullPath, string path, bool hasBom, LegacyNameRewriteResult result)
            {
                FullPath = fullPath;
                Path = path;
                HasBom = hasBom;
                Result = result;
            }

            /// <summary> 絶対パス。 </summary>
            internal string FullPath { get; }

            /// <summary> プロジェクトからの相対パス（<c>Assets/...</c>）。ログに使う。 </summary>
            internal string Path { get; }

            /// <summary> 元のファイルに BOM があったか。書き戻すときに保つ。 </summary>
            internal bool HasBom { get; }

            internal LegacyNameRewriteResult Result { get; }
        }

        /// <summary>
        ///     対象を走査し、確認のうえで書き換える。
        /// </summary>
        [MenuItem(MENU_PATH, priority = 100)]
        private static void MigrateFromMenu()
        {
            List<Candidate> candidates = FindCandidates(LegacyNameMap.CreateFromLoadedAssemblies());
            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog(MENU_NAME, "6.x の名前（SymphonyFrameWork）で書き換えられる箇所はありません。", "OK");
                return;
            }

            int replacementCount = candidates.Sum(candidate => candidate.Result.Replacements.Count);
            string[] forwarders = candidates
                .SelectMany(candidate => candidate.Result.RemainingForwarders)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            StringBuilder message = new();
            message.AppendLine($"{candidates.Count} ファイルの {replacementCount} 箇所を 7.0.0 の名前へ書き換えます。");
            if (forwarders.Length > 0)
            {
                message.AppendLine();
                message.AppendLine($"次の旧名は書き換えずに残します（[Obsolete] の警告が出ます）: {string.Join(", ", forwarders)}");
            }

            message.AppendLine();
            message.AppendLine("メンバー名の変更（BlockName → SuiteName など）は書き換えません。移行ガイド Migration7.md を見て直してください。");
            message.AppendLine("書き換える前に、バージョン管理へコミットしておくことをおすすめします。");

            if (!EditorUtility.DisplayDialog(MENU_NAME, message.ToString(), "書き換える", "キャンセル")) { return; }

            int writtenCount = 0;
            foreach (Candidate candidate in candidates)
            {
                if (TryWrite(candidate)) { writtenCount++; }
            }

            AssetDatabase.Refresh();
            SymphonyDebugLogger.LogDirect(
                $"[{nameof(LegacyNameMigration)}] {writtenCount} / {candidates.Count} ファイルを書き換えました。"
                + " 残るエラーは移行ガイド Migration7.md のメンバー名の対応を見て直してください。");
        }

        /// <summary>
        ///     書き換えが必要なスクリプトを探す。
        /// </summary>
        /// <param name="map"> 対応表。 </param>
        /// <returns> 書き換える候補。 </returns>
        private static List<Candidate> FindCandidates(LegacyNameMap map)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string packageRoot = GetPackageRoot(projectRoot);

            List<Candidate> candidates = new();
            foreach (string fullPath in Directory.EnumerateFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
            {
                if (packageRoot != null && IsUnder(fullPath, packageRoot)) { continue; }

                try
                {
                    byte[] bytes = File.ReadAllBytes(fullPath);
                    bool hasBom = HasUtf8Bom(bytes);
                    int offset = hasBom ? Utf8Bom.Length : 0;
                    string source = Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset);

                    LegacyNameRewriteResult result = LegacyNameRewriter.Rewrite(source, map);
                    if (!result.IsChanged) { continue; }

                    candidates.Add(new Candidate(fullPath, ToProjectPath(fullPath, projectRoot), hasBom, result));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    SymphonyDebugLogger.LogDirect(
                        $"[{nameof(LegacyNameMigration)}] '{fullPath}' を読めませんでした: {exception.Message}",
                        LogKindEnum.Warning);
                }
            }

            return candidates;
        }

        /// <summary>
        ///     1ファイルを書き戻し、内容をログへ出す。失敗しても次のファイルへ進む。
        /// </summary>
        /// <param name="candidate"> 書き換える候補。 </param>
        /// <returns> 書き込めた場合はtrue。 </returns>
        private static bool TryWrite(Candidate candidate)
        {
            try
            {
                byte[] body = Encoding.UTF8.GetBytes(candidate.Result.Text);
                byte[] bytes = candidate.HasBom ? Utf8Bom.Concat(body).ToArray() : body;
                File.WriteAllBytes(candidate.FullPath, bytes);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                SymphonyDebugLogger.LogDirect(
                    $"[{nameof(LegacyNameMigration)}] '{candidate.Path}' を書き換えられませんでした: {exception.Message}",
                    LogKindEnum.Warning);
                return false;
            }

            LegacyNameRewriteResult result = candidate.Result;
            StringBuilder log = new();
            log.Append($"[{nameof(LegacyNameMigration)}] '{candidate.Path}' を書き換えました。");
            log.Append($" 置き換え {result.Replacements.Count} 件");
            if (result.AddedUsings.Count > 0) { log.Append($"、using を追加: {string.Join(", ", result.AddedUsings)}"); }
            if (result.RemovedUsings.Count > 0) { log.Append($"、using を削除: {string.Join(", ", result.RemovedUsings)}"); }
            if (result.RemainingForwarders.Count > 0) { log.Append($"、残した旧名: {string.Join(", ", result.RemainingForwarders)}"); }
            foreach (string unresolved in result.Unresolved) { log.Append($"\n  {unresolved}"); }

            SymphonyDebugLogger.LogDirect(log.ToString(), result.Unresolved.Count > 0 ? LogKindEnum.Warning : LogKindEnum.Normal);
            return true;
        }

        /// <summary>
        ///     このパッケージのフォルダの絶対パスを取得する。Assets の外（Packages）にある場合も返すが、走査には関係しない。
        /// </summary>
        /// <param name="projectRoot"> プロジェクトのフォルダ。 </param>
        /// <returns> パッケージのフォルダ。見つからなければnull。 </returns>
        private static string GetPackageRoot(string projectRoot)
        {
            string asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(EDITOR_ASSEMBLY_NAME);
            if (string.IsNullOrEmpty(asmdefPath)) { return null; }

            // asmdef は <パッケージ>/Editor/ にある。
            string editorFolder = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(projectRoot, asmdefPath)));
            return Path.GetDirectoryName(editorFolder);
        }

        private static bool IsUnder(string path, string folder)
        {
            string normalizedFolder = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return Path.GetFullPath(path).StartsWith(normalizedFolder, StringComparison.OrdinalIgnoreCase);
        }

        private static string ToProjectPath(string fullPath, string projectRoot)
        {
            string relative = Path.GetFullPath(fullPath).Substring(projectRoot.Length).TrimStart('/', '\\');
            return relative.Replace('\\', '/');
        }

        private static bool HasUtf8Bom(byte[] bytes)
        {
            return bytes.Length >= Utf8Bom.Length
                && bytes[0] == Utf8Bom[0]
                && bytes[1] == Utf8Bom[1]
                && bytes[2] == Utf8Bom[2];
        }

        #endregion
    }
}
