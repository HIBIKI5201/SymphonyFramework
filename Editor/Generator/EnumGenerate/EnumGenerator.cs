using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using SymphonyFrameWork.Core;

using UnityEditor;
using UnityEngine;

using FileMode = System.IO.FileMode;
using Task = System.Threading.Tasks.Task;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     enumを自動生成する。
    /// </summary>
    public static class EnumGenerator
    {
        #region 外部向けAPI

        /// <summary>
        ///     有効な識別子を抽出し、通常またはフラグ形式のenumソースを生成する。
        /// </summary>
        public static void EnumGenerate(string[] strings, string fileName, bool flag = false)
        {
            EnumGenerate(strings, fileName, flag, true);
        }

        /// <summary>
        ///     指定名の自動生成enumファイルパスを取得する。
        /// </summary>
        public static string GetEnumFilePath(string fileName)
            => $"{EditorSymphonyConstant.ENUM_PATH}/{fileName}Enum.cs";

        #endregion

        #region 内部処理

        private static readonly Regex IdentifierRegex = new(@"^@?[a-zA-Z_][a-zA-Z0-9_]*$");
        private static readonly string[] ReservedWords = { "abstract", "as", "base", "bool", "break", "while" };

        /// <summary>
        ///     AssetDatabase更新の所有者を指定してenumソースを生成する。
        /// </summary>
        /// <param name="strings"> enumの列挙子候補。 </param>
        /// <param name="fileName"> 生成するenumの型名。 </param>
        /// <param name="flag"> Flags形式で生成する場合はtrue。 </param>
        /// <param name="refreshAssetDatabase"> 生成後にAssetDatabaseを更新する場合はtrue。 </param>
        internal static async void EnumGenerate(
            string[] strings,
            string fileName,
            bool flag,
            bool refreshAssetDatabase)
        {
            // 出力内容を作る前に候補を検証し、無効な識別子と重複を取り除く。
            HashSet<string> hash = new HashSet<string>(new string[1] { "None" }.Concat(strings))
                .Where(s =>
                {
                    // C#の識別子として使用できない候補は、生成後のコンパイルエラーを避けるため除外する。
                    if (!IdentifierRegex.IsMatch(s))
                    {
                        Debug.LogWarning($"無効な文字で始まっているか無効な文字が含まれているため'{s}'を除外しました");
                        return false;
                    }

                    // 予約語は既存の生成結果を変えず、問題のある候補として警告する。
                    // TODO(#161): 「除外しました」と警告しながらreturn trueで候補に残している。
                    //             シーン名やタグ名に予約語があると、生成した.csがコンパイルエラーになる。
                    //             除外するか@を前置してエスケープするかを決め、警告文を実際の挙動へ揃える。
                    if (ReservedWords.Contains(s))
                    {
                        Debug.LogWarning($"無効な文字列'{s}'を除外しました");
                    }

                    return true;
                })
                .ToHashSet();

            // 自動生成物は利用側のAssets/Scripts/SymphonyFrameWork配下へ置き、手編集を前提にしない。
            CreateResourcesFolder($"{EditorSymphonyConstant.ENUM_PATH}/");

            // 書き込み前に出力先と全ソース行を確定し、途中状態の内容を生成しない。
            string enumFilePath = GetEnumFilePath(fileName);
            IEnumerable<string> content = !flag
                ? NormalEnumGenerate(fileName, hash)
                : FlagEnumGenerate(fileName, hash);

            // 設定を正本として既存の生成物を上書きし、他プロセスが使用中の場合だけ再試行する。
            int maxRetries = 5;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (FileStream stream = new(enumFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (StreamWriter writer = new(stream, Encoding.UTF8))
                    {
                        foreach (string line in content)
                        {
                            await writer.WriteLineAsync(line);
                        }
                    }

                    // 書き込み完了後だけインポートし、必要な呼び出し経路ではAssetDatabase全体も更新する。
                    File.SetLastAccessTime(enumFilePath, DateTime.Now);
                    AssetDatabase.ImportAsset(enumFilePath, ImportAssetOptions.ForceUpdate);
                    if (refreshAssetDatabase) { AssetDatabase.Refresh(); }

                    Debug.Log($"{fileName}Enumを生成しました");
                    return;
                }
                catch (IOException e)
                {
                    // 最終試行でも書き込めない場合だけ例外を戻し、呼び出し側へ失敗を伝える。
                    if (attempt == maxRetries)
                    {
                        Debug.LogError($"ファイル書き込みに失敗しました（{enumFilePath}）：{e.Message}");
                        throw;
                    }

                    // Unityや外部エディタがファイルを解放する猶予を置いてから再試行する。
                    Debug.LogWarning($"ファイルが使用中のため再試行します（{attempt}/{maxRetries}）...");
                    await Task.Delay(500);
                }
            }
        }

        /// <summary>
        ///     リソースフォルダが無ければ生成する。
        /// </summary>
        /// <returns> フォルダまたはAssembly Definitionを生成・変更した場合はtrue。 </returns>
        private static bool CreateResourcesFolder(string resourcesPath)
        {
            bool hasAssetChanges = false;

            // 既存の利用側フォルダを上書きせず、出力先が無い場合だけ作成する。
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
                AssetDatabase.ImportAsset(resourcesPath, ImportAssetOptions.ForceUpdate);
                hasAssetChanges = true;
            }

            // Assembly Definitionの生成前後を比較し、実際にAssetDatabase更新が必要かを判定する。
            string enumAsmdefPath =
                EditorSymphonyConstant.ENUM_PATH + "/SymphonyFrameWork.Enum.asmdef";
            string mainAsmdefPath =
                EditorSymphonyConstant.FRAMEWORK_PATH + "/SymphonyFrameWork.asmdef";
            string previousEnumAsmdef = File.Exists(enumAsmdefPath)
                ? File.ReadAllText(enumAsmdefPath)
                : null;
            string previousMainAsmdef = File.Exists(mainAsmdefPath)
                ? File.ReadAllText(mainAsmdefPath)
                : null;

            // enum用asmdefを用意し、PackageInitializerと同じ参照注入を適用する。
            AssemblyGenerator.CreateEnumAssembly(
                EditorSymphonyConstant.ENUM_PATH + "/SymphonyFrameWork.Enum",
                EditorSymphonyConstant.FRAMEWORK_PATH + "/SymphonyFrameWork");

            hasAssetChanges |= !File.Exists(enumAsmdefPath) ||
                               previousEnumAsmdef != File.ReadAllText(enumAsmdefPath);
            hasAssetChanges |= File.Exists(mainAsmdefPath) &&
                               previousMainAsmdef != File.ReadAllText(mainAsmdefPath);
            return hasAssetChanges;
        }

        /// <summary>
        ///     通常のenumを生成する。
        /// </summary>
        /// <param name="fileName"> 生成するenumの型名。 </param>
        /// <param name="hash"> 重複除去済みの列挙子名。 </param>
        /// <returns> 通常enumを構成するソース行。 </returns>
        private static IEnumerable<string> NormalEnumGenerate(string fileName, HashSet<string> hash)
        {
            // 型宣言を先頭に置き、検証済みの候補を現在の列挙順で連結する。
            IEnumerable<string> content = new[]
            {
                "/// <summary>\n"
                + "///     Symphony Frameworkが自動生成した列挙型。\n"
                + "/// </summary>\n"
                + "public enum " + fileName + "Enum : int\n{"
            };

            content = content.Concat(hash.SelectMany((s, i) => new[]
            {
                $"    /// <summary> {s}を表す。 </summary>",
                $"    {s} = {i},"
            }));
            content = content.Append("}");

            return content;
        }

        /// <summary>
        ///     Flags属性付きenumのソース行を生成する。
        /// </summary>
        private static IEnumerable<string> FlagEnumGenerate(string fileName, HashSet<string> hash)
        {
            // Flags属性と型宣言を先頭に置き、候補をビット位置へ割り当てる。
            IEnumerable<string> content = new[]
            {
                "using System;\n\n"
                + "/// <summary>\n"
                + "///     Symphony Frameworkが自動生成したフラグ列挙型。\n"
                + "/// </summary>\n"
                + "[Flags]\npublic enum " + fileName + "Enum : int\n{"
            };

            content = content.Concat(hash.SelectMany((s, i) => new[]
            {
                $"    /// <summary> {s}を表す。 </summary>",
                $"    {s} = 1 << {i},"
            }));
            content = content.Append("}");

            return content;
        }

        /// <summary>
        ///     デバッグメニューからenum出力先とAssembly Definitionを生成する。
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + "Debug/" + nameof(CreateResourcesFolder), priority = 1000)]
        private static void CreateResourceFolderDebug()
        {
            bool hasAssetChanges = CreateResourcesFolder($"{EditorSymphonyConstant.ENUM_PATH}/");
            // フォルダまたはasmdefが変わった場合だけAssetDatabase全体を更新する。
            if (hasAssetChanges) { AssetDatabase.Refresh(); }
        }

        #endregion
    }
}
