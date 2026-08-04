using System;
using System.Runtime.CompilerServices;

namespace SymphonyFrameWork.Core
{
    /// <summary>
    ///     エディタ用の定数値を持つ。
    /// </summary>
    public static class EditorSymphonyConstant
    {
        /// <summary>
        ///     パッケージか開発中か判定する。
        /// </summary>
        /// <param name="sourceFilePath"> 呼び出し元ソースファイルの自動取得パス。 </param>
        /// <returns> Package配下から呼び出された場合はtrue。 </returns>
        public static bool IsPackage([CallerFilePath] string sourceFilePath = "") =>
            !sourceFilePath.Replace('\\', '/').Contains("/Assets/");

        /// <summary>
        /// アセットかパッケージのルートパスを返す。
        /// </summary>
        /// <returns> Package導入時またはAssets直置き時のFrameworkルートパス。 </returns>
        public static string FRAMEWORK_PATH
        {
            get
            {
                if (IsPackage())
                {
                    return "Packages/" + SymphonyConstant.SYMPHONY_PACKAGE;
                }
                else
                {
                    return "Assets/" + SymphonyConstant.SYMPHONY_FRAMEWORK;
                }

            }
        }

        #region 自動生成物のパス
        /// <summary> Editor用設定アセットの出力ディレクトリ。 </summary>
        public static string RESOURCES_EDITOR_PATH = "Assets/Editor/" + SymphonyConstant.SYMPHONY_FRAMEWORK + "/Configs";

        /// <summary> 自動生成enumの出力ディレクトリ。 </summary>
        public static string ENUM_PATH = "Assets/Scripts/" + SymphonyConstant.SYMPHONY_FRAMEWORK;

        /// <summary> Asset Store Toolsの既定ルートパス。 </summary>
        public const string ASSET_STORE_TOOLS_PATH = "Assets/AssetStoreTools";

        /// <summary> パッケージ対象から除外するフォルダ名の設定ファイル。 </summary>
        [Obsolete("PackagerConfig.jsonへ移行しました。ASSET_STORE_TOOLS_CONFIG_FILE_NAMEを使用してください。", error: false)]
        public const string ASSET_STORE_TOOLS_IGNORE_FILE = ASSET_STORE_TOOLS_PATH + "/ignore.txt";

        /// <summary> パッケージ化設定ファイルの名前。対象フォルダ直下に置かれる。 </summary>
        public const string ASSET_STORE_TOOLS_CONFIG_FILE_NAME = "PackagerConfig.json";

        /// <summary> ディレクトリごとの現在リビジョンを持つバージョンログの名前。対象フォルダ直下に置かれる。 </summary>
        public const string ASSET_STORE_TOOLS_VERSION_LOG_FILE_NAME = "PackageVersions.json";

        /// <summary> 出力時のリビジョンを記録するファイルの名前。各パッケージ対象ディレクトリ直下に置かれる。 </summary>
        public const string ASSET_STORE_TOOLS_EXPORTED_VERSION_FILE_NAME = "ExportedVersion.json";

        /// <summary> 出力したパッケージ名とリビジョンの一覧の名前。出力先フォルダに置かれる。 </summary>
        public const string ASSET_STORE_TOOLS_MANIFEST_FILE_NAME = "PackageManifest.json";
        #endregion

        /// <summary> 管理ウィンドウ用UI Toolkitアセットの基準パス。 </summary>
        public static string UITK_PATH = FRAMEWORK_PATH + "/Editor/Administrator/UITK/";

        #region Setting Provider
        /// <summary> パッケージ固有のProjectSettingsファイルを保存する基準パス。 </summary>
        public const string PROJCET_SETTING_FILE_PATH = "ProjectSettings/Packages/" + SymphonyConstant.SYMPHONY_PACKAGE + "/";

        /// <summary> 開発者ごとのFramework設定ファイルを保存する基準パス。 </summary>
        public const string USER_SETTING_FILE_PATH = "UserSettings/" + SymphonyConstant.SYMPHONY_FRAMEWORK + "/";

        /// <summary> Unity Project Settings内の設定項目基準パス。 </summary>
        public const string PROJECT_SETTING_PATH = "Project/";
        #endregion

        #region Enumの名前
        /// <summary> オーディオグループenumの基底ファイル名。 </summary>
        public const string AudioGroupTypeEnumName = "AudioGroupType";

        /// <summary> シーン一覧enumの基底ファイル名。 </summary>
        public const string SceneListEnumFileName = "SceneList";

        /// <summary> タグenumの基底ファイル名。 </summary>
        public const string TagsEnumFileName = "Tags";

        /// <summary> レイヤーenumの基底ファイル名。 </summary>
        public const string LayersEnumFileName = "Layers";
        #endregion
    }
}
