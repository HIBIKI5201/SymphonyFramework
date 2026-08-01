using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.System.ServiceLocate;
using SymphonyFrameWork.Utility;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary> Unity SceneManagerによるScene操作とルートObject初期化を担当する。 </summary>
    internal sealed class UnitySceneLoader : ISceneLoader
    {
        /// <inheritdoc />
        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        /// <inheritdoc />
        public IReadOnlyList<string> GetLoadedSceneNames()
        {
            int sceneCount = SceneManager.sceneCount;
            var sceneNames = new List<string>(sceneCount);

            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (IsLoadedScene(scene))
                {
                    sceneNames.Add(scene.name);
                }
            }

            return sceneNames;
        }

        /// <inheritdoc />
        public bool TryGetLoadedScene(string sceneName, out Scene scene)
        {
            scene = default;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            Scene candidate = SceneManager.GetSceneByName(sceneName);
            if (!IsLoadedScene(candidate))
            {
                return false;
            }

            scene = candidate;
            return true;
        }

        /// <inheritdoc />
        public bool TrySetActiveScene(string sceneName)
        {
            return TryGetLoadedScene(sceneName, out Scene scene)
                && SceneManager.SetActiveScene(scene);
        }

        /// <inheritdoc />
        public async ValueTask<bool> LoadSceneAsync(
            string sceneName,
            IProgress<float> progress,
            CancellationToken token)
        {
            AsyncOperation operation =
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError(
                    $"[{nameof(UnitySceneLoader)}] {sceneName} is not registered in Build Settings.");
                return false;
            }

            await SymphonyAwaitable.WaitWhile(
                () =>
                {
                    progress?.Report(operation.progress);
                    return !operation.isDone;
                },
                token);

            progress?.Report(1f);
            return TryGetLoadedScene(sceneName, out _);
        }

        /// <inheritdoc />
        public async ValueTask<bool> UnloadSceneAsync(
            string sceneName,
            IProgress<float> progress,
            CancellationToken token)
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError(
                    $"[{nameof(UnitySceneLoader)}] Failed to start unloading scene: {sceneName}.");
                return false;
            }

            await SymphonyAwaitable.WaitWhile(
                () =>
                {
                    progress?.Report(operation.progress);
                    return !operation.isDone;
                },
                token);

            progress?.Report(1f);
            return !TryGetLoadedScene(sceneName, out _);
        }

        /// <inheritdoc />
        public async ValueTask InitializeRootObjectsAsync(string sceneName)
        {
            if (!TryGetLoadedScene(sceneName, out Scene scene))
            {
                return;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();
            var initializeTasks = new List<Task>();

            foreach (GameObject rootObject in rootObjects)
            {
                rootObject.TryGetComponent(out IInjectable injectable);
                rootObject.TryGetComponent(out IInitializeAsync initializer);

                if (injectable != null || initializer != null)
                {
                    initializeTasks.Add(
                        InitializeRootObjectAsync(
                            sceneName,
                            rootObject,
                            injectable,
                            initializer));
                }
            }

            if (0 < initializeTasks.Count)
            {
                await Task.WhenAll(initializeTasks);
            }
        }

        /// <summary> ルートObjectへの依存注入と非同期初期化を文脈付きで実行する。 </summary>
        /// <param name="sceneName"> 初期化中のシーン名。 </param>
        /// <param name="rootObject"> 初期化するルートObject。 </param>
        /// <param name="injectable"> 依存注入を受ける実装。 </param>
        /// <param name="initializer"> 非同期初期化を行う実装。 </param>
        /// <returns> 初期化処理を表すTask。 </returns>
        private static async Task InitializeRootObjectAsync(
            string sceneName,
            GameObject rootObject,
            IInjectable injectable,
            IInitializeAsync initializer)
        {
            if (injectable != null)
            {
                try
                {
                    ServiceInjector.TryAutoInject(injectable);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new SceneInitializationException(
                        sceneName,
                        rootObject.name,
                        injectable.GetType(),
                        exception);
                }
            }

            if (initializer == null)
            {
                return;
            }

            try
            {
                await initializer.DoInitialize();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new SceneInitializationException(
                    sceneName,
                    rootObject.name,
                    initializer.GetType(),
                    exception);
            }
        }

        /// <summary> Unity Sceneが有効かつロード済みで、名前を持つか確認する。 </summary>
        /// <param name="scene"> 検証するScene。 </param>
        /// <returns> 有効なロード済みSceneの場合はtrue。 </returns>
        private static bool IsLoadedScene(Scene scene) =>
            scene.IsValid()
            && scene.isLoaded
            && !string.IsNullOrWhiteSpace(scene.name);
    }
}
