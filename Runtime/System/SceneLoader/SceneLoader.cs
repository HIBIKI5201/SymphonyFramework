using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.Utility;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary> シーンのロード、アンロード、Active Scene選択を提供する公開エントリポイント。 </summary>
    public static class SceneLoader
    {
        private static SceneLoadRegistry _registry;
        private static SceneLoadService _service;
        private static SceneLoadQuery _query;
        private static SceneLoadViewModel _viewModel;

        /// <summary> ロードされているシーンを返す。 </summary>
        /// <param name="sceneName"> 取得するシーン名。 </param>
        /// <param name="scene"> 取得できたロード済みシーン。 </param>
        /// <returns> ロード済みシーンを取得できた場合はtrue。 </returns>
        public static bool GetExistScene(string sceneName, out Scene scene)
        {
            EnsureInitialized();
            return _service.TryGetLoadedScene(sceneName, out scene);
        }

        /// <summary> シーンが追跡中か確認する。 </summary>
        /// <param name="sceneName"> 存在を確認するシーン名。 </param>
        /// <returns> シーンが追跡中の場合はtrue。 </returns>
        public static bool IsExist(string sceneName)
        {
            EnsureInitialized();
            return _query.TryGetInfo(sceneName, out _);
        }

        /// <summary> シーンの状態を返す。 </summary>
        /// <param name="sceneName"> 状態を取得するシーン名。 </param>
        /// <param name="state"> 取得できたシーン状態。 </param>
        /// <returns> 状態を取得できた場合はtrue。 </returns>
        public static bool TryGetState(string sceneName, out SceneLoadStateEnum state)
        {
            EnsureInitialized();

            if (_query.TryGetInfo(sceneName, out SceneLoadInfo sceneInfo))
            {
                state = sceneInfo.State;
                return true;
            }

            state = SceneLoadStateEnum.None;
            return false;
        }

        /// <summary> 追跡中シーンの不変な状態スナップショット一覧を返す。 </summary>
        /// <returns> シーン名のordinal昇順で並んだ変更不能な一覧。 </returns>
        public static IReadOnlyList<SceneLoadInfo> GetSceneInfos()
        {
            EnsureInitialized();
            return _query.GetInfos();
        }

        /// <summary> 指定した追跡中シーンの不変な状態スナップショットを返す。 </summary>
        /// <param name="sceneName"> 状態を取得するシーン名。 </param>
        /// <param name="sceneInfo"> 取得できた状態スナップショット。 </param>
        /// <returns> 状態を取得できた場合はtrue。 </returns>
        public static bool TryGetSceneInfo(
            string sceneName,
            out SceneLoadInfo sceneInfo)
        {
            EnsureInitialized();
            return _query.TryGetInfo(sceneName, out sceneInfo);
        }

        /// <summary> シーンをActive Sceneにする。 </summary>
        /// <param name="sceneName"> Active Sceneにするロード済みシーン名。 </param>
        /// <returns> Active Sceneを変更できた場合はtrue。 </returns>
        public static bool SetActiveScene(string sceneName)
        {
            EnsureInitialized();
            return _service.TrySetActiveScene(sceneName);
        }

        /// <summary> 既にロード済みのシーンを指定優先度で追跡登録する。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="priority"> 優先度。 </param>
        /// <returns> 登録に成功した場合はtrue。 </returns>
        public static bool RegisterLoadedScene(string sceneName, int priority)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            return _service.TryRegisterLoadedScene(
                new SceneLoadRequest(sceneName, priority));
        }

        /// <summary> Scene Load Requestに従ってシーンをロードする。 </summary>
        /// <param name="request"> ロードするシーン名と優先度。 </param>
        /// <param name="progress"> ロード進捗の通知先。 </param>
        /// <param name="mode"> AdditiveまたはSingle相当のロード方式。 </param>
        /// <param name="token"> ロード処理を中断するためのトークン。 </param>
        /// <returns> ロードに成功した場合はtrue。 </returns>
        /// <exception cref="ArgumentException"> リクエストのシーン名がnull、空、空白の場合。 </exception>
        /// <exception cref="SceneInitializationException"> ロード後の依存注入または非同期初期化に失敗した場合。 </exception>
        public static Awaitable<bool> LoadSceneAsync(
            SceneLoadRequest request,
            IProgress<float> progress = null,
            LoadSceneMode mode = LoadSceneMode.Additive,
            CancellationToken token = default)
        {
            EnsureInitialized();
            ValidateRequest(request, nameof(request));

            return SymphonyAwaitable.FromTask(
                _service.LoadScene(
                    request,
                    progress,
                    mode,
                    token));
        }

        /// <summary> シーンをロードする。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="loadingAction"> ロード進捗を受け取る処理。 </param>
        /// <param name="mode"> AdditiveまたはSingle相当のロード方式。 </param>
        /// <param name="priority"> ロード後のActive Scene選択に使用する優先度。 </param>
        /// <param name="token"> ロード処理を中断するためのトークン。 </param>
        /// <returns> ロードに成功した場合はtrue。 </returns>
        /// <exception cref="ArgumentException"> シーン名がnull、空、空白の場合。 </exception>
        /// <exception cref="SceneInitializationException"> ロード後の依存注入または非同期初期化に失敗した場合。 </exception>
        public static Awaitable<bool> LoadSceneAsync(
            string sceneName,
            Action<float> loadingAction = null,
            LoadSceneMode mode = LoadSceneMode.Additive,
            int priority = 0,
            CancellationToken token = default)
        {
            EnsureInitialized();

            var request = new SceneLoadRequest(sceneName, priority);
            return SymphonyAwaitable.FromTask(
                _service.LoadScene(
                    request,
                    WrapProgress(loadingAction),
                    mode,
                    token));
        }

        /// <summary> 複数のScene Load Requestをロードする。 </summary>
        /// <param name="requests"> ロードするシーン名と優先度の一覧。 </param>
        /// <param name="progress"> 全シーンの平均進捗の通知先。 </param>
        /// <param name="token"> ロード処理を中断するためのトークン。 </param>
        /// <returns> すべてロードできた場合はtrue。 </returns>
        /// <exception cref="ArgumentNullException"> リクエスト一覧がnullの場合。 </exception>
        /// <exception cref="ArgumentException"> リクエスト一覧が空、または無効なシーン名を含む場合。 </exception>
        /// <exception cref="SceneInitializationException"> ロード後の依存注入または非同期初期化に失敗した場合。 </exception>
        public static Awaitable<bool> LoadScenesAsync(
            SceneLoadRequest[] requests,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            EnsureInitialized();
            ValidateRequests(requests);
            return SymphonyAwaitable.FromTask(
                _service.LoadScenes(requests, progress, token));
        }

        /// <summary> 複数のシーンをロードする。 </summary>
        /// <param name="sceneNames"> ロードするシーン名の一覧。 </param>
        /// <param name="loadingAction"> 全シーンの平均進捗を受け取る処理。 </param>
        /// <param name="token"> ロード処理を中断するためのトークン。 </param>
        /// <returns> すべてロードできた場合はtrue。 </returns>
        /// <exception cref="ArgumentNullException"> シーン名一覧がnullの場合。 </exception>
        /// <exception cref="ArgumentException"> シーン名一覧が空、または無効なシーン名を含む場合。 </exception>
        /// <exception cref="SceneInitializationException"> ロード後の依存注入または非同期初期化に失敗した場合。 </exception>
        public static Awaitable<bool> LoadScenesAsync(
            string[] sceneNames,
            Action<float> loadingAction = null,
            CancellationToken token = default)
        {
            EnsureInitialized();
            ValidateSceneNames(sceneNames);

            var requests = new SceneLoadRequest[sceneNames.Length];
            for (int i = 0; i < sceneNames.Length; i++)
            {
                requests[i] = new SceneLoadRequest(sceneNames[i]);
            }

            return SymphonyAwaitable.FromTask(
                _service.LoadScenes(
                    requests,
                    WrapProgress(loadingAction),
                    token));
        }

        /// <summary> シーンをアンロードする。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="loadingAction"> アンロード進捗を受け取る処理。 </param>
        /// <param name="token"> アンロード処理を中断するためのトークン。 </param>
        /// <returns> アンロードに成功した場合はtrue。 </returns>
        /// <exception cref="ArgumentException"> シーン名がnull、空、空白の場合。 </exception>
        public static Awaitable<bool> UnloadSceneAsync(
            string sceneName,
            Action<float> loadingAction = null,
            CancellationToken token = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("シーン名を指定してください。", nameof(sceneName));
            }

            return SymphonyAwaitable.FromTask(
                _service.UnloadScene(
                    sceneName,
                    WrapProgress(loadingAction),
                    token));
        }

        /// <summary> 複数のシーンをアンロードする。 </summary>
        /// <param name="sceneNames"> アンロードするシーン名の一覧。 </param>
        /// <param name="loadingAction"> 全シーンの平均進捗を受け取る処理。 </param>
        /// <param name="token"> アンロード処理を中断するためのトークン。 </param>
        /// <returns> すべてアンロードできた場合はtrue。 </returns>
        /// <exception cref="ArgumentNullException"> シーン名一覧がnullの場合。 </exception>
        /// <exception cref="ArgumentException"> シーン名一覧が空、または無効なシーン名を含む場合。 </exception>
        public static Awaitable<bool> UnloadScenesAsync(
            string[] sceneNames,
            Action<float> loadingAction = null,
            CancellationToken token = default)
        {
            EnsureInitialized();
            ValidateSceneNames(sceneNames);

            return SymphonyAwaitable.FromTask(
                _service.UnloadScenes(
                    sceneNames,
                    WrapProgress(loadingAction),
                    token));
        }

        /// <summary> シーンのロード完了後に一度実行するcallbackを登録する。 </summary>
        /// <param name="sceneName"> ロード完了を監視するシーン名。 </param>
        /// <param name="action"> ロード完了後に実行する処理。 </param>
        public static void RegisterAfterSceneLoad(string sceneName, Action action)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("シーン名を指定してください。", nameof(sceneName));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _service.RegisterAfterSceneLoad(sceneName, action);
        }

        /// <summary> 指定したシーンがロードされるまで待機する。 </summary>
        /// <param name="sceneName"> ロード完了を待機するシーン名。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 待機処理を表すAwaitable。 </returns>
        public static Awaitable WaitForLoadSceneAsync(
            string sceneName,
            CancellationToken token = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("シーン名を指定してください。", nameof(sceneName));
            }

            return SymphonyAwaitable.FromTask(
                _service.WaitForLoadSceneAsync(sceneName, token));
        }

        /// <summary> Scene Loaderが初期化済みかどうか。 </summary>
        internal static bool IsInitialized =>
            _service != null
            && _registry != null
            && _query != null
            && _viewModel != null;

        /// <summary> Compositionが所有する現在のScene Load ViewModel。 </summary>
        internal static SceneLoadViewModel CurrentViewModel => _viewModel;

        /// <summary> OrchestratorからScene Loaderを初期化する。 </summary>
        internal static void Initialize()
        {
            ResetRuntimeState();
            _registry = new SceneLoadRegistry();
            _service = new SceneLoadService(_registry, new UnitySceneLoader());
            _query = new SceneLoadQuery(_registry);
            _viewModel = new SceneLoadViewModel(_query, _service);
        }

        /// <summary> 追跡状態とServiceを破棄して未初期化状態へ戻す。 </summary>
        internal static void ResetRuntimeState()
        {
            _viewModel?.Dispose();
            _registry?.Clear();
            _viewModel = null;
            _query = null;
            _service = null;
            _registry = null;
        }

        /// <summary> 初期Sceneロード後に起動時設定を適用する。 </summary>
        /// <param name="config"> 起動時のScene整理とロード設定。 </param>
        /// <returns> 起動時処理を表すTask。 </returns>
        internal static Task AfterSceneLoad(SceneLoadConfig config)
        {
            EnsureInitialized();

            return _service.InitializeAfterSceneLoad(
                config?.IsResetAndLoadOnPlay ?? false,
                config?.InitializeSceneList,
                config?.ResetIgnoreSceneList);
        }

        /// <summary> Scene Loaderが利用可能な状態か検証する。 </summary>
        private static void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new SymphonyNotInitializedException(typeof(SceneLoader));
            }
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
        private static void ValidateRequests(SceneLoadRequest[] requests)
        {
            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            if (requests.Length == 0)
            {
                throw new ArgumentException(
                    "ロード対象を1件以上指定してください。",
                    nameof(requests));
            }

            for (int i = 0; i < requests.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(requests[i].SceneName))
                {
                    throw new ArgumentException(
                        $"インデックス{i}のシーン名がnullまたは空です。",
                        nameof(requests));
                }
            }
        }

        /// <summary> Scene名一覧が有効か検証する。 </summary>
        /// <param name="sceneNames"> 検証するScene名一覧。 </param>
        private static void ValidateSceneNames(string[] sceneNames)
        {
            if (sceneNames == null)
            {
                throw new ArgumentNullException(nameof(sceneNames));
            }

            if (sceneNames.Length == 0)
            {
                throw new ArgumentException(
                    "シーン名を1件以上指定してください。",
                    nameof(sceneNames));
            }

            for (int i = 0; i < sceneNames.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(sceneNames[i]))
                {
                    throw new ArgumentException(
                        $"インデックス{i}のシーン名がnullまたは空です。",
                        nameof(sceneNames));
                }
            }
        }

        /// <summary> Action進捗通知を同期実行するIProgressへ変換する。 </summary>
        /// <param name="action"> 既存APIから受け取った進捗通知。 </param>
        /// <returns> 変換した通知先。null入力の場合はnull。 </returns>
        private static IProgress<float> WrapProgress(Action<float> action) =>
            action == null ? null : new ActionProgress(action);

        /// <summary> Actionを同じ呼び出しスタックで実行する進捗adapter。 </summary>
        private sealed class ActionProgress : IProgress<float>
        {
            /// <summary> 通知先Actionを指定してadapterを生成する。 </summary>
            /// <param name="action"> 通知先。 </param>
            internal ActionProgress(Action<float> action)
            {
                _action = action ?? throw new ArgumentNullException(nameof(action));
            }

            private readonly Action<float> _action;

            /// <inheritdoc />
            public void Report(float value)
            {
                _action.Invoke(value);
            }
        }
    }
}
