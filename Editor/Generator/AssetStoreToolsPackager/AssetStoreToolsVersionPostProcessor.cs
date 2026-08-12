using System;
using System.Collections.Generic;

using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     パッケージ対象ディレクトリの変更を検知し、リビジョンの加算を処理待ちへ積む。
    /// </summary>
    /// <remarks>
    ///     AssetPostprocessorは任意のタイミングで再入するため、この型はファイル書き込み、
    ///     AssetDatabase.Refresh、購読を所有しない。変更を記録してSymphonyEditorOrchestratorへ
    ///     通知し、実際の書き出しはOrchestratorがcoalesceして1回だけ行う。
    /// </remarks>
    internal sealed class AssetStoreToolsVersionPostProcessor : AssetPostprocessor
    {
        #region 外部向けAPI

        /// <summary> host callbackによる変更が処理待ちになったときに通知する。 </summary>
        internal static event Action OnHostChangesPending;

        /// <summary>
        ///     処理待ちのディレクトリ名を破棄する。
        /// </summary>
        internal static void Initialize()
        {
            _pendingDirectoryNames.Clear();
        }

        /// <summary>
        ///     処理待ちのディレクトリ名を破棄する。
        /// </summary>
        internal static void Shutdown()
        {
            _pendingDirectoryNames.Clear();
        }

        /// <summary>
        ///     coalesceした変更ぶんのリビジョンを進め、バージョンログを1回だけ書き出す。
        /// </summary>
        /// <returns> バージョンログを書き出し、AssetDatabaseの更新が必要な場合はtrue。 </returns>
        internal static bool ProcessPendingChanges()
        {
            // 保留が無ければファイルI/OとAssetDatabase更新を発生させない。
            if (_pendingDirectoryNames.Count == 0) { return false; }

            AssetStoreToolsVersionLog log = AssetStoreToolsVersionLogStore.Load();

            // 壊れたバージョンログを読み込めなかった場合は加算しない。
            // 巻き戻したリビジョンで上書きすると、インポート先が更新を見落とす。
            //
            // 保留は消費せずに残す。読み込み失敗はファイルロックのように一過性のことがあり、
            // ここで捨てると次に同じディレクトリが変更されるまでリビジョンが進まない。
            if (log == null) { return false; }

            // 読み込めた後で消費する。書き出しに失敗しても同じ変更で再入し続けないようにする。
            string[] directoryNames = new string[_pendingDirectoryNames.Count];
            _pendingDirectoryNames.CopyTo(directoryNames);
            _pendingDirectoryNames.Clear();

            // 同一バッチではディレクトリごとに1回だけリビジョンを進める。
            foreach (string directoryName in directoryNames) { log.IncrementVersion(directoryName); }

            return AssetStoreToolsVersionLogStore.Save(log);
        }

        #endregion

        #region 内部処理

        /// <summary> 加算対象として記録済みのディレクトリ名。 </summary>
        private static readonly HashSet<string> _pendingDirectoryNames =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     パッケージ対象ディレクトリ配下の変更を処理待ちとして記録する。
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            string rootPath = AssetStoreToolsPackagerData.AssetStoreToolsPath;
            // 対象ルートが未設定なら、すべてのアセット変更を無関係として扱う。
            if (string.IsNullOrEmpty(rootPath)) { return; }

            // インポート、削除、移動先、移動元を同一バッチへ集約し、重複通知を避ける。
            bool hasPendingChange = false;
            hasPendingChange |= CollectDirectoryNames(importedAssets, rootPath);
            hasPendingChange |= CollectDirectoryNames(deletedAssets, rootPath);
            hasPendingChange |= CollectDirectoryNames(movedAssets, rootPath);
            hasPendingChange |= CollectDirectoryNames(movedFromAssetPaths, rootPath);

            // 新しい処理待ちが追加された場合だけ、Orchestratorへ書き出し要求を通知する。
            if (hasPendingChange) { OnHostChangesPending?.Invoke(); }
        }

        /// <summary>
        ///     アセットパスから加算対象のディレクトリ名を集めて処理待ちへ積む。
        /// </summary>
        /// <param name="assetPaths"> 変更されたアセットのパス。 </param>
        /// <param name="rootPath"> パッケージ対象フォルダのルートパス。 </param>
        /// <returns> 1件でも積んだ場合はtrue。 </returns>
        private static bool CollectDirectoryNames(string[] assetPaths, string rootPath)
        {
            // Unityのコールバックが一覧を渡さない変更種別は処理対象外とする。
            if (assetPaths == null) { return false; }

            bool hasPendingChange = false;
            // 同じディレクトリの複数変更はHashSetへ集約し、リビジョンを1回だけ進める。
            foreach (string assetPath in assetPaths)
            {
                if (AssetStoreToolsVersionPathResolver.TryResolveDirectoryName(
                        assetPath,
                        rootPath,
                        out string directoryName))
                {
                    hasPendingChange |= _pendingDirectoryNames.Add(directoryName);
                }
            }

            return hasPendingChange;
        }

        #endregion
    }
}
