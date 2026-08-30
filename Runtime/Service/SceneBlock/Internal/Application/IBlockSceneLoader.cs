using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.System.SceneLoad;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockが必要とするシーン操作の最小契約。
    /// </summary>
    /// <remarks>
    ///     Scene Loadサブシステムの具象型からApplicationを切り離し、
    ///     実際のシーンロードを伴わない単体テストを可能にするために置く。
    /// </remarks>
    internal interface IBlockSceneLoader
    {
        #region 外部向けAPI

        /// <summary>
        ///     指定したシーンが現在ロードされているか確認する。
        /// </summary>
        /// <param name="sceneName"> 確認するシーン名。 </param>
        /// <returns> ロード済みの場合はtrue。 </returns>
        bool IsSceneLoaded(string sceneName);

        /// <summary>
        ///     指定した複数シーンをまとめてロードする。
        /// </summary>
        /// <param name="requests"> ロードするシーン名と優先度の一覧。 </param>
        /// <param name="progress"> 平均進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> すべてロードできた場合はtrue。 </returns>
        Task<bool> LoadScenesAsync(
            IReadOnlyList<SceneLoadRequest> requests,
            IProgress<float> progress,
            CancellationToken token);

        /// <summary>
        ///     指定した複数シーンをまとめてアンロードする。
        /// </summary>
        /// <param name="sceneNames"> アンロードするシーン名の一覧。 </param>
        /// <param name="progress"> 平均進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> すべてアンロードできた場合はtrue。 </returns>
        Task<bool> UnloadScenesAsync(
            IReadOnlyList<string> sceneNames,
            IProgress<float> progress,
            CancellationToken token);

        #endregion
    }
}
