using SymphonyFrameWork.Core;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace SymphonyFrameWork.Editor
{
    /// <summary> Asset Store Tools Packagerのプロジェクト共有パス設定を保持する。 </summary>
    [FilePath(EditorSymphonyConstant.PROJCET_SETTING_FILE_PATH
        + nameof(AssetStoreToolsPackagerData) + ".asset",
        FilePathAttribute.Location.ProjectFolder)]
    public sealed class AssetStoreToolsPackagerData : ScriptableSingleton<AssetStoreToolsPackagerData>
    {
        /// <summary> パッケージ対象となるAsset Store Toolsフォルダのパス。 </summary>
        public static string AssetStoreToolsPath => instance._assetStoreToolsPath;

        /// <summary> 生成したパッケージを保存するフォルダのパス。 </summary>
        public static string ExportedPackagesPath => instance.exportedPackagesPath;

        /// <summary>
        ///     Packagerウィンドウで選べる出力パイプライン。
        /// </summary>
        /// <remarks>
        ///     アサインされていない場合は空。要素にnullが含まれることがある。
        ///     Project Settingsの配列で参照を外したままにできるため。
        /// </remarks>
        public static IReadOnlyList<AssetStoreToolsPackagePipeline> Pipelines => instance._pipelines;

        /// <summary> 出力パイプラインの一覧を保存する。 </summary>
        /// <param name="pipelines"> 保存する一覧。nullの場合は空として扱う。 </param>
        public static void SetPipelines(IReadOnlyList<AssetStoreToolsPackagePipeline> pipelines)
        {
            AssetStoreToolsPackagePipeline[] values = pipelines == null
                ? Array.Empty<AssetStoreToolsPackagePipeline>()
                : new AssetStoreToolsPackagePipeline[pipelines.Count];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = pipelines[i];
            }

            instance._pipelines = values;
            EditorUtility.SetDirty(instance);
            AssetDatabase.SaveAssets();
            Save();
        }

        /// <summary> パッケージ対象フォルダのパスを保存する。 </summary>
        public static void SetAssetStoreToolsPath(string path)
        {
            if (instance._assetStoreToolsPath != path)
            {
                instance._assetStoreToolsPath = path;
                EditorUtility.SetDirty(instance);
                AssetDatabase.SaveAssets();
                Save();
            }
        }
        /// <summary> パッケージ出力先フォルダのパスを保存する。 </summary>
        public static void SetExportedPackagesPath(string path)
        {
            if (instance.exportedPackagesPath != path)
            {
                instance.exportedPackagesPath = path;
                EditorUtility.SetDirty(instance);
                AssetDatabase.SaveAssets();
                Save();
            }
        }

        [SerializeField, Tooltip("生成したパッケージを保存するフォルダのパス。")]
        private string exportedPackagesPath = "ExportedPackages";

        [SerializeField, Tooltip("パッケージ対象となるAsset Store Toolsフォルダのパス。")]
        private string _assetStoreToolsPath = EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH;

        [SerializeField, Tooltip("Packagerウィンドウで選べる出力パイプライン。")]
        private AssetStoreToolsPackagePipeline[] _pipelines = Array.Empty<AssetStoreToolsPackagePipeline>();

        /// <summary> 現在の設定値をProjectSettingsへ保存する。 </summary>
        private static void Save() => instance.Save(true);
    }
}
