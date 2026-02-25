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

        public void ResetSceneData()
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
        /// <param name="sceneName">シーン名</param>
        /// <param name="loadingAction">ロードの進捗率を引数にしたメソッド</param>
        /// <param name="mode"></param>
        /// <param name="token"></param>
        /// <returns>ロードに成功したか</returns>
        public async ValueTask<bool> LoadScene(
            string sceneName,
            Action<float> loadingAction = null,
            LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken token = default)
        {
            //ロードしようとしているシーンが既に存在するか確認。
            if (_data.IsExistScene(sceneName))
            {
                Debug.LogWarning($"{sceneName} is already loaded.");
                return false;
            }

            #region ロード開始
            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (operation == null)
            {
                Debug.LogError($"{sceneName} is not register. check scene list of build setting.");
                return false;
            }

            _data.LoadStart(sceneName);

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
            Scene loadedScene = SceneManager.GetSceneByName(sceneName);

            //辞書にシーン名とシーン情報を保存。
            var isLoadSuccess = loadedScene.IsValid() && loadedScene.isLoaded;
            if (!isLoadSuccess)
            {
                Debug.LogWarning($"Failed Loading Scene: {sceneName}");
                _data.LoadFail(sceneName);
                return false;
            }

            //シングルロードの場合は辞書をクリアする。
            if (mode == LoadSceneMode.Single) 
            {
                _data.Reset(new KeyValuePair<string, Scene>(sceneName, loadedScene)); 
            }
            else
            {
                _data.LoadComplete(sceneName, loadedScene);
            }

            //ロード終了後にロード待ちしていたイベントを実行。
            _data.InvokeLoadedAction(sceneName);
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

        public async Task<bool> LoadScenes(
            string[] sceneNames,
            Action<float> loadingAction = null,
            CancellationToken token = default)
        {
            ValueTask<bool>[] loadTasks = new ValueTask<bool>[sceneNames.Length]; // シーンごとのロードタスク。
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

        private readonly SceneLoadData _data;
    }
}
