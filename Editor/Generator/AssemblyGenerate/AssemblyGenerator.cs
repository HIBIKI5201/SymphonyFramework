using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SymphonyFramework.Debugger.Logger;

using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     Assembly Definitionを自動生成する。
    /// </summary>
    // 6.x の名前（SymphonyFrameWork.Editor）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Editor",
        sourceAssembly: "SymphonyFrameWork.Editor")]
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
                SymphonyDebugLogger.LogDirect("メインアセンブリが見つかりません。リファレンスの追加は行われません", LogKindEnum.Error);
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

            // 利用側の生成物のasmdefへ書き込むため、この差分は手で戻さず永続化する。
            string updatedJson = JsonUtility.ToJson(mainAsmdef, true);
            File.WriteAllText(mainAsmdefPath, updatedJson);
            AssetDatabase.ImportAsset(mainAsmdefPath);
        }

        /// <summary>
        ///     対象Assembly DefinitionへのGUID参照を取り除く。
        /// </summary>
        /// <param name="mainAsmdefPath"> 参照を持つAssembly Definitionのパス。 </param>
        /// <param name="targetAsmdefPath"> 参照先のAssembly Definitionのパス。 </param>
        /// <returns> 参照を取り除いた場合はtrue。 </returns>
        public static bool RemoveAssemblyReference(string mainAsmdefPath, string targetAsmdefPath)
        {
            if (!File.Exists(mainAsmdefPath)) { return false; }

            string targetGuid = AssetDatabase.AssetPathToGUID(targetAsmdefPath);
            if (string.IsNullOrEmpty(targetGuid)) { return false; }

            AssemblyDefinitionData mainAsmdef =
                JsonUtility.FromJson<AssemblyDefinitionData>(File.ReadAllText(mainAsmdefPath));
            string reference = "GUID:" + targetGuid;

            // 参照が無ければファイルを書き直さず、不要な再インポートを起こさない。
            if (!mainAsmdef.references.Contains(reference)) { return false; }

            mainAsmdef.references = mainAsmdef.references.Where(item => item != reference).ToArray();
            File.WriteAllText(mainAsmdefPath, JsonUtility.ToJson(mainAsmdef, true));
            AssetDatabase.ImportAsset(mainAsmdefPath);
            return true;
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     自動生成enum用Assembly Definitionを作成し、Frameworkへの参照を入れる。
        /// </summary>
        /// <param name="selfPath"> 拡張子を除いた自動生成enum用asmdefのパス。 </param>
        /// <param name="frameworkPath"> 拡張子を除いたFramework本体のasmdefのパス。 </param>
        /// <remarks>
        ///     **参照の向きは生成物からFrameworkへの片方向である。** 生成物にはポーズカテゴリーの
        ///     interfaceも入り、それが<c>IPausable</c>を継承するため。Framework側は生成物を参照しない。
        /// </remarks>
        internal static void CreateEnumAssembly(string selfPath, string frameworkPath)
        {
            string selfAsmdefPath = selfPath + ".asmdef";

            // 生成物の配置先に専用Assembly Definitionが無い場合だけ作成する。
            GenerateAssembly(selfPath, new AssemblyDefinitionData(Path.GetFileName(selfPath)));

            AddAsssemblyReference(selfAsmdefPath, frameworkPath + ".asmdef");
        }

        #endregion
    }
}
