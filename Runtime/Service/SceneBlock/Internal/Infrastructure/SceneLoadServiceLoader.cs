using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.System.SceneLoad;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockのシーン操作を、Scene Loadサブシステムへ委譲して実装する。
    /// </summary>
    /// <remarks>
    ///     Scene Load側はScene Blockを知らない。依存の向きを一方向に保つため、
    ///     契約の実装はScene Block側のこの型が持つ。
    /// </remarks>
    internal sealed class SceneLoadServiceLoader : IBlockSceneLoader
    {
        #region 外部向けAPI

        /// <summary>
        ///     委譲先のScene Load Serviceを指定して生成する。
        /// </summary>
        /// <param name="service"> シーン操作を実行するService。 </param>
        /// <exception cref="ArgumentNullException"> serviceがnullの場合。 </exception>
        internal SceneLoadServiceLoader(SceneLoadService service)
        {
            // Compositionが必ず初期化済みのServiceを渡す契約であり、欠落は結線の誤りとして扱う。
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        ///     指定したシーンが現在ロードされているか確認する。
        /// </summary>
        /// <param name="sceneName"> 確認するシーン名。 </param>
        /// <returns> ロード済みの場合はtrue。 </returns>
        public bool IsSceneLoaded(string sceneName) =>
            _service.TryGetLoadedScene(sceneName, out _);

        /// <summary>
        ///     指定した複数シーンをまとめてロードする。
        /// </summary>
        /// <param name="requests"> ロードするシーン名と優先度の一覧。 </param>
        /// <param name="progress"> 平均進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> すべてロードできた場合はtrue。 </returns>
        public Task<bool> LoadScenesAsync(
            IReadOnlyList<SceneLoadRequest> requests,
            IProgress<float> progress,
            CancellationToken token)
        {
            // Scene Load Serviceは配列を受け取る契約のため、境界でだけ配列へ写す。
            return _service.LoadScenes(ToArray(requests), progress, token);
        }

        /// <summary>
        ///     指定した複数シーンをまとめてアンロードする。
        /// </summary>
        /// <param name="sceneNames"> アンロードするシーン名の一覧。 </param>
        /// <param name="progress"> 平均進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> すべてアンロードできた場合はtrue。 </returns>
        public Task<bool> UnloadScenesAsync(
            IReadOnlyList<string> sceneNames,
            IProgress<float> progress,
            CancellationToken token)
        {
            return _service.UnloadScenes(ToArray(sceneNames), progress, token);
        }

        #endregion

        #region 内部処理

        private readonly SceneLoadService _service;

        /// <summary>
        ///     読み取り専用の一覧を配列へ写す。
        /// </summary>
        /// <typeparam name="T"> 要素の型。 </typeparam>
        /// <param name="source"> コピー元の一覧。 </param>
        /// <returns> 同じ順序の配列。 </returns>
        private static T[] ToArray<T>(IReadOnlyList<T> source)
        {
            if (source == null) { return Array.Empty<T>(); }

            T[] snapshot = new T[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                snapshot[index] = source[index];
            }

            return snapshot;
        }

        #endregion
    }
}
