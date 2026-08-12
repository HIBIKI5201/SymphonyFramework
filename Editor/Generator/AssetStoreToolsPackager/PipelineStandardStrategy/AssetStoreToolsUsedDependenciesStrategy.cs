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
    public sealed class AssetStoreToolsUsedDependenciesStrategy : AssetStoreToolsStandardStrategy
    {
        #region 外部向けAPI

        /// <inheritdoc />
        public override string DisplayName => "Used Dependencies";

        #endregion

        #region 内部処理

        /// <inheritdoc />
        protected internal override void Plan(AssetStoreToolsPackagePlan plan)
        {
            // 出力元自身を依存元から除外し、プロジェクト側から実際に参照されるアセットを確定する。
            HashSet<string> usedAssetPaths =
                CollectProjectUsedDependencies(AssetStoreToolsPackagerData.AssetStoreToolsPath);

            // 各出力単位を同じ依存関係集合で絞り込み、確認内容と実際の出力対象を一致させる。
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
            // ネイティブプラグイン等は依存関係だけでは欠落するため、指定拡張子を必ず残す。
            if (AssetStoreToolsPackager.HasForceIncludeExtension(path, forceIncludeExtensions)) { return true; }

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

            // Packagesを依存元に含めず、利用側プロジェクトのAssetsだけを走査する。
            string[] allAssetGuids = AssetDatabase.FindAssets(string.Empty, new[] { "Assets" });

            string normalizedExcludedRootPath = excludedRootPath
                ?.Replace("\\", "/")
                .TrimEnd('/');

            foreach (string guid in allAssetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // 出力元自身からの参照で全候補が使用中になることを避ける。
                if (!string.IsNullOrEmpty(normalizedExcludedRootPath)
                    && (path.Equals(normalizedExcludedRootPath, StringComparison.Ordinal)
                        || path.StartsWith(normalizedExcludedRootPath + "/", StringComparison.Ordinal)))
                {
                    continue;
                }

                // 間接参照も使用中として残すため、依存関係を再帰的に取得する。
                string[] dependencies = AssetDatabase.GetDependencies(path, recursive: true);

                foreach (string dependency in dependencies)
                {
                    usedPaths.Add(dependency);
                }
            }

            return usedPaths;
        }

        #endregion
    }
}
