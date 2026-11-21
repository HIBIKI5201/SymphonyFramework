using System.Collections.Generic;
using System.Threading;

using SymphonyFramework.Scenes;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary>
    ///     6.x の <c>SceneLoader</c> を <see cref="Attacca" /> へ転送する。
    /// </summary>
    /// <remarks>
    ///     7.0.0 の移行期間だけ残す。8.0.0 で削除する。
    ///     <c>Action&lt;float&gt;</c> の進捗は <see cref="Attacca.WrapProgress" /> で <c>IProgress&lt;float&gt;</c> へ包む。
    ///     <b>Suite が保持しているシーンのアンロードは、6.x と違い <c>false</c> を返して何もしない（6.18.0 の変更）。</b>
    /// </remarks>
    [global::System.Obsolete("SceneLoader は Attacca（名前空間 SymphonyFramework.Scenes）へ改名しました。Attacca を使用してください。8.0.0 で削除します。")]
    public static class SceneLoader
    {
        #region 外部向けAPI

        /// <summary> 旧 <c>GetExistScene</c>。<see cref="Attacca.TryGetScene" /> へ転送する。 </summary>
        public static bool GetExistScene(string sceneName, out Scene scene) => Attacca.TryGetScene(sceneName, out scene);

        /// <summary> 旧 <c>IsExist</c>。<see cref="Attacca.IsLoaded" /> へ転送する。 </summary>
        public static bool IsExist(string sceneName) => Attacca.IsLoaded(sceneName);

        /// <summary> <see cref="Attacca.TryGetState" /> へ転送する。 </summary>
        public static bool TryGetState(string sceneName, out SceneLoadStateEnum state) =>
            Attacca.TryGetState(sceneName, out state);

        /// <summary> <see cref="Attacca.GetSceneInfos" /> へ転送する。 </summary>
        public static IReadOnlyList<SceneLoadInfo> GetSceneInfos() => Attacca.GetSceneInfos();

        /// <summary> <see cref="Attacca.TryGetSceneInfo" /> へ転送する。 </summary>
        public static bool TryGetSceneInfo(string sceneName, out SceneLoadInfo sceneInfo) =>
            Attacca.TryGetSceneInfo(sceneName, out sceneInfo);

        /// <summary> <see cref="Attacca.SetActiveScene" /> へ転送する。 </summary>
        public static bool SetActiveScene(string sceneName) => Attacca.SetActiveScene(sceneName);

        /// <summary> <see cref="Attacca.RegisterLoadedScene" /> へ転送する。 </summary>
        public static bool RegisterLoadedScene(string sceneName, int priority) =>
            Attacca.RegisterLoadedScene(sceneName, priority);

        /// <summary> 旧 <c>LoadSceneAsync</c>。<see cref="Attacca.LoadAsync(SceneLoadRequest, global::System.IProgress{float}, LoadSceneMode, CancellationToken)" /> へ転送する。 </summary>
        public static Awaitable<bool> LoadSceneAsync(
            SceneLoadRequest request,
            global::System.IProgress<float> progress = null,
            LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken token = default) =>
            Attacca.LoadAsync(request, progress, mode, token);

        /// <summary> 旧 <c>LoadSceneAsync</c>。進捗の Action を包んで <see cref="Attacca" /> へ転送する。 </summary>
        public static Awaitable<bool> LoadSceneAsync(
            string sceneName,
            global::System.Action<float> loadingAction = null,
            LoadSceneMode mode = LoadSceneMode.Additive,
            int priority = 0,
            CancellationToken token = default) =>
            Attacca.LoadAsync(sceneName, Attacca.WrapProgress(loadingAction), mode, priority, token);

        /// <summary> 旧 <c>LoadScenesAsync</c>。<see cref="Attacca" /> の配列版へ転送する。 </summary>
        public static Awaitable<bool> LoadScenesAsync(
            SceneLoadRequest[] requests,
            global::System.IProgress<float> progress = null,
            CancellationToken token = default) =>
            Attacca.LoadAsync(requests, progress, token);

        /// <summary> 旧 <c>LoadScenesAsync</c>。進捗の Action を包んで <see cref="Attacca" /> の配列版へ転送する。 </summary>
        public static Awaitable<bool> LoadScenesAsync(
            string[] sceneNames,
            global::System.Action<float> loadingAction = null,
            CancellationToken token = default) =>
            Attacca.LoadAsync(sceneNames, Attacca.WrapProgress(loadingAction), token);

        /// <summary> 旧 <c>UnloadSceneAsync</c>。進捗の Action を包んで <see cref="Attacca" /> へ転送する。 </summary>
        public static Awaitable<bool> UnloadSceneAsync(
            string sceneName,
            global::System.Action<float> loadingAction = null,
            CancellationToken token = default) =>
            Attacca.UnloadAsync(sceneName, Attacca.WrapProgress(loadingAction), token);

        /// <summary> 旧 <c>UnloadScenesAsync</c>。進捗の Action を包んで <see cref="Attacca" /> の配列版へ転送する。 </summary>
        public static Awaitable<bool> UnloadScenesAsync(
            string[] sceneNames,
            global::System.Action<float> loadingAction = null,
            CancellationToken token = default) =>
            Attacca.UnloadAsync(sceneNames, Attacca.WrapProgress(loadingAction), token);

        /// <summary> 旧 <c>RegisterAfterSceneLoad</c>。<see cref="Attacca.WhenLoaded" /> へ転送する。 </summary>
        public static void RegisterAfterSceneLoad(string sceneName, global::System.Action action) =>
            Attacca.WhenLoaded(sceneName, action);

        /// <summary> 旧 <c>WaitForLoadSceneAsync</c>。<see cref="Attacca.WaitForLoadAsync" /> へ転送する。 </summary>
        public static Awaitable WaitForLoadSceneAsync(string sceneName, CancellationToken token = default) =>
            Attacca.WaitForLoadAsync(sceneName, token);

        #endregion
    }
}
