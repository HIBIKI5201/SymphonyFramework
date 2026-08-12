using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Utility;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary>
    ///     Scene Loadの処理順、状態遷移、優先度判断を実行する。
    /// </summary>
    internal sealed class SceneLoadService
    {
        #region 外部向けAPI

        /// <summary>
        ///     状態保存先とUnity Scene操作契約を指定してServiceを生成する。
        /// </summary>
        /// <param name="registry"> シーンEntityの保存先。 </param>
        /// <param name="loader"> Unity Scene操作の契約。 </param>
        internal SceneLoadService(SceneLoadRegistry registry, ISceneLoader loader)
        {
            // 状態の所有者とUnity操作境界を同じServiceの生存期間へ固定する。
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary> 観測可能なScene Load状態が変化したときに発行される。 </summary>
        internal event Action OnStateChanged;

        /// <summary>
        ///     指定したロード済みSceneを取得し、追跡状態を同期する。
        /// </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="scene"> 取得できたScene。 </param>
        /// <returns> 取得できた場合はtrue。 </returns>
        internal bool TryGetLoadedScene(string sceneName, out Scene scene)
        {
            scene = default;
            // 名前が無い要求はUnityへ問い合わせず、追跡状態も変更しない。
            if (string.IsNullOrWhiteSpace(sceneName)) { return false; }

            // Unityの実状態を正とし、追跡漏れまたは古い追跡を照合する。
            bool hasTrackedScene = _registry.TryGet(sceneName, out SceneLoadEntity entity);
            if (_loader.TryGetLoadedScene(sceneName, out scene))
            {
                if (!hasTrackedScene)
                {
                    // Unityにだけ存在するSceneを完了状態として追跡へ追加する。
                    _registry.RegisterLoaded(new SceneLoadRequest(sceneName));
                    NotifyStateChanged();
                }

                return true;
            }

            // Unityに存在しない追跡は古いため削除し、表示状態も同期する。
            if (hasTrackedScene && _registry.Remove(sceneName)) { NotifyStateChanged(); }

            return false;
        }

        /// <summary>
        ///     指定したロード済みSceneをActive Sceneへ切り替える。
        /// </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <returns> 切り替えられた場合はtrue。 </returns>
        internal bool TrySetActiveScene(string sceneName)
        {
            // 名前が無いかUnityに存在しないSceneはActive化できず、残っている追跡も除去する。
            if (string.IsNullOrWhiteSpace(sceneName)
                || !_loader.TryGetLoadedScene(sceneName, out _))
            {
                if (_registry.Remove(sceneName)) { NotifyStateChanged(); }

                return false;
            }

            // Unityにだけ存在するSceneは、Active化前に完了状態として追跡へ追加する。
            if (!_registry.TryGet(sceneName, out SceneLoadEntity entity))
            {
                entity = _registry.RegisterLoaded(new SceneLoadRequest(sceneName));
                NotifyStateChanged();
            }

            // Unity側のActive Scene変更が失敗した場合はRegistryへ成功状態を記録しない。
            if (!_loader.TrySetActiveScene(sceneName)) { return false; }

            // Unityの変更が確定してからRegistryと表示状態を同期する。
            if (_registry.SetActiveScene(entity.Name)) { NotifyStateChanged(); }

            return true;
        }

        /// <summary>
        ///     既にロード済みのSceneを指定優先度で追跡登録する。
        /// </summary>
        /// <param name="request"> シーン名と優先度。 </param>
        /// <returns> 登録できた場合はtrue。 </returns>
        internal bool TryRegisterLoadedScene(SceneLoadRequest request)
        {
            // RegistryやUnityへ触れる前にRequestの同一性を検証する。
            ValidateRequest(request, nameof(request));

            // Unityに存在しないSceneは追跡へ残さず、登録失敗として返す。
            if (!_loader.TryGetLoadedScene(request.SceneName, out _))
            {
                if (_registry.Remove(request.SceneName)) { NotifyStateChanged(); }

                return false;
            }

            // Unityで確認できたSceneを完了状態として追跡し、優先度も更新する。
            _registry.RegisterLoaded(request);
            NotifyStateChanged();

            // UnityのActive Sceneと一致する場合だけ、Registry側のActive情報も同じ優先度へ揃える。
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

        /// <summary>
        ///     指定したSceneをロードする。
        /// </summary>
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
            // 状態を変更する前にRequestを検証し、無効な非同期処理を開始しない。
            ValidateRequest(request, nameof(request));

            // 追跡済みかつUnityにも存在するSceneは再ロードせず、優先度に応じてActive化する。
            bool hasTrackedScene =
                _registry.TryGet(request.SceneName, out SceneLoadEntity trackedEntity);
            if (hasTrackedScene
                && _loader.TryGetLoadedScene(request.SceneName, out _))
            {
                if (_registry.ActiveScenePriority <= trackedEntity.Priority) { TrySetActiveScene(request.SceneName); }

                // 既にロード済みでも、完了待ちcallbackは一度だけ解放する。
                _registry.TakeLoadedAction(request.SceneName)?.Invoke();
                return true;
            }

            // Unityにだけ存在するSceneは再ロードせず、追跡状態を完了へ同期する。
            if (_loader.TryGetLoadedScene(request.SceneName, out _))
            {
                int priority = hasTrackedScene ? trackedEntity.Priority : request.Priority;
                SceneLoadEntity loadedEntity = _registry.RegisterLoaded(
                    new SceneLoadRequest(request.SceneName, priority));
                NotifyStateChanged();

                if (_registry.ActiveScenePriority <= loadedEntity.Priority) { TrySetActiveScene(request.SceneName); }

                // 同期完了後に、登録済みの完了待ちcallbackを一度だけ解放する。
                _registry.TakeLoadedAction(request.SceneName)?.Invoke();
                return true;
            }

            // Unityに存在しない古い追跡は削除してから、新しいロード状態を登録する。
            if (hasTrackedScene && _registry.Remove(request.SceneName)) { NotifyStateChanged(); }

            SceneLoadEntity entity = _registry.StartLoading(request);
            NotifyStateChanged();

            // Unityの進捗をEntityと呼び出し側へ同じタイミングで反映する。
            SceneProgress serviceProgress = new(
                value =>
                {
                    if (entity.ReportProgress(value)) { NotifyStateChanged(); }

                    progress?.Report(value);
                });

            bool isLoadSuccess;
            try
            {
                // 指定トークンをUnity処理のフレーム待機へ伝播し、キャンセル時は例外をそのまま返す。
                isLoadSuccess = await _loader.LoadSceneAsync(
                    request.SceneName,
                    serviceProgress,
                    token);
            }
            catch
            {
                // 例外やキャンセルで未完了のEntityを追跡へ残さず、元の例外を維持する。
                _registry.Remove(request.SceneName);
                NotifyStateChanged();
                throw;
            }

            // Unityが失敗を返した場合も、未完了のEntityを追跡へ残さない。
            if (!isLoadSuccess)
            {
                _registry.Remove(request.SceneName);
                NotifyStateChanged();
                return false;
            }

            // Single相当では対象Sceneを先に確保してから、指定トークンで他Sceneの整理完了を待つ。
            if (mode == LoadSceneMode.Single) { await ResetScene(request.SceneName, token); }

            // Unityロードと必要なScene整理が完了してから、Entityを完了状態へ確定する。
            entity.CompleteLoading();
            NotifyStateChanged();

            // 現在のActive Scene以上の優先度なら、ロード済みSceneをActive候補として選ぶ。
            if (_registry.ActiveScenePriority <= request.Priority) { TrySetActiveScene(request.SceneName); }

            // 状態確定後に完了callbackを解放し、ロード用トークンを受け取らないルート初期化も完了まで待つ。
            _registry.TakeLoadedAction(request.SceneName)?.Invoke();
            await _loader.InitializeRootObjectsAsync(request.SceneName);
            return true;
        }

        /// <summary>
        ///     指定した複数Sceneをロードする。
        /// </summary>
        /// <param name="requests"> ロード対象と優先度の一覧。 </param>
        /// <param name="progress"> 平均進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> すべてロードできた場合はtrue。 </returns>
        internal async Task<bool> LoadScenes(
            SceneLoadRequest[] requests,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            // 一部だけ処理を開始しないよう、全Requestを開始前に検証する。
            ValidateRequests(requests, nameof(requests));

            Task<bool>[] loadTasks = new Task<bool>[requests.Length];
            float[] progresses = new float[requests.Length];

            // 各Sceneのロードを並行して開始し、指定トークンをすべての処理へ伝播する。
            for (int i = 0; i < requests.Length; i++)
            {
                int index = i;
                loadTasks[i] = LoadScene(
                    requests[i],
                    new SceneProgress(value => progresses[index] = value),
                    token: token);
            }

            // 全Taskの状態を監視しながら平均進捗を通知し、キャンセル時は待機を中断する。
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
                if (!await loadTasks[i]) { return false; }
            }

            return true;
        }

        /// <summary>
        ///     指定したSceneをアンロードする。
        /// </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="progress"> 進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> アンロードに成功した場合はtrue。 </returns>
        internal async Task<bool> UnloadScene(
            string sceneName,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            // 名前が無い要求はUnityへ渡さず、追跡状態も変更しない。
            if (string.IsNullOrWhiteSpace(sceneName)) { return false; }

            // Unityの実状態を正とし、追跡状態との食い違いを同期する。
            bool hasTrackedScene = _registry.TryGet(sceneName, out SceneLoadEntity entity);
            if (!_loader.TryGetLoadedScene(sceneName, out _))
            {
                if (_registry.Remove(sceneName)) { NotifyStateChanged(); }

                return true;
            }

            // Unityにだけ存在するSceneは、アンロード開始前に完了状態として追跡する。
            if (!hasTrackedScene)
            {
                entity = _registry.RegisterLoaded(new SceneLoadRequest(sceneName));
                NotifyStateChanged();
            }

            // アンロード中の状態を先に通知し、以後の進捗を同じEntityへ反映する。
            entity.StartUnloading();
            NotifyStateChanged();

            SceneProgress serviceProgress = new(
                value =>
                {
                    if (entity.ReportProgress(value)) { NotifyStateChanged(); }

                    progress?.Report(value);
                });

            // 指定トークンをUnity処理のフレーム待機へ伝播し、キャンセル時は例外をそのまま返す。
            bool isUnloadSuccess = await _loader.UnloadSceneAsync(
                sceneName,
                serviceProgress,
                token);

            // Unityが失敗を返した場合は、まだロード済みのEntityを完了状態へ戻す。
            if (!isUnloadSuccess)
            {
                entity.CompleteLoading();
                NotifyStateChanged();
                return false;
            }

            // Unityのアンロード成功後に追跡から除去し、表示状態を確定する。
            _registry.Remove(sceneName);
            NotifyStateChanged();

            // Active Sceneを失った場合は、残る完了済みSceneから次候補を選ぶ。
            if (string.Equals(
                sceneName,
                _registry.ActiveSceneName,
                StringComparison.Ordinal))
            {
                if (_registry.TryGetHighestPriorityLoaded(out SceneLoadEntity nextActive))
                {
                    TrySetActiveScene(nextActive.Name);
                }
                else if (_registry.ClearActiveScene()) { NotifyStateChanged(); }
            }

            return true;
        }

        /// <summary>
        ///     指定した複数Sceneをアンロードする。
        /// </summary>
        /// <param name="sceneNames"> アンロード対象名の一覧。 </param>
        /// <param name="progress"> 平均進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> すべてアンロードできた場合はtrue。 </returns>
        internal async Task<bool> UnloadScenes(
            string[] sceneNames,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            // 一部だけ処理を開始しないよう、全Scene名を開始前に検証する。
            ValidateSceneNames(sceneNames, nameof(sceneNames));

            Task<bool>[] unloadTasks = new Task<bool>[sceneNames.Length];
            float[] progresses = new float[sceneNames.Length];

            // 各Sceneのアンロードを並行して開始し、指定トークンをすべての処理へ伝播する。
            for (int i = 0; i < sceneNames.Length; i++)
            {
                int index = i;
                unloadTasks[i] = UnloadScene(
                    sceneNames[i],
                    new SceneProgress(value => progresses[index] = value),
                    token);
            }

            // 全Taskの状態を監視しながら平均進捗を通知し、キャンセル時は待機を中断する。
            await WaitForAll(
                unloadTasks,
                progresses,
                progress,
                token);

            for (int i = 0; i < unloadTasks.Length; i++)
            {
                if (!await unloadTasks[i]) { return false; }
            }

            return true;
        }

        /// <summary>
        ///     シーンのロード完了後に一度実行するcallbackを登録する。
        /// </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="action"> 実行するcallback。 </param>
        internal void RegisterAfterSceneLoad(string sceneName, Action action)
        {
            // 既に完了済みなら即時実行し、未完了ならRegistryへ一度だけの通知として保持する。
            if (_registry.RegisterLoadedAction(sceneName, action)) { action.Invoke(); }
        }

        /// <summary>
        ///     指定したSceneがロード完了状態になるまで待機する。
        /// </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="token"> 待機を中断するトークン。 </param>
        /// <returns> 待機処理を表すTask。 </returns>
        internal async Task WaitForLoadSceneAsync(
            string sceneName,
            CancellationToken token = default)
        {
            // 未追跡またはLoadingの間は次フレームまで待ち、指定トークンで待機を中断する。
            while (!_registry.TryGet(sceneName, out SceneLoadEntity entity)
                || entity.State < SceneLoadStateEnum.Complete)
            {
                await Awaitable.NextFrameAsync(token);
            }
        }

        /// <summary>
        ///     Unityのロード済みScene一覧からRegistryを同期する。
        /// </summary>
        internal void SynchronizeLoadedScenes()
        {
            // UnityのScene一覧とActive Sceneを同じ同期単位でRegistryへ反映する。
            if (_registry.Synchronize(
                _loader.GetLoadedSceneNames(),
                _loader.ActiveSceneName))
            {
                NotifyStateChanged();
            }
        }

        /// <summary>
        ///     起動時設定に従って初期Sceneをロードし、対象外のSceneを整理する。
        /// </summary>
        /// <param name="isResetAndLoadOnPlay"> Sceneの整理と初期ロードを行うか。 </param>
        /// <param name="initializeSceneNames"> 初期ロードするScene名。 </param>
        /// <param name="resetIgnoreSceneNames"> 整理時に残すScene名。 </param>
        /// <returns> 起動時処理を表すTask。 </returns>
        internal async Task InitializeAfterSceneLoad(
            bool isResetAndLoadOnPlay,
            string[] initializeSceneNames,
            string[] resetIgnoreSceneNames)
        {
            // 起動時点のUnity状態を先に取り込み、以後の整理対象を正しい一覧から決める。
            SynchronizeLoadedScenes();

            // リセット無効または初期Scene未指定なら、同期だけで起動時処理を終える。
            if (!isResetAndLoadOnPlay
                || initializeSceneNames == null
                || initializeSceneNames.Length == 0)
            {
                return;
            }

            ValidateSceneNames(initializeSceneNames, nameof(initializeSceneNames));
            HashSet<string> scenesToKeep = new(initializeSceneNames, StringComparer.Ordinal);

            // 無効な除外名は無視し、有効なSceneだけを保持対象へ追加する。
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

            // 初期Sceneと除外Sceneのどちらにも含まれない追跡中Sceneを整理対象にする。
            List<string> unloadSceneNames = new();
            foreach (KeyValuePair<string, SceneLoadEntity> pair in _registry.Entities)
            {
                if (!scenesToKeep.Contains(pair.Key)) { unloadSceneNames.Add(pair.Key); }
            }

            // 設定順を維持して初期SceneのRequestへ変換する。
            SceneLoadRequest[] requests = new SceneLoadRequest[initializeSceneNames.Length];
            for (int i = 0; i < initializeSceneNames.Length; i++)
            {
                requests[i] = new SceneLoadRequest(initializeSceneNames[i]);
            }

            // Unityは最後の1シーンをアンロードできないため、整理対象がロード済みシーンの
            // 全件になる構成ではアンロード先行が成立しない。ロードを先に行い、初期シーンが
            // 残ることを保証してからアンロードする。ロード済みシーンの数で順序を変えず、
            // どの構成でも同じ順序にする。LoadSceneMode.SingleのResetSceneとも順序が揃う。

            // 置き換え先が用意できていない状態で現在のシーンを捨てない。
            // ロード失敗自体はUnitySceneLoaderがエラーログで通知済み。
            if (!await LoadScenes(requests)) { return; }

            // 整理対象がある場合だけ、置き換え先のロード完了後にアンロードする。
            if (0 < unloadSceneNames.Count) { await UnloadScenes(unloadSceneNames.ToArray()); }
        }

        #endregion

        #region 内部処理

        private readonly SceneLoadRegistry _registry;
        private readonly ISceneLoader _loader;

        /// <summary>
        ///     指定したSceneだけを残し、それ以外をアンロードする。
        /// </summary>
        /// <param name="sceneNameToKeep"> 残すScene名。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> Scene整理を表すTask。 </returns>
        private async Task ResetScene(
            string sceneNameToKeep,
            CancellationToken token)
        {
            // 保持対象以外の追跡中Sceneを、現在のRegistryスナップショットから収集する。
            List<string> unloadSceneNames = new();
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

            // 整理対象がある場合だけ、指定トークンを各アンロードと集約待機へ伝播する。
            if (0 < unloadSceneNames.Count)
            {
                await UnloadScenes(
                    unloadSceneNames.ToArray(),
                    token: token);
            }
        }

        /// <summary>
        ///     複数処理の完了を待ちながら平均進捗を通知する。
        /// </summary>
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
            // キャンセル要求が届くまで、全Taskの完了と平均進捗をフレーム単位で監視する。
            while (!token.IsCancellationRequested)
            {
                // 各Sceneの最新進捗から呼び出し側へ通知する平均値を求める。
                float totalProgress = 0f;
                for (int i = 0; i < progresses.Length; i++) { totalProgress += progresses[i]; }

                float averageProgress = totalProgress / progresses.Length;
                progress?.Report(averageProgress);

                // faultedやcanceledも含め、全Taskが終端状態へ到達したかを確認する。
                bool allDone = true;
                for (int i = 0; i < tasks.Length; i++)
                {
                    if (!tasks[i].IsCompleted)
                    {
                        allDone = false;
                        break;
                    }
                }

                if (allDone) { break; }

                // 毎フレームの待機へ同じトークンを渡し、キャンセルを遅延させない。
                await Awaitable.NextFrameAsync(token);
            }

            // ループ条件で検出したキャンセルを成功完了として扱わず、呼び出し側へ伝播する。
            token.ThrowIfCancellationRequested();

            // 個別Taskの結果を取り出す前に、集約進捗の完了値を保証する。
            progress?.Report(1f);
        }

        /// <summary>
        ///     Scene Load Requestが有効か検証する。
        /// </summary>
        /// <param name="request"> 検証するRequest。 </param>
        /// <param name="parameterName"> 例外へ設定する引数名。 </param>
        private static void ValidateRequest(
            SceneLoadRequest request,
            string parameterName)
        {
            // Requestの既定値では対象を識別できないため、状態変更前に拒否する。
            if (string.IsNullOrWhiteSpace(request.SceneName))
            {
                throw new ArgumentException(
                    "シーン名を指定してください。",
                    parameterName);
            }
        }

        /// <summary>
        ///     Scene Load Request一覧が有効か検証する。
        /// </summary>
        /// <param name="requests"> 検証するRequest一覧。 </param>
        /// <param name="parameterName"> 例外へ設定する引数名。 </param>
        private static void ValidateRequests(
            SceneLoadRequest[] requests,
            string parameterName)
        {
            // nullの一覧は空配列と区別し、呼び出し契約違反として通知する。
            if (requests == null) { throw new ArgumentNullException(parameterName); }

            // 処理対象が無い呼び出しを成功として扱わない。
            if (requests.Length == 0)
            {
                throw new ArgumentException(
                    "ロード対象を1件以上指定してください。",
                    parameterName);
            }

            // 一部だけ処理を開始しないよう、全Requestを開始前に検証する。
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

        /// <summary>
        ///     Scene名一覧が有効か検証する。
        /// </summary>
        /// <param name="sceneNames"> 検証するScene名一覧。 </param>
        /// <param name="parameterName"> 例外へ設定する引数名。 </param>
        private static void ValidateSceneNames(
            string[] sceneNames,
            string parameterName)
        {
            // nullの一覧は空配列と区別し、呼び出し契約違反として通知する。
            if (sceneNames == null) { throw new ArgumentNullException(parameterName); }

            // 処理対象が無い呼び出しを成功として扱わない。
            if (sceneNames.Length == 0)
            {
                throw new ArgumentException(
                    "シーン名を1件以上指定してください。",
                    parameterName);
            }

            // 一部だけ処理を開始しないよう、全Scene名を開始前に検証する。
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

        /// <summary>
        ///     Scene Load状態の変更を購読者へ通知する。
        /// </summary>
        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }

        /// <summary>
        ///     delegateを同期的なIProgress通知として公開する。
        /// </summary>
        private sealed class SceneProgress : IProgress<float>
        {
            #region 外部向けAPI

            /// <summary>
            ///     進捗通知先を指定して生成する。
            /// </summary>
            /// <param name="report"> 進捗通知先。 </param>
            internal SceneProgress(Action<float> report)
            {
                // Entityと呼び出し側への進捗反映を同じ呼び出しスタックへ固定する。
                _report = report ?? throw new ArgumentNullException(nameof(report));
            }

            /// <inheritdoc />
            public void Report(float value)
            {
                _report.Invoke(value);
            }

            #endregion

            #region 内部処理

            private readonly Action<float> _report;

            #endregion
        }

        #endregion
    }
}
