using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Api = SymphonyFrameWork.System.API;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary>
    ///     <see cref="Api.SceneLoader"/> へ移動しました。
    /// </summary>
    [Obsolete("SymphonyFrameWork.System.API.SceneLoader に移動しました。今後はそちらを使用してください。", error: false)]
    public static class SceneLoader
    {
        /// <summary>
        ///     ロードされているシーンを返す。
        /// </summary>
        /// <param name="sceneName"> 取得するシーン名。 </param>
        /// <param name="scene"> 取得できたロード済みシーン。 </param>
        /// <returns> ロード済みシーンを取得できた場合はtrue。 </returns>
        public static bool GetExistScene(string sceneName, out Scene scene)
            => Api.SceneLoader.GetExistScene(sceneName, out scene);

        /// <summary>
        ///     シーンが存在するかどうか。
        /// </summary>
        /// <param name="sceneName"> 存在を確認するシーン名。 </param>
        /// <returns> シーンが追跡中の場合はtrue。 </returns>
        public static bool IsExist(string sceneName) => Api.SceneLoader.IsExist(sceneName);

        /// <summary>
        ///     シーンの状態を返す。
        /// </summary>
        /// <param name="sceneName"> 状態を取得するシーン名。 </param>
        /// <param name="state"> 取得できたシーン状態。 </param>
        /// <returns> 状態を取得できた場合はtrue。 </returns>
        public static bool TryGetState(string sceneName, out SceneLoadState state)
            => Api.SceneLoader.TryGetState(sceneName, out state);

        /// <summary>
        ///     シーンをアクティブにする。
        /// </summary>
        /// <param name="sceneName"> アクティブにするロード済みシーン名。 </param>
        /// <returns> アクティブシーンを変更できた場合はtrue。 </returns>
        public static bool SetActiveScene(string sceneName) => Api.SceneLoader.SetActiveScene(sceneName);

        /// <summary>
        ///     既にロード済みのシーンを指定優先度で追跡登録する。
        /// </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="priority"> 優先度。 </param>
        /// <returns> 登録に成功した場合はtrue。 </returns>
        public static bool RegisterLoadedScene(string sceneName, int priority) =>
            Api.SceneLoader.RegisterLoadedScene(sceneName, priority);

        /// <summary>
        ///     シーンをロードする。
        /// </summary>
        /// <param name="sceneName">シーン名</param>
        /// <param name="loadingAction">ロードの進捗率を引数にしたメソッド</param>
        /// <param name="mode"> AdditiveまたはSingle相当のロード方式。 </param>
        /// <param name="priority"> ロード後のアクティブシーン選択に使用する優先度。 </param>
        /// <param name="token"> ロード処理を中断するためのトークン。 </param>
        /// <returns>ロードに成功したか</returns>
        public static ValueTask<bool> LoadScene(
            string sceneName,
            Action<float> loadingAction = null,
            LoadSceneMode mode = LoadSceneMode.Additive,
            int priority = 0,
            CancellationToken token = default)
            => Api.SceneLoader.LoadScene(sceneName, loadingAction, mode, priority, token);

        /// <summary>
        ///     シーンをロードする。
        /// </summary>
        /// <param name="sceneNames"> ロードするシーン名の一覧。 </param>
        /// <param name="loadingAction"> 全シーンの平均進捗率を受け取る処理。 </param>
        /// <param name="token"> ロード処理を中断するためのトークン。 </param>
        /// <returns> すべてのシーンをロードできた場合はtrue。 </returns>
        public static ValueTask<bool> LoadScenes(
            string[] sceneNames,
            Action<float> loadingAction = null,
            CancellationToken token = default)
            => Api.SceneLoader.LoadScenes(sceneNames, loadingAction, token);

        /// <summary>
        ///     シーンをアンロードする。
        /// </summary>
        /// <param name="sceneName">シーン名</param>
        /// <param name="loadingAction">ロードの進捗率を引数にしたメソッド</param>
        /// <param name="token"> アンロード処理を中断するためのトークン。 </param>
        /// <returns>アンロードに成功したか</returns>
        public static ValueTask<bool> UnloadScene(
            string sceneName,
            Action<float> loadingAction = null,
            CancellationToken token = default)
            => Api.SceneLoader.UnloadScene(sceneName, loadingAction, token);

        /// <summary>
        ///     シーンをアンロードする。
        /// </summary>
        /// <param name="sceneNames"> アンロードするシーン名の一覧。 </param>
        /// <param name="loadingAction"> 全シーンの平均進捗率を受け取る処理。 </param>
        /// <param name="token"> アンロード処理を中断するためのトークン。 </param>
        /// <returns> すべてのシーンをアンロードできた場合はtrue。 </returns>
        public static ValueTask<bool> UnloadScenes(
            string[] sceneNames,
            Action<float> loadingAction = null,
            CancellationToken token = default)
            => Api.SceneLoader.UnloadScenes(sceneNames, loadingAction, token);

        /// <summary>
        ///     シーンがロードされた時に実行されるイベントを登録する。
        ///     ロード済みの場合は即座に実行される。
        /// </summary>
        /// <param name="sceneName"> ロード完了を監視するシーン名。 </param>
        /// <param name="action"> ロード完了後に一度実行する処理。 </param>
        public static void RegisterAfterSceneLoad(string sceneName, Action action) =>
            Api.SceneLoader.RegisterAfterSceneLoad(sceneName, action);

        /// <summary>
        ///     指定したシーンがロードされるまで待機する
        /// </summary>
        /// <param name="sceneName"> ロード完了を待機するシーン名。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        public static ValueTask WaitForLoadSceneAsync(string sceneName, CancellationToken token = default)
            => Api.SceneLoader.WaitForLoadSceneAsync(sceneName, token);
    }
}
