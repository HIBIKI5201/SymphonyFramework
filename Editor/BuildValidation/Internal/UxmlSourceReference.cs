using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     UXMLの参照文字列をファイルやUnityへアクセスせず解析する。
    /// </summary>
    internal static class UxmlSourceReference
    {
        #region 外部向けAPI

        /// <summary>
        ///     参照元からAssets配下の依存パスを解決する。
        /// </summary>
        /// <param name="ownerPath"> Assetsから始まる参照元のパス。 </param>
        /// <param name="source"> TemplateまたはStyleのsrc。 </param>
        /// <param name="assetPath"> 正規化した依存パス。失敗時はnull。 </param>
        /// <param name="error"> 失敗理由。成功時はnull。 </param>
        /// <returns> Assets内のUXMLまたはUSSに解決できた場合はtrue。 </returns>
        internal static bool Resolve(string ownerPath, string source, out string assetPath, out string error)
        {
            assetPath = null;
            error = null;
            if (string.IsNullOrWhiteSpace(source))
            {
                error = "Template/Styleのsrcが未設定です。";
                return false;
            }

            // URIのクエリーとフラグメントはパスに含めない。
            string path = Uri.UnescapeDataString(source.Split('?', '#')[0]).Replace('\\', '/');
            if (path.StartsWith(PROJECT_URI_PREFIX, StringComparison.Ordinal))
            {
                path = path.Substring(PROJECT_URI_PREFIX.Length);
            }
            else if (!path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                if (path.StartsWith("/", StringComparison.Ordinal) || path.Contains(":"))
                {
                    error = $"プロジェクト外または未対応のUI依存です。src: {source}";
                    return false;
                }

                int separatorIndex = ownerPath?.LastIndexOf('/') ?? -1;
                if (separatorIndex < 0)
                {
                    error = $"参照元のアセットパスが不正です。Path: {ownerPath}";
                    return false;
                }

                path = ownerPath.Substring(0, separatorIndex + 1) + path;
            }

            // カレントディレクトリに依存せず、Assetsの外へ出る参照を拒否する。
            List<string> segments = new();
            foreach (string segment in path.Split('/'))
            {
                if (segment.Length == 0 || segment == ".") { continue; }
                if (segment == "..")
                {
                    if (segments.Count <= 1)
                    {
                        error = $"UI依存がAssetsの外を指しています。src: {source}";
                        return false;
                    }

                    segments.RemoveAt(segments.Count - 1);
                    continue;
                }

                if (segment.IndexOfAny(new[] { ':', '\0', '?', '#' }) >= 0)
                {
                    error = $"UI依存のパスが不正です。src: {source}";
                    return false;
                }

                segments.Add(segment);
            }

            string resolvedPath = string.Join("/", segments);
            if (!resolvedPath.StartsWith("Assets/", StringComparison.Ordinal)
                || (!resolvedPath.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase)
                    && !resolvedPath.EndsWith(".uss", StringComparison.OrdinalIgnoreCase)))
            {
                error = $"Assets内のUXML/USS以外を指すUI依存です。src: {source}";
                return false;
            }

            assetPath = resolvedPath;
            return true;
        }

        /// <summary>
        ///     srcのクエリーからGUIDを取り出す。
        /// </summary>
        /// <remarks> キーとGUIDの大文字小文字は区別しない。空のguidも検証対象として返す。 </remarks>
        internal static bool TryGetGuid(string source, out string guid)
        {
            guid = null;
            if (string.IsNullOrEmpty(source)) { return false; }
            int queryIndex = source.IndexOf('?');
            if (queryIndex < 0) { return false; }

            string query = source.Substring(queryIndex + 1).Split('#')[0];
            foreach (string part in query.Split('&'))
            {
                if (!part.StartsWith("guid=", StringComparison.OrdinalIgnoreCase)) { continue; }
                guid = Uri.UnescapeDataString(part.Substring("guid=".Length));
                return true;
            }

            return false;
        }

        #endregion

        #region 内部処理

        private const string PROJECT_URI_PREFIX = "project://database/";

        #endregion
    }
}
