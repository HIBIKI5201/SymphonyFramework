using System.Runtime.CompilerServices;

namespace SymphonyFrameWork.Core
{
    /// <summary>
    ///     エディタ用の定数値を持つ
    /// </summary>
    public static class EditorSymphonyConstant
    {
        public static bool IsPackage([CallerFilePath] string sourceFilePath = "") =>
            !sourceFilePath.Replace('\\', '/').Contains("/Assets/");

        /// <summary>
        /// アセットかパッケージのルートパスを返す
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <returns></returns>
        public static string FRAMEWORK_PATH()
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

        #region 自動生成物のパス
        public static string RESOURCES_EDITOR_PATH = "Assets/Editor/" + SymphonyConstant.SYMPHONY_FRAMEWORK + "/Configs";
        public static string ENUM_PATH = "Assets/Scripts/" + SymphonyConstant.SYMPHONY_FRAMEWORK;

        public const string ASSET_STORE_TOOLS_PATH = "Assets/AssetStoreTools";
        #endregion

        public static string UITK_PATH = FRAMEWORK_PATH() + "/Editor/Administrator/UITK/";

        #region ウィンドウのコンフィグ
        public const string ServiceLocatorSetInstanceKey = "ServiceLocatorSetInstanceLog";
        public const bool ServiceLocatorSetInstanceDefault = true;
        public const string ServiceLocatorGetInstanceKey = "ServiceLocatorGetInstanceLog";
        public const bool ServiceLocatorGetInstanceDefault = false;
        public const string ServiceLocatorDestroyInstanceKey = "ServiceLocatorDestroyInstanceLog";
        public const bool ServiceLocatorDestroyInstanceDefault = true;
        #endregion

        #region Enumの名前
        public const string AudioGroupTypeEnumName = "AudioGroupType";
        public const string SceneListEnumFileName = "SceneList";
        public const string TagsEnumFileName = "Tags";
        public const string LayersEnumFileName = "Layers";
        #endregion
    }
}