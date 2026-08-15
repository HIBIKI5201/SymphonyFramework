using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Debugger.Logger;
using SymphonyFrameWork.System.ServiceLocate;
using SymphonyFrameWork.Utility;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary>
    ///     Unity SceneManagerによるScene操作とルートObject初期化を担当する。
    /// </summary>
    internal sealed class UnitySceneLoader : ISceneLoader
    {
        #region 外部向けAPI

        /// <inheritdoc />
        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        /// <inheritdoc />
        public IReadOnlyList<string> GetLoadedSceneNames()
        {
            // 呼び出し時点のScene数を固定し、Unityの状態から独立した名前一覧へ切り出す。
            int sceneCount = SceneManager.sceneCount;
            List<string> sceneNames = new(sceneCount);

            // 無効またはロード未完了のSceneをApplication層へ公開しない。
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (IsLoadedScene(scene)) { sceneNames.Add(scene.name); }
            }

            return sceneNames;
        }

        /// <inheritdoc />
        public bool TryGetLoadedScene(string sceneName, out Scene scene)
        {
            scene = default;
            // 名前が無い要求はUnityへ問い合わせず、未取得として扱う。
            if (string.IsNullOrWhiteSpace(sceneName)) { return false; }

            // Unityが返すSceneは、存在だけでなくロード完了まで確認してから公開する。
            Scene candidate = SceneManager.GetSceneByName(sceneName);
            if (!IsLoadedScene(candidate)) { return false; }

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
        public async Task<bool> LoadSceneAsync(
            string sceneName,
            IProgress<float> progress,
            CancellationToken token)
        {
            // Application層のロード方式をAdditiveへ固定し、Single相当の整理はServiceで行う。
            AsyncOperation operation =
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                SymphonyDebugLogger.LogDirect(
                    $"[{nameof(UnitySceneLoader)}] {sceneName} is not registered in Build Settings.", LogKindEnum.Error);
                return false;
            }

            // Unityの非同期処理が完了するまで毎フレーム進捗を通知し、指定トークンで待機だけを中断する。
            await SymphonyAwaitable.WaitWhile(
                () =>
                {
                    progress?.Report(operation.progress);
                    return !operation.isDone;
                },
                token);

            // Unityの完了通知後に最終進捗を保証し、実際のロード状態を結果にする。
            progress?.Report(1f);
            return TryGetLoadedScene(sceneName, out _);
        }

        /// <inheritdoc />
        public async Task<bool> UnloadSceneAsync(
            string sceneName,
            IProgress<float> progress,
            CancellationToken token)
        {
            // Unityがアンロード処理を開始できない場合は、待機へ進まず失敗を返す。
            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation == null)
            {
                SymphonyDebugLogger.LogDirect(
                    $"[{nameof(UnitySceneLoader)}] Failed to start unloading scene: {sceneName}.", LogKindEnum.Error);
                return false;
            }

            // Unityの非同期処理が完了するまで毎フレーム進捗を通知し、指定トークンで待機だけを中断する。
            await SymphonyAwaitable.WaitWhile(
                () =>
                {
                    progress?.Report(operation.progress);
                    return !operation.isDone;
                },
                token);

            // Unityの完了通知後に最終進捗を保証し、実際にSceneが消えたことを結果にする。
            progress?.Report(1f);
            return !TryGetLoadedScene(sceneName, out _);
        }

        /// <inheritdoc />
        public async Task InitializeRootObjectsAsync(string sceneName)
        {
            // ロード済みでないSceneには初期化対象が存在しないため何もしない。
            if (!TryGetLoadedScene(sceneName, out Scene scene)) { return; }

            // この契約はトークンを受け取らないため、ルート単位の初期化を収集してすべての完了を待つ。
            GameObject[] rootObjects = scene.GetRootGameObjects();
            List<Task> initializeTasks = new();

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

            // 対象がある場合だけ全初期化の完了を待ち、いずれかの失敗を呼び出し側へ伝播する。
            if (0 < initializeTasks.Count) { await Task.WhenAll(initializeTasks); }
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     ルートObjectへの依存注入と非同期初期化を文脈付きで実行する。
        /// </summary>
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
            // 依存注入を先に完了させ、初期化処理が注入済みの依存を利用できるようにする。
            if (injectable != null)
            {
                try
                {
                    ServiceInjector.TryAutoInject(injectable);
                }
                catch (OperationCanceledException)
                {
                    // キャンセルは初期化失敗へ包まず、呼び出し側のキャンセル制御へそのまま返す。
                    throw;
                }
                catch (Exception exception)
                {
                    // 失敗したScene、Object、実装型を保持して原因を特定できるようにする。
                    throw new SceneInitializationException(
                        sceneName,
                        rootObject.name,
                        injectable.GetType(),
                        exception);
                }
            }

            // 非同期初期化を実装していないルートObjectは、依存注入だけで処理を終える。
            if (initializer == null) { return; }

            // 依存注入後に非同期初期化を待ち、完了前にSceneロード成功を返さない。
            try
            {
                await initializer.DoInitialize();
            }
            catch (OperationCanceledException)
            {
                // キャンセルは初期化失敗へ包まず、呼び出し側のキャンセル制御へそのまま返す。
                throw;
            }
            catch (Exception exception)
            {
                // 失敗したScene、Object、実装型を保持して原因を特定できるようにする。
                throw new SceneInitializationException(
                    sceneName,
                    rootObject.name,
                    initializer.GetType(),
                    exception);
            }
        }

        /// <summary>
        ///     Unity Sceneが有効かつロード済みで、名前を持つか確認する。
        /// </summary>
        /// <param name="scene"> 検証するScene。 </param>
        /// <returns> 有効なロード済みSceneの場合はtrue。 </returns>
        private static bool IsLoadedScene(Scene scene) =>
            scene.IsValid()
            && scene.isLoaded
            && !string.IsNullOrWhiteSpace(scene.name);

        #endregion
    }
}
