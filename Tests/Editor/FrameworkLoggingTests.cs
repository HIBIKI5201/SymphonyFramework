using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using SymphonyFrameWork.Core;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     フレームワーク内のログが <c>SymphonyDebugLogger</c> 経由に統一されている契約を検証する。
    /// </summary>
    /// <remarks>
    ///     <c>UnityEngine.Debug</c> を直接呼ぶと、Editorの購読者が拾えず
    ///     <c>Cache/Log.txt</c> へ残らない。統一の維持は目視では続かないため、配置を機械的に見る。
    /// </remarks>
    public sealed class FrameworkLoggingTests
    {
        /// <summary>
        ///     ログの集約先そのものであり、Unity標準の出力を直接呼ぶことが役割であるファイル。
        /// </summary>
        private static readonly string[] LoggerImplementationFiles =
        {
            "Runtime/Debug/SymphonyDebugLogger.cs",
            "Core/Internal/CoreLogRelay.cs",
            // ログの出力先そのもの。書き込みに失敗したことは、その出力先へは残せない。
            "Editor/Debug/SymphonyDebugLogFileWriter.cs",
        };

        /// <summary> <c>Debug.Log</c> 系の直接呼び出し。 </summary>
        private static readonly Regex DirectDebugLogPattern =
            new(@"\bDebug\s*\.\s*Log(Warning|Error|Exception|Format)?\s*\(", RegexOptions.Compiled);

        /// <summary> コメント行は対象外。規約の説明としてAPI名が出るため。 </summary>
        private static readonly Regex CommentLinePattern = new(@"^\s*(//|/\*|\*)", RegexOptions.Compiled);

        /// <summary> パッケージ本体がUnity標準のログAPIを直接呼ばない。 </summary>
        [Test]
        public void FrameworkSource_AllAreas_DoesNotCallUnityDebugLogDirectly()
        {
            List<string> violations = new();

            foreach (string area in new[] { "Runtime", "Core", "Editor" })
            {
                violations.AddRange(FindDirectDebugLogCalls(area));
            }

            Assert.That(
                violations,
                Is.Empty,
                "ログは SymphonyDebugLogger 経由に統一します。"
                + " Unity標準のDebugを直接呼ぶと Cache/Log.txt へ残りません。"
                + $"\n{string.Join("\n", violations)}");
        }

        /// <summary>
        ///     指定した領域から <c>Debug.Log</c> 系の直接呼び出しを探す。
        /// </summary>
        /// <param name="area"> パッケージルートからの相対ディレクトリ。 </param>
        /// <returns> 違反箇所の一覧。 </returns>
        private static IEnumerable<string> FindDirectDebugLogCalls(string area)
        {
            string root = Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, area);

            Assert.That(Directory.Exists(root), Is.True, $"'{root}' が見つかりません。");

            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path
                    .GetRelativePath(EditorSymphonyConstant.FRAMEWORK_PATH, path)
                    .Replace("\\", "/");

                if (Array.IndexOf(LoggerImplementationFiles, relative) >= 0) { continue; }

                string[] lines = File.ReadAllLines(path);

                for (int index = 0; index < lines.Length; index++)
                {
                    if (CommentLinePattern.IsMatch(lines[index])) { continue; }
                    if (!DirectDebugLogPattern.IsMatch(lines[index])) { continue; }

                    yield return $"{relative}:{index + 1} {lines[index].Trim()}";
                }
            }
        }
    }
}
