using SymphonyFrameWork.Core;
using System;
using System.Buffers;
using System.IO;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    public readonly ref struct AssetStoreToolsPackageContext
    {
        public AssetStoreToolsPackageContext(
            string basePackageName,
            string exportRoot,
            string[] exportDirectories)
        {
            ExportDirectories = exportDirectories;
            this.DateTime = DateTime.Now;
            PackageName = GeneratePackageName(basePackageName, DateTime);

            ExportRoot = Path.Combine(Application.dataPath, "..", exportRoot);
            ExportLocalPath = Path.Combine(exportRoot, PackageName);
            ExportFullPath = Path.Combine(ExportRoot, PackageName);
        }

        public readonly string PackageName;
        public readonly string ExportRoot;
        public readonly string ExportLocalPath;
        public readonly string ExportFullPath;
        public readonly string[] ExportDirectories;
        public readonly DateTime DateTime;

        private static string GeneratePackageName(string baseName, DateTime dateTime)
            => $"Export_{baseName}_{dateTime:yyyyMMdd_HHmmss}";
    }
}
