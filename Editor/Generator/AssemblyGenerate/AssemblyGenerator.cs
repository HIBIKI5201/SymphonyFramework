using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     アセンブリを自動生成する
    /// </summary>
    public static class AssemblyGenerator
    {
        public static void GenerateAssembly(string path, AssemblyDefinitionData data)
        {
            string AsmdefPath = path + ".asmdef";

            if (!File.Exists(AsmdefPath))
            {
                string directoryPath = Path.GetDirectoryName(AsmdefPath);
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
                // アセンブリ定義ファイルを作成。
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(AsmdefPath, json);
                AssetDatabase.ImportAsset(AsmdefPath); // アセットデータベースにインポート。
            }
        }

        public static void AddAsssemblyReference(string mainAsmdefPath, string targetAsmdefPath)
        {
            const string GUID_PREFIX = "GUID:";

            if (!File.Exists(mainAsmdefPath))
            {
                Debug.LogError("メインアセンブリが見つかりません。リファレンスの追加は行われません");
            }

            string mainAsmdefJson = File.ReadAllText(mainAsmdefPath);
            AssemblyDefinitionData mainAsmdef = JsonUtility.FromJson<AssemblyDefinitionData>(mainAsmdefJson);

            string enumAsmdefGUID = AssetDatabase.AssetPathToGUID(targetAsmdefPath);
            if (string.IsNullOrEmpty(enumAsmdefGUID)) { return; }
            enumAsmdefGUID = GUID_PREFIX + enumAsmdefGUID;

            // 参照がすでに追加されていない場合にのみ追加。
            if (mainAsmdef.references.Contains(enumAsmdefGUID)) { return; }

            List<string> referencesList = new(mainAsmdef.references) { enumAsmdefGUID };
            mainAsmdef.references = referencesList.ToArray();

            // 変更を反映。
            string updatedJson = JsonUtility.ToJson(mainAsmdef, true);
            File.WriteAllText(mainAsmdefPath, updatedJson);
            AssetDatabase.ImportAsset(mainAsmdefPath); // アセットデータベースにインポート。
        }

        internal static void CreateEnumAssembly(string selfPath, string mainPath = "")
        {
            string selfAsmdefPath = selfPath + ".asmdef";

            GenerateAssembly(selfPath, new AssemblyDefinitionData(Path.GetFileName(selfPath)));

            // SymphonyFrameWork.asmdef に参照を追加
            if (string.IsNullOrEmpty(mainPath))
            {
                string targetAsmdefPath = mainPath + ".asmdef";
                AddAsssemblyReference(targetAsmdefPath, selfAsmdefPath);
            }
        }
    }
}