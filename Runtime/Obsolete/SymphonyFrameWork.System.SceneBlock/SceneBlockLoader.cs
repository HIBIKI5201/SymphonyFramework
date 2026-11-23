using System.Collections.Generic;
using System.Threading;

using SymphonyFramework.Scenes;

using UnityEngine;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     6.x の <c>SceneBlockLoader</c> を <see cref="Suite" /> へ転送する。
    /// </summary>
    /// <remarks>
    ///     7.0.0 の移行期間だけ残す。8.0.0 で削除する。
    ///     引数と戻り値は新しい型（<see cref="SuiteAsset" />、<see cref="SuiteInfo" />）になる。
    ///     旧型名（<c>SceneBlockAsset</c>、<c>SceneBlockInfo</c>）は、新しい型の <c>[MovedFrom]</c> で Unity の API Updater が書き換える。
    /// </remarks>
    [global::System.Obsolete("SceneBlockLoader は Suite（名前空間 SymphonyFramework.Scenes）へ改名しました。Suite を使用してください。8.0.0 で削除します。")]
    public static class SceneBlockLoader
    {
        #region 外部向けAPI

        /// <summary> <see cref="Suite.LoadAsync" /> へ転送する。 </summary>
        public static Awaitable<bool> LoadAsync(
            SuiteAsset block,
            global::System.IProgress<float> progress = null,
            CancellationToken token = default) =>
            Suite.LoadAsync(block, progress, token);

        /// <summary> <see cref="Suite.UnloadAsync" /> へ転送する。 </summary>
        public static Awaitable<bool> UnloadAsync(
            SuiteAsset block,
            global::System.IProgress<float> progress = null,
            CancellationToken token = default) =>
            Suite.UnloadAsync(block, progress, token);

        /// <summary> <see cref="Suite.IsLoaded" /> へ転送する。 </summary>
        public static bool IsLoaded(string blockName) => Suite.IsLoaded(blockName);

        /// <summary> 旧 <c>TryGetBlockInfo</c>。<see cref="Suite.TryGetInfo" /> へ転送する。 </summary>
        public static bool TryGetBlockInfo(string blockName, out SuiteInfo blockInfo) =>
            Suite.TryGetInfo(blockName, out blockInfo);

        /// <summary> 旧 <c>GetBlockInfos</c>。<see cref="Suite.GetInfos" /> へ転送する。 </summary>
        public static IReadOnlyList<SuiteInfo> GetBlockInfos() => Suite.GetInfos();

        #endregion
    }
}
