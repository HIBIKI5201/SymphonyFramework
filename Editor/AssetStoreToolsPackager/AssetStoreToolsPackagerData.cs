using SymphonyFrameWork.Core;
using System.Runtime.CompilerServices;
using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    [FilePath(EditorSymphonyConstant.PROJCET_SETTING_FILE_PATH
        + nameof(AssetStoreToolsPackagerData) + ".asset",
        FilePathAttribute.Location.ProjectFolder)]
    public class AssetStoreToolsPackagerData : ScriptableSingleton<AssetStoreToolsPackagerData>
    {
        public static string AssetStoreToolsPath => instance._assetStoreToolsPath;

        private string _assetStoreToolsPath = EditorSymphonyConstant.ASSET_STORE_TOOLS_PATH;

        public void Save() => Save(true);
    }
}
