using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Assembly Definitionを自動生成する。
    /// </summary>
    public static class AssemblyGenerator
    {
        #region 外部向けAPI

        /// <summary>
        ///     指定パスに存在しないAssembly Definitionファイルを生成する。
        /// </summary>
        public static void GenerateAssembly(string path, AssemblyDefinitionData data)
        {
            string AsmdefPath = path + ".asmdef";

            // 既存の定義と利用者の設定を上書きしないため、ファイルが無い場合だけ生成する。
            if (!File.Exists(AsmdefPath))
            {
                string directoryPath = Path.GetDirectoryName(AsmdefPath);
                if (!Directory.Exists(directoryPath)) { Directory.CreateDirectory(directoryPath); }

                // 出力先を用意してから内容を確定し、Unityが参照を解決できるよう即時インポートする。
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(AsmdefPath, json);
                AssetDatabase.ImportAsset(AsmdefPath);
            }
        }

        /// <summary>
        ///     対象Assembly DefinitionへのGUID参照を重複なしで追加する。
        /// </summary>
        public static void AddAsssemblyReference(string mainAsmdefPath, string targetAsmdefPath)
        {
            const string GUID_PREFIX = "GUID:";

            // 参照元が無い状態では有効な定義を書き戻せないため、変更せず終了する。
            if (!File.Exists(mainAsmdefPath))
            {
                Debug.LogError("メインアセンブリが見つかりません。リファレンスの追加は行われません");
                return;
            }

            string mainAsmdefJson = File.ReadAllText(mainAsmdefPath);
            AssemblyDefinitionData mainAsmdef = JsonUtility.FromJson<AssemblyDefinitionData>(mainAsmdefJson);

            string enumAsmdefGUID = AssetDatabase.AssetPathToGUID(targetAsmdefPath);
            // AssetDatabaseが対象を認識していない場合は、空のGUIDを参照へ混入させない。
            if (string.IsNullOrEmpty(enumAsmdefGUID)) { return; }
            enumAsmdefGUID = GUID_PREFIX + enumAsmdefGUID;

            // Editor起動のたびに呼ばれるため、既に追加済みの参照は重複させない。
            if (mainAsmdef.references.Contains(enumAsmdefGUID)) { return; }

            List<string> referencesList = new(mainAsmdef.references) { enumAsmdefGUID };
            mainAsmdef.references = referencesList.ToArray();

            // PackageInitializerが自動生成enumへの参照を注入するため、この差分は手で戻さず永続化する。
            string updatedJson = JsonUtility.ToJson(mainAsmdef, true);
            File.WriteAllText(mainAsmdefPath, updatedJson);
            AssetDatabase.ImportAsset(mainAsmdefPath);
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     自動生成enum用Assembly Definitionを作成し、必要ならFrameworkから参照する。
        /// </summary>
        internal static void CreateEnumAssembly(string selfPath, string mainPath = "")
        {
            string selfAsmdefPath = selfPath + ".asmdef";

            // 生成物の配置先に専用Assembly Definitionが無い場合だけ作成する。
            GenerateAssembly(selfPath, new AssemblyDefinitionData(Path.GetFileName(selfPath)));

            // 参照元が指定された場合だけ、自動生成enumをFrameworkから利用可能にする。
            if (!string.IsNullOrEmpty(mainPath))
            {
                string targetAsmdefPath = mainPath + ".asmdef";
                AddAsssemblyReference(targetAsmdefPath, selfAsmdefPath);
            }
        }

        #endregion
    }
}
