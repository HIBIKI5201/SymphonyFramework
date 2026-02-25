using SymphonyFrameWork.Debugger;
using SymphonyFrameWork.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System.SceneLoad
{
    public class SceneLoadManager
    {
        public SceneLoadManager(SceneLoadData data)
        {
            _data = data;
        }

        public bool TrySetActiveScene(string name)
        {
            if (!_data.TryGetSceneInfo(name, out SceneLoadData.SceneInfo info))
            {
                SymphonyDebugLogger.LogDirect($"{name} is not loaded scene");
                return false;
            }

            SceneManager.SetActiveScene(info.Scene);
            return true;
        }

        /// <summary>
        ///     シーンをロードする。
        /// </summary>
        /// <param name="name">シーン名</param>
        /// <param name="loadingAction">ロードの進捗率を引数にしたメソッド</param>
        /// <param name="mode"></param>
        /// <param name="token"></param>
        /// <returns>ロードに成功したか</returns>
        public async ValueTask<bool> LoadScene(
            string name,
            Action<float> loadingAction = null,
            LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken token = default)
        {
            //ロードしようとしているシーンが既に存在するか確認。
            if (_data.IsExistScene(name))
            {
                Debug.LogWarning($"{name} is already loaded.");
                return false;
            }

            #region ロード開始
            var operation = SceneManager.LoadSceneAsync(name, mode);
            if (operation == null)
            {
                Debug.LogError($"{name} is not register. check scene list of build setting.");
                return false;
            }

            _data.LoadStart(name);

            #endregion

            #region ロード中。
            await SymphonyTask.WaitUntil(
                () =>
                {
                    loadingAction?.Invoke(operation.progress);
                    return operation.isDone;
                },
                token);
            #endregion

            #region ロード完了後。
            Scene loadedScene = SceneManager.GetSceneByName(name);

            //辞書にシーン名とシーン情報を保存。
            var isLoadSuccess = loadedScene.IsValid() && loadedScene.isLoaded;
            if (!isLoadSuccess)
            {
                Debug.LogWarning($"Failed Loading Scene: {name}");
                _data.LoadFail(name);
                return false;
            }

            //シングルロードの場合は辞書をクリアする。
            if (mode == LoadSceneMode.Single)
            {
                _data.Reset(new KeyValuePair<string, Scene>(name, loadedScene));
            }
            else
            {
                _data.LoadComplete(name, loadedScene);
            }

            //ロード終了後にロード待ちしていたイベントを実行。
            _data.InvokeLoadedAction(name);
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

        /// <summary>
        ///     シーンをロードする。
        /// </summary>
        /// <param name="sceneName">シーン名</param>
        /// <param name="loadingAction">ロードの進捗率を引数にしたメソッド</param>
        /// <param name="mode"></param>
        /// <param name="token"></param>
        /// <returns>ロードに成功したか</returns>
        public async ValueTask<bool> LoadScenes(
            string[] names,
            Action<float> loadingAction = null,
            CancellationToken token = default)
        {
            foreach (string scene in names)
            {
                if (string.IsNullOrEmpty(scene))
                {
                    Debug.LogWarning($"load scenes is canceled becouse contain null or empty in scene names");
                    return false;
                }
            }

            ValueTask<bool>[] loadTasks = new ValueTask<bool>[names.Length]; // シーンごとのロードタスク。
            float[] progresses = new float[names.Length]; // シーンごとの進捗率。

            // 全てのシーンのロードを開始。
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                loadTasks[i] = LoadScene(names[i], n => progresses[index] = n, token: token);
            }

            StringBuilder debugProgress = new StringBuilder();
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

                #region デバッグ用に各シーンの進捗率をログ出力。
                debugProgress.Clear();
                debugProgress.AppendLine($"AverageProgress : {averageProgress}");
                for (int i = 0; i < progresses.Length; i++)
                {
                    debugProgress.Append($"\n  Scene {names[i]} Progress : {progresses[i]}");
                }
                Debug.Log(debugProgress.ToString());
                #endregion

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
        ///     シーンをアンロードする。
        /// </summary>
        /// <param name="name">シーン名</param>
        /// <param name="loadingAction">ロードの進捗率を引数にしたメソッド</param>
        /// <param name="token"></param>
        /// <returns>アンロードに成功したか</returns>
        public async ValueTask<bool> UnloadScene(
            string name,
            Action<float> loadingAction = null,
            CancellationToken token = default)
        {
            if (!_data.IsExistScene(name))
            {
                Debug.LogWarning($"{name} is not loaded");
                return false;
            }

            //アンロード開始。
            var operation = SceneManager.UnloadSceneAsync(name);
            if (operation == null)
            {
                Debug.LogError($"{name} is not register. check scene list of build setting.");
                return false;
            }

            _data.UnloadStart(name);

            //ロード中。
            await SymphonyTask.WaitUntil(
                () =>
                {
                    loadingAction?.Invoke(operation.progress);
                    return operation.isDone;
                },
                token);

            _data.UnloadComplete(name);

            return true;
        }

        internal void ResetSceneData()
        {
            int sceneCount = SceneManager.sceneCount;
            KeyValuePair<string, Scene>[] kvps = new KeyValuePair<string, Scene>[sceneCount];
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                kvps[i] = new KeyValuePair<string, Scene>(scene.name, scene);
            }

            _data.Reset(kvps);
        }

        internal async ValueTask InitializeLoad(string[] names)
        {
            ValueTask<bool>[] initializeTasks = new ValueTask<bool>[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                string sceneName = names[i];
                initializeTasks[i] = LoadScene(sceneName);
            }

            foreach (var task in initializeTasks)
            {
                await task;
            }
        }

        private readonly SceneLoadData _data;
    }
}
