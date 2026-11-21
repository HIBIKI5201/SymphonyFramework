using System;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     Unity Assembly Definitionファイルへシリアライズする設定値を保持する。
    /// </summary>
    [Serializable]
    // 6.x の名前（SymphonyFrameWork.Editor）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Editor",
        sourceAssembly: "SymphonyFrameWork.Editor")]
    public sealed class AssemblyDefinitionData
    {
        #region 外部向けAPI

        /// <summary>
        ///     Assembly名を指定して既定の定義データを生成する。
        /// </summary>
        public AssemblyDefinitionData(string name)
        {
            this.name = name;
        }

        /// <summary> Assembly名。 </summary>
        public string name = string.Empty;

        /// <summary> 既定のルート名前空間。 </summary>
        public string rootNamespace = string.Empty;

        /// <summary> 参照するAssembly Definitionの名前またはGUID。 </summary>
        public string[] references = new string[0];

        /// <summary> Assemblyを含めるプラットフォーム。 </summary>
        public string[] includePlatforms = new string[0];

        /// <summary> Assemblyから除外するプラットフォーム。 </summary>
        public string[] excludePlatforms = new string[0];

        /// <summary> unsafeコードを許可するかを示す。 </summary>
        public bool allowUnsafeCode;

        /// <summary> 事前コンパイル済み参照を明示的に上書きするかを示す。 </summary>
        public bool overrideReferences;

        /// <summary> 参照する事前コンパイル済みAssembly。 </summary>
        public string[] precompiledReferences = new string[0];

        /// <summary> 他Assemblyから自動参照されるかを示す。 </summary>
        public bool autoReferenced = true;

        /// <summary> コンパイルに必要なdefine制約。 </summary>
        public string[] defineConstraints = new string[0];

        /// <summary> パッケージバージョンに応じて定義するシンボル。 </summary>
        public string[] versionDefines = new string[0];

        /// <summary> UnityEngineへの参照を無効にするかを示す。 </summary>
        public bool noEngineReferences;

        /// <summary> Assembly Definitionが使用するプラットフォーム設定。 </summary>
        public string[] platforms = new string[0];

        #endregion
    }
}
