using SymphonyFrameWork.Config;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary>
    ///     シーンのロードを管理するクラス
    /// </summary>
    public static class SceneLoader
    {
        private static SceneLoadManager _manager;
        private static SceneLoadData _data;

        /// <summary>
        ///     ロードされているシーンを返す。
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static bool GetExistScene(string sceneName, out Scene scene)
        {
            bool result = _data.TryGetSceneInfo(sceneName, out SceneLoadData.SceneInfo info);
            scene = info.Scene;
            return result;
        }

        /// <summary>
        ///     シーンが存在するかどうか。
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public static bool IsExist(string sceneName) => _data.IsExistScene(sceneName);

        /// <summary>
        ///     シーンの状態を返す。
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool TryGetState(string sceneName, out SceneLoadState state) => _data.TryGetSceneState(sceneName, out state);

        /// <summary>
        ///     シーンをアクティブにする。
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public static bool SetActiveScene(string sceneName) => _manager.TrySetActiveScene(sceneName);

        /// <summary>
        ///     シーンをロードする。
        /// </summary>
        /// <param name="sceneName">シーン名</param>
        /// <param name="loadingAction">ロードの進捗率を引数にしたメソッド</param>
        /// <param name="mode"></param>
        /// <param name="token"></param>
        /// <returns>ロードに成功したか</returns>
        public static ValueTask<bool> LoadScene(
            string sceneName,
            Action<float> loadingAction = null,
            LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken token = default)
        {
            return _manager.LoadScene(
                sceneName,
                loadingAction,
                mode,
                token);
        }

        /// <summary>
        ///     シーンをロードする。
        /// </summary>
        /// <param name="sceneNames"></param>
        /// <param name="loadingAction"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static ValueTask<bool> LoadScenes(
            string[] sceneNames,
            Action<float> loadingAction = null,
            CancellationToken token = default)
        {
            return _manager.LoadScenes(
                sceneNames,
                loadingAction,
                token);
        }

        /// <summary>
        ///     シーンをアンロードする。
        /// </summary>
        /// <param name="sceneName">シーン名</param>
        /// <param name="loadingAction">ロードの進捗率を引数にしたメソッド</param>
        /// <param name="token"></param>
        /// <returns>アンロードに成功したか</returns>
        public static ValueTask<bool> UnloadScene(
            string sceneName,
            Action<float> loadingAction = null,
            CancellationToken token = default)
        {
            return _manager.UnloadScene(
                sceneName,
                loadingAction,
                token
                );
        }

        /// <summary>
        ///     シーンをアンロードする。
        /// </summary>
        /// <param name="sceneNames"></param>
        /// <param name="loadingAction"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static ValueTask<bool> UnloadScenes(
            string[] sceneNames,
            Action<float> loadingAction = null,
            CancellationToken token = default)
        {
            return _manager.UnloadScenes(
                sceneNames,
                loadingAction,
                token);
        }

        /// <summary>
        ///     シーンがロードされた時に実行されるイベントを登録する。
        ///     ロード済みの場合は即座に実行される。
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="action"></param>
        public static void RegisterAfterSceneLoad(string sceneName, Action action) =>
            _data.AddLoadedAction(sceneName, action);

        /// <summary>
        ///     指定したシーンがロードされるまで待機する
        /// </summary>
        /// <param name="sceneName"></param>
        public static async ValueTask WaitForLoadSceneAsync(string sceneName, CancellationToken token = default)
        {
            while (!_data.TryGetSceneState(sceneName, out SceneLoadState state) || state < SceneLoadState.Complete)
            {
                await Awaitable.NextFrameAsync(token);
            }
        }

        /// <summary>
        ///     コアシステムからの初期化
        /// </summary>
        internal static void Initialize()
        {
            _data = new SceneLoadData();
            _manager = new(_data);
        }

        /// <summary>
        ///     ゲーム開始時の初期化処理
        /// </summary>
        internal static async ValueTask AfterSceneLoad()
        {
            await InitializeSceneLoad();
        }

        /// <summary>
        ///     シーンの初期化
        /// </summary>
        /// <returns></returns>
        private static async ValueTask InitializeSceneLoad()
        {
            SceneManagerConfig config = SymphonyConfigLocator.GetConfig<SceneManagerConfig>();
            // シーンリセットの条件が揃っていない場合は何もしない。
            if (config == null || !config.IsResetAndLoadOnPlay || config.InitializeSceneList.Length <= 0) { return; }

            // 現状のシーン状況を保存する。
            _manager.ResetSceneData();

            // シーンのロード状況をリセットする。
            string[] resetIgnoreScenes = GetResetIgnoreScenes(config);
            await SceneResetter.ResetScene(_manager, config, resetIgnoreScenes);

            // ロードしていない初期シーンをロードする。
            await SceneResetter.LoadScene(_manager, config);
        }

        private static string[] GetResetIgnoreScenes(SceneManagerConfig config)
        {
            string[] resetIgnoreScenes = new string[config.ResetIgnoreSceneList.Length + 1];
            Array.Copy(config.ResetIgnoreSceneList, resetIgnoreScenes, config.ResetIgnoreSceneList.Length);
            resetIgnoreScenes[config.ResetIgnoreSceneList.Length] = SymphonyCoreSystem.SYMPHONY_SCENE_NAME;
            return resetIgnoreScenes;
        }
    }
}