using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;

using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     ポーズカテゴリーのinterfaceを利用側プロジェクトへ自動生成する。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         生成物は <c>Assets/Scripts/SymphonyFrameWork/PauseCategory/</c> 配下へ書き出し、
    ///         専用のAssembly Definitionから <c>SymphonyFrameWork</c> を参照する。
    ///     </para>
    ///     <para>
    ///         **自動生成enumと同じ場所へは置けない。** カテゴリーは
    ///         <c>PauseManager.IPausable</c> を継承するため <c>SymphonyFrameWork</c> の参照が要るが、
    ///         <c>SymphonyFrameWork</c> 側が <c>SymphonyFrameWork.Enum</c> を参照しているため循環する。
    ///     </para>
    /// </remarks>
    public static class PauseCategoryGenerator
    {
        #region 外部向けAPI

        /// <summary> 生成先ディレクトリのパス。 </summary>
        public static string GeneratePath => EditorSymphonyConstant.ENUM_PATH + "/PauseCategory";

        /// <summary> 生成物を収めるAssembly Definitionの名前。 </summary>
        public const string ASSEMBLY_NAME = "SymphonyFrameWork.PauseCategory";

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

            CreateAssembly();

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

        #endregion

        #region 内部処理

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
            return "using SymphonyFrameWork.System;\n"
                   + "\n"
                   + "/// <summary>\n"
                   + "///     Symphony Frameworkが自動生成したポーズカテゴリー。\n"
                   + "/// </summary>\n"
                   + "/// <remarks>\n"
                   + $"///     このinterfaceを実装した対象は、PauseManager.SetPause&lt;{interfaceName}&gt;(true)\n"
                   + "///     で停止する。Project Settingsのカテゴリー設定から生成される。\n"
                   + "/// </remarks>\n"
                   + $"public interface {interfaceName} : PauseManager.IPausable\n"
                   + "{\n"
                   + "}\n";
        }

        /// <summary>
        ///     生成先のディレクトリとAssembly Definitionを用意する。
        /// </summary>
        /// <remarks>
        ///     **参照の向きは生成物からフレームワークへの片方向である。**
        ///     フレームワーク側はこのアセンブリを参照しない。
        /// </remarks>
        private static void CreateAssembly()
        {
            if (!Directory.Exists(GeneratePath))
            {
                Directory.CreateDirectory(GeneratePath);
                AssetDatabase.ImportAsset(GeneratePath, ImportAssetOptions.ForceUpdate);
            }

            string selfPath = $"{GeneratePath}/{ASSEMBLY_NAME}";
            AssemblyGenerator.GenerateAssembly(selfPath, new AssemblyDefinitionData(ASSEMBLY_NAME));

            // 生成物がPauseManager.IPausableを継承できるよう、フレームワークへの参照を入れる。
            AssemblyGenerator.AddAsssemblyReference(
                $"{selfPath}.asmdef",
                $"{EditorSymphonyConstant.FRAMEWORK_PATH}/SymphonyFrameWork.asmdef");
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
