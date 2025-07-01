using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SymphonyFrameWork.Core;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     アセンブリを自動生成する
    /// </summary>
    public static class AssemblyGenerator
    {
        public static void CreateEnumAssembly(string selfPath, string mainPath = "")
        {
            var selfAsmdefPath = selfPath + ".asmdef";

            // SymphonyFrameWork.Enum.asmdef の生成
            if (!File.Exists(selfAsmdefPath))
            {
                var directoryPath = Path.GetDirectoryName(selfAsmdefPath);
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

                var enumAsmdef = new AssemblyDefinitionData("SymphonyFrameWork.Enum");

                // アセンブリ定義ファイルを作成
                var json = JsonUtility.ToJson(enumAsmdef, true);
                File.WriteAllText(selfAsmdefPath, json);
                AssetDatabase.ImportAsset(selfAsmdefPath); // アセットデータベースにインポート
            }

            // SymphonyFrameWork.asmdef に参照を追加
            if (string.IsNullOrEmpty(mainPath))
            {
                var targetAsmdefPath = mainPath + ".asmdef";
                AddAsssemblyReference(targetAsmdefPath, selfAsmdefPath);
            }
        }

        public static void AddAsssemblyReference(string mainAsmdefPath, string targetAsmdefPath)
        {
            const string guidPrefix = "GUID:";

            if (File.Exists(mainAsmdefPath))
            {
                var mainAsmdefJson = File.ReadAllText(mainAsmdefPath);
                var mainAsmdef = JsonUtility.FromJson<AssemblyDefinitionData>(mainAsmdefJson);

                var enumAsmdefGUID = guidPrefix + AssetDatabase.AssetPathToGUID(targetAsmdefPath);
                
                //ターゲットアセンブリのGUIDが取得できなければ終了
                if (enumAsmdefGUID == guidPrefix)
                    return;

                // 参照がすでに追加されていない場合にのみ追加
                if (!mainAsmdef.references.Contains(enumAsmdefGUID))
                {
                    var referencesList = new List<string>(mainAsmdef.references)
                    {
                        enumAsmdefGUID
                    };
                    mainAsmdef.references = referencesList.ToArray();

                    // 変更を反映
                    var updatedJson = JsonUtility.ToJson(mainAsmdef, true);
                    File.WriteAllText(mainAsmdefPath, updatedJson);
                    AssetDatabase.ImportAsset(mainAsmdefPath); // アセットデータベースにインポート
                }
            }
            else
            {
                Debug.LogError("メインアセンブリが見つかりません。リファレンスの追加は行われません");
            }
        }

        [Serializable]
        private class AssemblyDefinitionData
        {
            public string name = string.Empty;
            public string rootNamespace = string.Empty;
            public string[] references = new string[0];
            public string[] includePlatforms = new string[0];
            public string[] excludePlatforms = new string[0];
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public string[] precompiledReferences = new string[0];
            public bool autoReferenced = true;
            public string[] defineConstraints = new string[0];
            public string[] versionDefines = new string[0];
            public bool noEngineReferences;
            public string[] platforms = new string[0];

            public AssemblyDefinitionData(string name)
            {
                this.name = name;
            }
        }
    }
}