using SymphonyFrameWork.Debugger;
using SymphonyFrameWork.Utility;
using System;
using System.Collections.Generic;
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
            await SymphonyTask.WaitUntil(() => operation.isDone, token);
            #endregion

            #region ロード完了後。
            Scene loadedScene = SceneManager.GetSceneByName(sceneName);

            //辞書にシーン名とシーン情報を保存。
            var isLoadSuccess = loadedScene.IsValid() && loadedScene.isLoaded;
            if (!isLoadSuccess)
            {
                Debug.LogWarning($"Failed Loading Scene: {sceneName}");
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

        private readonly SceneLoadData _data;
    }
}
