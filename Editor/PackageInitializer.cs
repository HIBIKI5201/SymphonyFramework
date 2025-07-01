using System.IO;
using SymphonyFrameWork.Core;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     起動時に初期化する
    /// </summary>
    [InitializeOnLoad]
    public static class PackageInitializer
    {
        static PackageInitializer()
        {
            SymphonyConfigManager.AllConfigCheck();
            EnumInitialize();
            
            AssetDatabase.Refresh();
            
            Debug.Log("Symphony Framework Initialized");
        }

        private static void EnumInitialize()
        {
            //Enumファイルが無ければ生成する
            if (!Directory.Exists(EditorSymphonyConstant.ENUM_PATH))
            {
                AutoEnumGenerator.SceneListEnumGenerate();
                AutoEnumGenerator.TagsEnumGenerate();
                AutoEnumGenerator.LayersEnumGenerate();
                AutoEnumGenerator.AudioEnumGenerate();
            }
            
            //パッケージ内のEnumを消す
            var path = $"Packages/{SymphonyConstant.SYMPHONY_PACKAGE}/Enum"; //パッケージ内のEnumフォルダ
            if (Directory.Exists(path))
            {
                FileUtil.DeleteFileOrDirectory(path);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
                AssetDatabase.Refresh();
            }
            
            var enumAsmdefPath = EditorSymphonyConstant.ENUM_PATH + "/SymphonyFrameWork.Enum.asmdef";
            var mainAsmdefPath = EditorSymphonyConstant.FRAMEWORK_PATH() + "/SymphonyFrameWork.asmdef";
            AssemblyGenerator.AddAsssemblyReference(mainAsmdefPath, enumAsmdefPath);
        }
    }
}