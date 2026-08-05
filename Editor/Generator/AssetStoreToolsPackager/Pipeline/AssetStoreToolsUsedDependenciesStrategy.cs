using System;
using System.Collections.Generic;
using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     出力対象を、プロジェクト内で使用中のアセットと強制包含拡張子だけへ絞り込む手順。
    /// </summary>
    /// <remarks>
    ///     Plan段階の手順。パイプラインのどこに置いてもExecute段階より先に走る。
    /// </remarks>
    [Serializable]
    public sealed class AssetStoreToolsUsedDependenciesStrategy : AssetStoreToolsPackageStepStrategy
    {
        /// <inheritdoc />
        public override string DisplayName => "Used Dependencies";

        /// <inheritdoc />
        protected internal override void Plan(AssetStoreToolsPackagePlan plan)
        {
            HashSet<string> usedAssetPaths =
                CollectProjectUsedDependencies(AssetStoreToolsPackagerData.AssetStoreToolsPath);

            foreach (AssetStoreToolsPackagePlanEntry entry in plan.Entries)
            {
                entry.FilterAssetPaths(
                    path => IsUsedTarget(path, usedAssetPaths, plan.ForceIncludeExtensions));
            }
        }

        /// <summary>
        ///     アセットを出力対象として残すか判定する。
        /// </summary>
        /// <param name="path"> 判定するアセットのパス。 </param>
        /// <param name="usedAssetPaths"> プロジェクト内で使用中のアセットのパス集合。 </param>
        /// <param name="forceIncludeExtensions"> 依存関係に関わらず含める拡張子の一覧。 </param>
        /// <returns> 強制包含の拡張子に一致するか、使用中アセットであればtrue。 </returns>
        internal static bool IsUsedTarget(
            string path,
            HashSet<string> usedAssetPaths,
            IReadOnlyList<string> forceIncludeExtensions)
        {
            if (AssetStoreToolsPackager.HasForceIncludeExtension(path, forceIncludeExtensions))
            {
                return true;
            }

            return usedAssetPaths != null && usedAssetPaths.Contains(path);
        }

        /// <summary>
        ///     プロジェクト内の全アセットが依存している（＝使用している）アセットのパス一覧を取得する。
        /// </summary>
        /// <param name="excludedRootPath"> 依存元から除外するルートパス。 </param>
        /// <returns> 使用中アセットのパス集合。 </returns>
        private static HashSet<string> CollectProjectUsedDependencies(string excludedRootPath)
        {
            HashSet<string> usedPaths = new();

            // プロジェクト内のすべての一般アセット（Assetsフォルダ以下）を検索
            string[] allAssetGuids = AssetDatabase.FindAssets(string.Empty, new[] { "Assets" });

            string normalizedExcludedRootPath = excludedRootPath
                ?.Replace("\\", "/")
                .TrimEnd('/');

            foreach (string guid in allAssetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // AssetStoreToolsは除外して依存関係を追う
                if (!string.IsNullOrEmpty(normalizedExcludedRootPath)
                    && (path.Equals(normalizedExcludedRootPath, StringComparison.Ordinal)
                        || path.StartsWith(normalizedExcludedRootPath + "/", StringComparison.Ordinal)))
                {
                    continue;
                }

                // そのアセットが依存しているリソースをすべて取得
                string[] dependencies = AssetDatabase.GetDependencies(path, recursive: true);

                foreach (string dependency in dependencies)
                {
                    usedPaths.Add(dependency);
                }
            }

            return usedPaths;
        }
    }
}
