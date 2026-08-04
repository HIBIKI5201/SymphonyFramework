using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Utility;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary> Scene Loadの処理順、状態遷移、優先度判断を実行する。 </summary>
    internal sealed class SceneLoadService
    {
        /// <summary> 状態保存先とUnity Scene操作契約を指定してServiceを生成する。 </summary>
        /// <param name="registry"> シーンEntityの保存先。 </param>
        /// <param name="loader"> Unity Scene操作の契約。 </param>
        internal SceneLoadService(SceneLoadRegistry registry, ISceneLoader loader)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary> 観測可能なScene Load状態が変化したときに発行される。 </summary>
        internal event Action OnStateChanged;

        private readonly SceneLoadRegistry _registry;
        private readonly ISceneLoader _loader;

        /// <summary> 指定したロード済みSceneを取得し、追跡状態を同期する。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="scene"> 取得できたScene。 </param>
        /// <returns> 取得できた場合はtrue。 </returns>
        internal bool TryGetLoadedScene(string sceneName, out Scene scene)
        {
            scene = default;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            bool hasTrackedScene = _registry.TryGet(sceneName, out SceneLoadEntity entity);
            if (_loader.TryGetLoadedScene(sceneName, out scene))
            {
                if (!hasTrackedScene)
                {
                    _registry.RegisterLoaded(new SceneLoadRequest(sceneName));
                    NotifyStateChanged();
                }

                return true;
            }

            if (hasTrackedScene && _registry.Remove(sceneName))
            {
                NotifyStateChanged();
            }

            return false;
        }

        /// <summary> 指定したロード済みSceneをActive Sceneへ切り替える。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <returns> 切り替えられた場合はtrue。 </returns>
        internal bool TrySetActiveScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)
                || !_loader.TryGetLoadedScene(sceneName, out _))
            {
                if (_registry.Remove(sceneName))
                {
                    NotifyStateChanged();
                }

                return false;
            }

            if (!_registry.TryGet(sceneName, out SceneLoadEntity entity))
            {
                entity = _registry.RegisterLoaded(new SceneLoadRequest(sceneName));
                NotifyStateChanged();
            }

            if (!_loader.TrySetActiveScene(sceneName))
            {
                return false;
            }

            if (_registry.SetActiveScene(entity.Name))
            {
                NotifyStateChanged();
            }

            return true;
        }

        /// <summary> 既にロード済みのSceneを指定優先度で追跡登録する。 </summary>
        /// <param name="request"> シーン名と優先度。 </param>
        /// <returns> 登録できた場合はtrue。 </returns>
        internal bool TryRegisterLoadedScene(SceneLoadRequest request)
        {
            ValidateRequest(request, nameof(request));

            if (!_loader.TryGetLoadedScene(request.SceneName, out _))
            {
                if (_registry.Remove(request.SceneName))
                {
                    NotifyStateChanged();
                }

                return false;
            }

            _registry.RegisterLoaded(request);
            NotifyStateChanged();

            if (string.Equals(
                _loader.ActiveSceneName,
                request.SceneName,
                StringComparison.Ordinal)
                && _registry.SetActiveScene(request.SceneName))
            {
                NotifyStateChanged();
            }

            return true;
        }

        /// <summary> 指定したSceneをロードする。 </summary>
        /// <param name="request"> ロード対象と優先度。 </param>
        /// <param name="progress"> 進捗の通知先。 </param>
        /// <param name="mode"> AdditiveまたはSingle相当のロード方式。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> ロードに成功した場合はtrue。 </returns>
        internal async Task<bool> LoadScene(
            SceneLoadRequest request,
            IProgress<float> progress = null,
            LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken token = default)
        {
            ValidateRequest(request, nameof(request));

            bool hasTrackedScene =
                _registry.TryGet(request.SceneName, out SceneLoadEntity trackedEntity);
            if (hasTrackedScene
                && _loader.TryGetLoadedScene(request.SceneName, out _))
            {
                if (_registry.ActiveScenePriority <= trackedEntity.Priority)
                {
                    TrySetActiveScene(request.SceneName);
                }

                _registry.TakeLoadedAction(request.SceneName)?.Invoke();
                return true;
            }

            if (_loader.TryGetLoadedScene(request.SceneName, out _))
            {
                int priority = hasTrackedScene ? trackedEntity.Priority : request.Priority;
                SceneLoadEntity loadedEntity = _registry.RegisterLoaded(
                    new SceneLoadRequest(request.SceneName, priority));
                NotifyStateChanged();

                if (_registry.ActiveScenePriority <= loadedEntity.Priority)
                {
                    TrySetActiveScene(request.SceneName);
                }

                _registry.TakeLoadedAction(request.SceneName)?.Invoke();
                return true;
            }

            if (hasTrackedScene && _registry.Remove(request.SceneName))
            {
                NotifyStateChanged();
            }

            SceneLoadEntity entity = _registry.StartLoading(request);
            NotifyStateChanged();

            var serviceProgress = new SceneProgress(
                value =>
                {
                    if (entity.ReportProgress(value))
                    {
                        NotifyStateChanged();
                    }

                    progress?.Report(value);
                });

            bool isLoadSuccess;
            try
            {
                isLoadSuccess = await _loader.LoadSceneAsync(
                    request.SceneName,
                    serviceProgress,
                    token);
            }
            catch
            {
                _registry.Remove(request.SceneName);
                NotifyStateChanged();
                throw;
            }

            if (!isLoadSuccess)
            {
                _registry.Remove(request.SceneName);
                NotifyStateChanged();
                return false;
            }

            if (mode == LoadSceneMode.Single)
            {
                await ResetScene(request.SceneName, token);
            }

            entity.CompleteLoading();
            NotifyStateChanged();

            if (_registry.ActiveScenePriority <= request.Priority)
            {
                TrySetActiveScene(request.SceneName);
            }

            _registry.TakeLoadedAction(request.SceneName)?.Invoke();
            await _loader.InitializeRootObjectsAsync(request.SceneName);
            return true;
        }

        /// <summary> 指定した複数Sceneをロードする。 </summary>
        /// <param name="requests"> ロード対象と優先度の一覧。 </param>
        /// <param name="progress"> 平均進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> すべてロードできた場合はtrue。 </returns>
        internal async Task<bool> LoadScenes(
            SceneLoadRequest[] requests,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            ValidateRequests(requests, nameof(requests));

            Task<bool>[] loadTasks = new Task<bool>[requests.Length];
            float[] progresses = new float[requests.Length];

            for (int i = 0; i < requests.Length; i++)
            {
                int index = i;
                loadTasks[i] = LoadScene(
                    requests[i],
                    new SceneProgress(value => progresses[index] = value),
                    token: token);
            }

            await WaitForAll(
                loadTasks,
                progresses,
                progress,
                token);

            // WaitForAllで全件完了済みだが、faultedの場合に元例外をそのまま伝播させるため
            // Task.Resultではなくawaitで取り出す。Resultだとキャンセルや例外が
            // AggregateExceptionへ包まれ、ValueTask時代の伝播と変わってしまう。
            for (int i = 0; i < loadTasks.Length; i++)
            {
                if (!await loadTasks[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary> 指定したSceneをアンロードする。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="progress"> 進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> アンロードに成功した場合はtrue。 </returns>
        internal async Task<bool> UnloadScene(
            string sceneName,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            bool hasTrackedScene = _registry.TryGet(sceneName, out SceneLoadEntity entity);
            if (!_loader.TryGetLoadedScene(sceneName, out _))
            {
                if (_registry.Remove(sceneName))
                {
                    NotifyStateChanged();
                }

                return true;
            }

            if (!hasTrackedScene)
            {
                entity = _registry.RegisterLoaded(new SceneLoadRequest(sceneName));
                NotifyStateChanged();
            }

            entity.StartUnloading();
            NotifyStateChanged();

            var serviceProgress = new SceneProgress(
                value =>
                {
                    if (entity.ReportProgress(value))
                    {
                        NotifyStateChanged();
                    }

                    progress?.Report(value);
                });

            bool isUnloadSuccess = await _loader.UnloadSceneAsync(
                sceneName,
                serviceProgress,
                token);
            if (!isUnloadSuccess)
            {
                entity.CompleteLoading();
                NotifyStateChanged();
                return false;
            }

            _registry.Remove(sceneName);
            NotifyStateChanged();

            if (string.Equals(
                sceneName,
                _registry.ActiveSceneName,
                StringComparison.Ordinal))
            {
                if (_registry.TryGetHighestPriorityLoaded(out SceneLoadEntity nextActive))
                {
                    TrySetActiveScene(nextActive.Name);
                }
                else if (_registry.ClearActiveScene())
                {
                    NotifyStateChanged();
                }
            }

            return true;
        }

        /// <summary> 指定した複数Sceneをアンロードする。 </summary>
        /// <param name="sceneNames"> アンロード対象名の一覧。 </param>
        /// <param name="progress"> 平均進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> すべてアンロードできた場合はtrue。 </returns>
        internal async Task<bool> UnloadScenes(
            string[] sceneNames,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            ValidateSceneNames(sceneNames, nameof(sceneNames));

            Task<bool>[] unloadTasks = new Task<bool>[sceneNames.Length];
            float[] progresses = new float[sceneNames.Length];

            for (int i = 0; i < sceneNames.Length; i++)
            {
                int index = i;
                unloadTasks[i] = UnloadScene(
                    sceneNames[i],
                    new SceneProgress(value => progresses[index] = value),
                    token);
            }

            await WaitForAll(
                unloadTasks,
                progresses,
                progress,
                token);

            for (int i = 0; i < unloadTasks.Length; i++)
            {
                if (!await unloadTasks[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary> シーンのロード完了後に一度実行するcallbackを登録する。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="action"> 実行するcallback。 </param>
        internal void RegisterAfterSceneLoad(string sceneName, Action action)
        {
            if (_registry.RegisterLoadedAction(sceneName, action))
            {
                action.Invoke();
            }
        }

        /// <summary> 指定したSceneがロード完了状態になるまで待機する。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="token"> 待機を中断するトークン。 </param>
        /// <returns> 待機処理を表すTask。 </returns>
        internal async Task WaitForLoadSceneAsync(
            string sceneName,
            CancellationToken token = default)
        {
            while (!_registry.TryGet(sceneName, out SceneLoadEntity entity)
                || entity.State < SceneLoadStateEnum.Complete)
            {
                await Awaitable.NextFrameAsync(token);
            }
        }

        /// <summary> Unityのロード済みScene一覧からRegistryを同期する。 </summary>
        internal void SynchronizeLoadedScenes()
        {
            if (_registry.Synchronize(
                _loader.GetLoadedSceneNames(),
                _loader.ActiveSceneName))
            {
                NotifyStateChanged();
            }
        }

        /// <summary> 起動時設定に従ってSceneを整理し、初期Sceneをロードする。 </summary>
        /// <param name="isResetAndLoadOnPlay"> Sceneの整理と初期ロードを行うか。 </param>
        /// <param name="initializeSceneNames"> 初期ロードするScene名。 </param>
        /// <param name="resetIgnoreSceneNames"> 整理時に残すScene名。 </param>
        /// <returns> 起動時処理を表すTask。 </returns>
        internal async Task InitializeAfterSceneLoad(
            bool isResetAndLoadOnPlay,
            string[] initializeSceneNames,
            string[] resetIgnoreSceneNames)
        {
            SynchronizeLoadedScenes();

            if (!isResetAndLoadOnPlay
                || initializeSceneNames == null
                || initializeSceneNames.Length == 0)
            {
                return;
            }

            ValidateSceneNames(initializeSceneNames, nameof(initializeSceneNames));
            var scenesToKeep = new HashSet<string>(initializeSceneNames, StringComparer.Ordinal);
            if (resetIgnoreSceneNames != null)
            {
                for (int i = 0; i < resetIgnoreSceneNames.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(resetIgnoreSceneNames[i]))
                    {
                        scenesToKeep.Add(resetIgnoreSceneNames[i]);
                    }
                }
            }

            var unloadSceneNames = new List<string>();
            foreach (KeyValuePair<string, SceneLoadEntity> pair in _registry.Entities)
            {
                if (!scenesToKeep.Contains(pair.Key))
                {
                    unloadSceneNames.Add(pair.Key);
                }
            }

            if (0 < unloadSceneNames.Count)
            {
                await UnloadScenes(unloadSceneNames.ToArray());
            }

            var requests = new SceneLoadRequest[initializeSceneNames.Length];
            for (int i = 0; i < initializeSceneNames.Length; i++)
            {
                requests[i] = new SceneLoadRequest(initializeSceneNames[i]);
            }

            await LoadScenes(requests);
        }

        /// <summary> 指定したSceneだけを残し、それ以外をアンロードする。 </summary>
        /// <param name="sceneNameToKeep"> 残すScene名。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> Scene整理を表すTask。 </returns>
        private async Task ResetScene(
            string sceneNameToKeep,
            CancellationToken token)
        {
            var unloadSceneNames = new List<string>();
            foreach (KeyValuePair<string, SceneLoadEntity> pair in _registry.Entities)
            {
                if (!string.Equals(
                    pair.Key,
                    sceneNameToKeep,
                    StringComparison.Ordinal))
                {
                    unloadSceneNames.Add(pair.Key);
                }
            }

            if (0 < unloadSceneNames.Count)
            {
                await UnloadScenes(
                    unloadSceneNames.ToArray(),
                    token: token);
            }
        }

        /// <summary> 複数処理の完了を待ちながら平均進捗を通知する。 </summary>
        /// <param name="tasks"> 待機する処理。 </param>
        /// <param name="progresses"> Sceneごとの進捗。 </param>
        /// <param name="progress"> 平均進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> 待機処理を表すTask。 </returns>
        private static async Task WaitForAll(
            Task<bool>[] tasks,
            float[] progresses,
            IProgress<float> progress,
            CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                float totalProgress = 0f;
                for (int i = 0; i < progresses.Length; i++)
                {
                    totalProgress += progresses[i];
                }

                float averageProgress = totalProgress / progresses.Length;
                progress?.Report(averageProgress);

                bool allDone = true;
                for (int i = 0; i < tasks.Length; i++)
                {
                    if (!tasks[i].IsCompleted)
                    {
                        allDone = false;
                        break;
                    }
                }

                if (allDone)
                {
                    break;
                }

                await Awaitable.NextFrameAsync(token);
            }

            token.ThrowIfCancellationRequested();
            progress?.Report(1f);
        }

        /// <summary> Scene Load Requestが有効か検証する。 </summary>
        /// <param name="request"> 検証するRequest。 </param>
        /// <param name="parameterName"> 例外へ設定する引数名。 </param>
        private static void ValidateRequest(
            SceneLoadRequest request,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(request.SceneName))
            {
                throw new ArgumentException(
                    "シーン名を指定してください。",
                    parameterName);
            }
        }

        /// <summary> Scene Load Request一覧が有効か検証する。 </summary>
        /// <param name="requests"> 検証するRequest一覧。 </param>
        /// <param name="parameterName"> 例外へ設定する引数名。 </param>
        private static void ValidateRequests(
            SceneLoadRequest[] requests,
            string parameterName)
        {
            if (requests == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (requests.Length == 0)
            {
                throw new ArgumentException(
                    "ロード対象を1件以上指定してください。",
                    parameterName);
            }

            for (int i = 0; i < requests.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(requests[i].SceneName))
                {
                    throw new ArgumentException(
                        $"インデックス{i}のシーン名がnullまたは空です。",
                        parameterName);
                }
            }
        }

        /// <summary> Scene名一覧が有効か検証する。 </summary>
        /// <param name="sceneNames"> 検証するScene名一覧。 </param>
        /// <param name="parameterName"> 例外へ設定する引数名。 </param>
        private static void ValidateSceneNames(
            string[] sceneNames,
            string parameterName)
        {
            if (sceneNames == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (sceneNames.Length == 0)
            {
                throw new ArgumentException(
                    "シーン名を1件以上指定してください。",
                    parameterName);
            }

            for (int i = 0; i < sceneNames.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(sceneNames[i]))
                {
                    throw new ArgumentException(
                        $"インデックス{i}のシーン名がnullまたは空です。",
                        parameterName);
                }
            }
        }

        /// <summary> Scene Load状態の変更を購読者へ通知する。 </summary>
        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }

        /// <summary> delegateを同期的なIProgress通知として公開する。 </summary>
        private sealed class SceneProgress : IProgress<float>
        {
            /// <summary> 進捗通知先を指定して生成する。 </summary>
            /// <param name="report"> 進捗通知先。 </param>
            internal SceneProgress(Action<float> report)
            {
                _report = report ?? throw new ArgumentNullException(nameof(report));
            }

            private readonly Action<float> _report;

            /// <inheritdoc />
            public void Report(float value)
            {
                _report.Invoke(value);
            }
        }
    }
}
