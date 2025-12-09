using SymphonyFrameWork.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     シーンのロードを管理するクラス
    /// </summary>
    public static class SceneLoader
    {
        public static Task InitializeScenesLoadTask => _initializeScenesLoadTask;

        private static readonly Dictionary<string, Scene> _sceneDict = new();
        private static readonly Dictionary<string, Action> _loadedActionDict = new();

        private static Task _initializeScenesLoadTask;

        /// <summary>
        ///     ロードされているシーンを返す
        ///     ない場合はnullを返す
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static bool GetExistScene(string sceneName, out Scene scene)
        {
            return _sceneDict.TryGetValue(sceneName, out scene);
        }

        /// <summary>
        ///     シーンをアクティブにする
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public static bool SetActiveScene(string sceneName)
        {
            if (_sceneDict.TryGetValue(sceneName, out var scene))
            {
                SceneManager.SetActiveScene(scene);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     シーンをロードする。
        /// </summary>
        /// <param name="sceneName">シーン名</param>
        /// <param name="loadingAction">ロードの進捗率を引数にしたメソッド</param>
        /// <param name="mode"></param>
        /// <param name="token"></param>
        /// <returns>ロードに成功したか</returns>
        public static async Task<bool> LoadScene(
            string sceneName,
            Action<float> loadingAction = null,
            LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken token = default)
        {
            //ロードしようとしているシーンが既にないか確認。
            if (_sceneDict.ContainsKey(sceneName))
            {
                Debug.LogWarning($"{sceneName}シーンは既にロードされています");
                return false;
            }

            #region ロード開始
            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (operation == null)
            {
                Debug.LogError($"{sceneName}シーンは登録されていません");
                return false;
            }
            #endregion

            #region ロード中。
            while (!operation.isDone)
            {
                loadingAction?.Invoke(operation.progress);
                await Awaitable.NextFrameAsync(token);
            }
            #endregion

            #region ロード完了後。
            var loadedScene = SceneManager.GetSceneByName(sceneName);

            //ロード終了後にロード待ちしていたイベントを実行。
            if (_loadedActionDict.TryGetValue(sceneName, out var action))
            {
                action?.Invoke();
                _loadedActionDict.Remove(sceneName);
            }

            //辞書にシーン名とシーン情報を保存。
            var isLoadSuccess = loadedScene.IsValid() && loadedScene.isLoaded;
            if (isLoadSuccess)
            {
                //シングルロードの場合は辞書をクリアする。
                if (mode == LoadSceneMode.Single)
                {
                    _sceneDict.Clear();
                }

                _sceneDict.TryAdd(sceneName, loadedScene);
            }
            #endregion

            #region シーン上のオブジェクトの初期化を実行する。
            GameObject[] objs = loadedScene.GetRootGameObjects();

            List<Task> initializeTasks = new();

            foreach (var obj in objs) //初期化インターフェースを取得して実行。
            {
                if (obj.TryGetComponent<IInitializeAsync>(out var initialize))
                {
                    initializeTasks.Add(initialize.DoInitialize());
                }
            }

            if (0 < initializeTasks.Count) //初期化が終了するまで待機。
            {
                await Task.WhenAll(initializeTasks);
            }
            #endregion

            return isLoadSuccess;
        }

        public static async Task<bool> LoadScenes(
            string[] sceneNames,
            Action<float> loadingAction = null,
            CancellationToken token = default)
        {
            Task<bool>[] loadTasks = new Task<bool>[sceneNames.Length]; // シーンごとのロードタスク。
            float[] progresses = new float[sceneNames.Length]; // シーンごとの進捗率。

            // 全てのシーンのロードを開始。
            for (int i = 0; i < sceneNames.Length; i++)
            {
                int index = i;
                loadTasks[i] = LoadScene(sceneNames[i], n => progresses[index] = n, token: token);
            }

            // ロード中の進捗率を計算して通知。
            while (!token.IsCancellationRequested)
            {
                // 全てのシーンの平均進捗率を計算。
                float totalProgress = 0f;
                for (int i = 0; i < progresses.Length; i++)
                {
                    totalProgress += progresses[i];
                }
                float averageProgress = totalProgress / progresses.Length;
                loadingAction?.Invoke(averageProgress);

                // デバッグ用に各シーンの進捗率をログ出力。
                StringBuilder debugProgress = new StringBuilder($"AverageProgress : {averageProgress}");
                for (int i = 0; i < progresses.Length; i++)
                {
                    debugProgress.Append($"\n  Scene {sceneNames[i]} Progress : {progresses[i]}");
                }
                Debug.Log(debugProgress.ToString());

                // 全てのシーンのロードが完了したか確認。
                bool allDone = true;
                for (int i = 0; i < loadTasks.Length; i++)
                {
                    if (!loadTasks[i].IsCompleted)
                    {
                        allDone = false;
                        break;
                    }
                }

                // 全てのシーンのロードが完了した場合、ループを抜ける。
                if (allDone) { break; }
                await Awaitable.NextFrameAsync(token);
            }

            for (int i = 0; i < loadTasks.Length; i++)
            {
                if (!loadTasks[i].Result) { return false; }
            }

            return true;
        }

        /// <summary>
        ///     シーンをアンロードする
        /// </summary>
        /// <param name="sceneName">シーン名</param>
        /// <param name="loadingAction">ロードの進捗率を引数にしたメソッド</param>
        /// <param name="token"></param>
        /// <returns>アンロードに成功したか</returns>
        public static async Task<bool> UnloadScene(
            string sceneName,
            Action<float> loadingAction = null,
            CancellationToken token = default)
        {
            if (!_sceneDict.ContainsKey(sceneName))
            {
                Debug.LogWarning($"{sceneName}シーンはロードされていません");
                return false;
            }

            //アンロード開始
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"{sceneName}シーンは登録されていません");
                return false;
            }

            //ロード中
            while (!operation.isDone)
            {
                loadingAction?.Invoke(operation.progress);
                await Awaitable.NextFrameAsync(token);
            }

            _sceneDict.Remove(sceneName);

            return true;
        }

        /// <summary>
        ///     シーンがロードされた時に実行される
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="action"></param>
        public static void RegisterAfterSceneLoad(string sceneName, Action action)
        {
            //既にロードされていたら終了
            if (_sceneDict.ContainsKey(sceneName))
            {
                action?.Invoke();
                return;
            }

            //アクションを追加
            if (!_loadedActionDict.TryAdd(sceneName, action))
                _loadedActionDict[sceneName] += action;
        }

        /// <summary>
        ///     指定したシーンがロードされるまで待機する
        /// </summary>
        /// <param name="sceneName"></param>
        public static async Task WaitForLoadSceneAsync(string sceneName)
        {
            while (!_sceneDict.ContainsKey(sceneName))
            {
                await Awaitable.NextFrameAsync();
            }
        }

        /// <summary>
        ///     指定したシーンがロードされるまで待機する
        /// </summary>
        /// <param name="sceneName"></param>
        [Obsolete]
        public static async Task WaitForLoadScene(string sceneName)
        {
            while (!_sceneDict.ContainsKey(sceneName))
            {
                await Awaitable.NextFrameAsync();
            }
        }

        /// <summary>
        ///     コアシステムからの初期化
        /// </summary>
        internal static void Initialize()
        {
            _sceneDict.Clear();
            _initializeScenesLoadTask = null;
        }

        /// <summary>
        ///     ゲーム開始時の初期化処理
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static async Task AfterSceneLoad()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                //シーン名とシーン情報を辞書に保存
                _sceneDict.TryAdd(scene.name, scene);
            }

            //初期化シーンのロード開始
            var config = SymphonyConfigLocator.GetConfig<SceneManagerConfig>();
            if (!config) return;

            List<string> initializeSceneList = config.InitializeSceneList;
            int count = initializeSceneList.Count;

            if (count <= 0) return;

            //全てのロードを待機するタスクを作成
            Task[] initializeTasks = new Task[count];
            for (int i = 0; i < count; i++)
            {
                string sceneName = initializeSceneList[i];
                initializeTasks[i] = LoadScene(sceneName);
            }

            _initializeScenesLoadTask = Task.WhenAll(initializeTasks);

            await _initializeScenesLoadTask;

            _initializeScenesLoadTask = Task.CompletedTask;
        }
    }
}