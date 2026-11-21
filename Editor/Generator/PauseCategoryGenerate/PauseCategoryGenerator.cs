using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using SymphonyFramework.Core;
using SymphonyFramework.Debugger.Logger;

using UnityEditor;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     ポーズカテゴリーのinterfaceを利用側プロジェクトへ自動生成する。
    /// </summary>
    /// <remarks>
    ///     生成物は <c>Assets/Scripts/SymphonyFramework/PauseCategory/</c> 配下へ書き出す。
    ///     **専用のAssembly Definitionは作らない。** 自動生成enumのフォルダの下にあるため、
    ///     <c>SymphonyFramework.Enum</c> アセンブリに含まれる。そのアセンブリが
    ///     <c>SymphonyFramework</c> を参照するため、<c>IPausable</c> を継承できる。
    /// </remarks>
    // 6.x の名前（SymphonyFrameWork.Editor）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Editor",
        sourceAssembly: "SymphonyFrameWork.Editor")]
    public static class PauseCategoryGenerator
    {
        #region 外部向けAPI

        /// <summary> 生成先ディレクトリのパス。 </summary>
        public static string GeneratePath => EditorSymphonyConstant.ENUM_PATH + "/PauseCategory";

        /// <summary> 6.19.0以前に生成していた専用Assembly Definitionの名前。新しい順。 </summary>
        internal static readonly string[] LegacyAssemblyNames =
        {
            "SymphonyFramework.PauseCategory",
            "SymphonyFrameWork.PauseCategory",
        };

        /// <summary>
        ///     Project Settingsに設定されたカテゴリーのinterfaceを生成する。
        /// </summary>
        /// <remarks>
        ///     **設定から消したカテゴリーのファイルは削除しない。** 利用側のコードが
        ///     そのinterfaceを実装している場合、黙って消すとコンパイルが壊れる。
        ///     不要になったものは利用者が自分で削除する。残っている場合はログで知らせる。
        /// </remarks>
        public static void Generate()
        {
            IReadOnlyList<string> interfaceNames =
                ResolveInterfaceNames(PauseCategoryConfig.instance.CategoryNames);

            EnsureOutputLocation();

            foreach (string interfaceName in interfaceNames)
            {
                string path = GetFilePath(interfaceName);
                string source = BuildSource(interfaceName);

                // 内容が同じファイルを書き直すと、AssetDatabaseが毎回再インポートする。
                if (File.Exists(path) && File.ReadAllText(path) == source) { continue; }

                File.WriteAllText(path, source);
                AssetDatabase.ImportAsset(path);
            }

            ReportOrphanedFiles(interfaceNames);
            AssetDatabase.Refresh();
        }

        /// <summary>
        ///     6.19.0以前の専用Assembly Definitionを取り除き、生成物を自動生成enumのアセンブリへまとめる。
        /// </summary>
        /// <returns> 生成物を書き換えた場合はtrue。 </returns>
        /// <remarks>
        ///     **asmdefを消すだけでは足りない。** 旧生成物は <c>PauseManager.IPausable</c> を継承しているため、
        ///     フォルダ内の全ての生成物を新しい名前へ書き換えてからasmdefを消す。
        ///     設定から消えたカテゴリーのファイルも残っているため、設定から作り直すのではなく書き換える。
        /// </remarks>
        internal static bool MigrateLegacyAssembly()
        {
            if (!Directory.Exists(GeneratePath)) { return false; }

            List<string> legacyAsmdefPaths = new();
            foreach (string assemblyName in LegacyAssemblyNames)
            {
                string path = $"{GeneratePath}/{assemblyName}.asmdef";
                if (File.Exists(path)) { legacyAsmdefPaths.Add(path); }
            }

            if (legacyAsmdefPaths.Count == 0) { return false; }

            foreach (string path in Directory.GetFiles(GeneratePath, "*.cs"))
            {
                string source = File.ReadAllText(path);
                string migrated = RewriteLegacySource(source);
                if (source != migrated) { File.WriteAllText(path, migrated); }
            }

            foreach (string path in legacyAsmdefPaths) { AssetDatabase.DeleteAsset(path); }

            SymphonyDebugLogger.LogDirect(
                "ポーズカテゴリー専用のAssembly Definitionを削除し、SymphonyFramework.Enumへまとめました。"
                + " 自前のAssembly Definitionから参照していた場合は、SymphonyFramework.Enumの参照へ置き換えてください。",
                LogKindEnum.Warning);
            return true;
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     旧生成物のソースを新しい名前へ書き換える。
        /// </summary>
        /// <param name="source"> 旧生成物のソース。 </param>
        /// <returns> 書き換えたソース。対象が無ければそのまま返す。 </returns>
        internal static string RewriteLegacySource(string source)
        {
            return source
                .Replace("using SymphonyFrameWork.System;", "using SymphonyFramework.Pause;")
                .Replace("using SymphonyFramework.System;", "using SymphonyFramework.Pause;")
                .Replace("PauseManager.SetPause&lt;", "Fermata.SetPause&lt;")
                .Replace(": PauseManager.IPausable", ": IPausable");
        }

        /// <summary> C#の識別子として使える名前か。 </summary>
        private static readonly Regex IdentifierRegex = new(@"^[A-Za-z_][A-Za-z0-9_]*$");

        /// <summary>
        ///     カテゴリー名の一覧を、重複なしのinterface名の一覧へ解決する。
        /// </summary>
        /// <param name="categoryNames"> 設定されたカテゴリー名。 </param>
        /// <returns> 生成対象のinterface名。設定順を保つ。 </returns>
        /// <remarks>
        ///     識別子として使えない名前は、生成後のコンパイルエラーを避けるため除外する。
        ///     除外したことはログで知らせる。黙って消すと、設定したのに生成されない理由が分からない。
        /// </remarks>
        internal static IReadOnlyList<string> ResolveInterfaceNames(IEnumerable<string> categoryNames)
        {
            List<string> names = new();
            HashSet<string> seen = new(StringComparer.Ordinal);

            foreach (string categoryName in categoryNames ?? Array.Empty<string>())
            {
                string interfaceName = ResolveInterfaceName(categoryName);

                // 使えない候補と、既に同じinterface名へ解決済みの候補は生成対象へ含めない。
                if (interfaceName == null || !seen.Add(interfaceName)) { continue; }

                names.Add(interfaceName);
            }

            return names;
        }

        /// <summary>
        ///     カテゴリー名1件をinterface名へ解決する。
        /// </summary>
        /// <param name="categoryName"> 解決するカテゴリー名。 </param>
        /// <returns> interface名。生成対象にできない場合はnull。 </returns>
        /// <remarks>
        ///     **前後の空白だけは落とす。** それ以外は入力をそのまま使い、
        ///     <c>I</c> と <c>Pausable</c> で挟む。設定画面が生成される名前をそのまま表示するため、
        ///     推測して整形するより、入力と結果が一対一で対応するほうが分かりやすい。
        ///     生成名は必ず <c>I</c> で始まり <c>Pausable</c> で終わるため、予約語にはならない。
        /// </remarks>
        internal static string ResolveInterfaceName(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) { return null; }

            string trimmed = categoryName.Trim();
            if (!IdentifierRegex.IsMatch(trimmed))
            {
                SymphonyDebugLogger.LogDirect(
                    $"'{categoryName}'はC#の識別子として使えないため、ポーズカテゴリーの生成対象から除外しました",
                    LogKindEnum.Warning);
                return null;
            }

            return $"I{trimmed}Pausable";
        }

        /// <summary>
        ///     interface名から生成先のファイルパスを組み立てる。
        /// </summary>
        /// <param name="interfaceName"> 生成するinterface名。 </param>
        /// <returns> 生成先のファイルパス。 </returns>
        internal static string GetFilePath(string interfaceName) =>
            $"{GeneratePath}/{interfaceName}.cs";

        /// <summary>
        ///     interface1件のソースを組み立てる。
        /// </summary>
        /// <param name="interfaceName"> 生成するinterface名。 </param>
        /// <returns> 書き出すC#ソース。 </returns>
        /// <remarks>
        ///     自動生成enumと同じく名前空間を持たせない。利用側がusingなしで実装できる形にする。
        /// </remarks>
        internal static string BuildSource(string interfaceName)
        {
            return "using SymphonyFramework.Pause;\n"
                   + "\n"
                   + "/// <summary>\n"
                   + "///     Symphony Frameworkが自動生成したポーズカテゴリー。\n"
                   + "/// </summary>\n"
                   + "/// <remarks>\n"
                   + $"///     このinterfaceを実装した対象は、Fermata.SetPause&lt;{interfaceName}&gt;(true)\n"
                   + "///     で停止する。Project Settingsのカテゴリー設定から生成される。\n"
                   + "/// </remarks>\n"
                   + $"public interface {interfaceName} : IPausable\n"
                   + "{\n"
                   + "}\n";
        }

        /// <summary>
        ///     生成先のディレクトリと、生成物を収める自動生成enumのAssembly Definitionを用意する。
        /// </summary>
        private static void EnsureOutputLocation()
        {
            if (!Directory.Exists(GeneratePath))
            {
                Directory.CreateDirectory(GeneratePath);
                AssetDatabase.ImportAsset(GeneratePath, ImportAssetOptions.ForceUpdate);
            }

            // 生成物がIPausableを継承できるよう、自動生成enumのアセンブリからFrameworkを参照させる。
            AssemblyGenerator.CreateEnumAssembly(
                EditorSymphonyConstant.ENUM_PATH + "/SymphonyFramework.Enum",
                EditorSymphonyConstant.FRAMEWORK_PATH + "/SymphonyFramework");
        }

        /// <summary>
        ///     設定から消えたのに残っている生成物を知らせる。
        /// </summary>
        /// <param name="interfaceNames"> 今回生成した対象のinterface名。 </param>
        private static void ReportOrphanedFiles(IReadOnlyList<string> interfaceNames)
        {
            if (!Directory.Exists(GeneratePath)) { return; }

            HashSet<string> generated = new(interfaceNames, StringComparer.Ordinal);
            foreach (string path in Directory.GetFiles(GeneratePath, "*.cs"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (generated.Contains(name)) { continue; }

                SymphonyDebugLogger.LogDirect(
                    $"'{name}'は設定に無いポーズカテゴリーです。"
                    + " 実装しているコードが無いことを確かめてから削除してください。",
                    LogKindEnum.Warning);
            }
        }

        #endregion
    }
}
