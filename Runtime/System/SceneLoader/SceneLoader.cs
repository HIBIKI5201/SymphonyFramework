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
            scene = default;

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            bool hasTrackedScene = _data.TryGetSceneInfo(sceneName, out SceneLoadData.SceneInfo info);
            if (hasTrackedScene && IsLoadedScene(info.Scene))
            {
                scene = info.Scene;
                return true;
            }

            Scene actualScene = SceneManager.GetSceneByName(sceneName);
            if (IsLoadedScene(actualScene))
            {
                _data.UpsertScene(sceneName, actualScene, hasTrackedScene ? info.Priority : 0);
                scene = actualScene;
                return true;
            }

            _data.RemoveScene(sceneName);
            return false;
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
        ///     既にロード済みのシーンを指定優先度で追跡登録する。
        /// </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="priority"> 優先度。 </param>
        /// <returns> 登録に成功した場合はtrue。 </returns>
        public static bool RegisterLoadedScene(string sceneName, int priority) =>
            _manager.TryRegisterLoadedScene(sceneName, priority);

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
            int priority = 0,
            CancellationToken token = default)
        {
            return _manager.LoadScene(
                name: sceneName,
                loadingAction: loadingAction,
                mode: mode,
                priority: priority,
                token: token);
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
        internal static void Initialize(CancellationToken destroyCancellationToken)
        {
            _destroyRegistration.Dispose();
            ResetRuntimeState();
            _data = new SceneLoadData();
            _manager = new(_data);
            _destroyRegistration = destroyCancellationToken.Register(ResetRuntimeState);
        }

        private static void ResetRuntimeState()
        {
            _data?.Clear();
            _manager = null;
            _data = null;
        }

        /// <summary>
        ///     ゲーム開始時の初期化処理
        /// </summary>
        internal static async ValueTask AfterSceneLoad()
        {
            await InitializeSceneLoad();
        }

        private static bool IsLoadedScene(Scene scene) =>
            scene.IsValid()
            && scene.isLoaded
            && !string.IsNullOrWhiteSpace(scene.name);

        /// <summary>
        ///     シーンの初期化
        /// </summary>
        /// <returns></returns>
        private static async ValueTask InitializeSceneLoad()
        {
            // 現状のシーン状況を保存する。
            _manager.ResetSceneData();

            SceneManagerConfig config = SymphonyConfigLocator.GetConfig<SceneManagerConfig>();
            // シーンリセットの条件が揃っていない場合は何もしない。
            if (config == null
                || !config.IsResetAndLoadOnPlay 
                || config.InitializeSceneList == null
                || config.InitializeSceneList.Length <= 0) { return; }


            // シーンのロード状況をリセットする。
            string[] resetIgnoreScenes = GetResetIgnoreScenes(config);
            await SceneResetter.ResetScene(_manager, resetIgnoreScenes);

            // ロードしていない初期シーンをロードする。
            await SceneResetter.LoadScene(_manager, config);
        }

        private static string[] GetResetIgnoreScenes(SceneManagerConfig config)
        {
            int resetIgnoreCount = config.ResetIgnoreSceneList?.Length ?? 0;
            int initializeSceneCount = config.InitializeSceneList?.Length ?? 0;

            string[] resetIgnoreScenes = new string[resetIgnoreCount + initializeSceneCount + 1];

            if (0 < resetIgnoreCount)
            {
                Array.Copy(config.ResetIgnoreSceneList, resetIgnoreScenes, resetIgnoreCount);
            }

            for (int i = 0; i < initializeSceneCount; i++)
            {
                resetIgnoreScenes[resetIgnoreCount + i] = config.InitializeSceneList[i];
            }

            resetIgnoreScenes[resetIgnoreCount + initializeSceneCount] = SymphonyCoreSystem.SYMPHONY_SCENE_NAME;
            return resetIgnoreScenes;
        }

        private static CancellationTokenRegistration _destroyRegistration;
    }
}
