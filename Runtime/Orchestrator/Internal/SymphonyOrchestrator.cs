using System;
using System.Collections.Generic;
using System.Threading;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.HUD;
using SymphonyFrameWork.System;
using SymphonyFrameWork.System.SaveSystem;
using SymphonyFrameWork.System.SceneLoad;
using SymphonyFrameWork.System.ServiceLocate;

using UnityEngine;

namespace SymphonyFrameWork.Orchestrator
{
    /// <summary>
    ///     SymphonyFrameWorkの永続オブジェクトを管理する、パッケージ全体のComposition Rootです。
    /// </summary>
    internal static class SymphonyOrchestrator
    {
        private static readonly List<Action> _resetActions = new();

        private static CancellationTokenRegistration _destroyRegistration;
        private static bool _isShuttingDown;
        private static SymphonyLifetimeComponent _systemObject;

        /// <summary>
        ///     永続オブジェクトを生成し、各サブシステムを初期化する。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void GameBeforeSceneLoaded()
        {
            Shutdown();
            DestroySystemObject();
            _isShuttingDown = false;

            ISystemObjectFactory systemObjectFactory = new SystemObjectFactory();

            try
            {
                GameObject systemGameObject = new(nameof(SymphonyOrchestrator));
                _systemObject = systemGameObject.AddComponent<SymphonyLifetimeComponent>();
                UnityEngine.Object.DontDestroyOnLoad(systemGameObject);

                SaveDataInitializer.Initialize(ResolveSaveDataLoader);
                RecordInitializedSubsystem(SaveStore.ResetRuntimeState);

                PauseManager.Initialize();
                RecordInitializedSubsystem(PauseManager.ResetRuntimeState);

                ServiceHostComponent serviceHost =
                    systemObjectFactory.CreateComponent<ServiceHostComponent>(
                        nameof(ServiceHostComponent));
                ServiceLocator.Initialize(serviceHost);
                RecordInitializedSubsystem(ServiceLocator.ResetRuntimeState);

                SceneLoader.Initialize();
                RecordInitializedSubsystem(SceneLoader.ResetRuntimeState);

                AudioManager.Initialize(
                    SymphonyConfigLocator.GetConfig<AudioConfig>(),
                    systemObjectFactory);
                RecordInitializedSubsystem(AudioManager.ResetRuntimeState);

                SymphonyDebugHUD.Initialize(systemObjectFactory);
                RecordInitializedSubsystem(SymphonyDebugHUD.ResetRuntimeState);

                _destroyRegistration =
                    _systemObject.destroyCancellationToken.Register(Shutdown);

                GC.Collect();
            }
            catch
            {
                Shutdown();
                DestroySystemObject();
                throw;
            }
        }

        /// <summary> 初期シーンのロード後にシーン管理を開始する。 </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void GameAfterSceneLoaded()
        {
            SceneLoadConfig config =
                SymphonyConfigLocator.GetConfig<SceneLoadConfig>();
            _ = SceneLoader.AfterSceneLoad(config);
        }

        /// <summary>
        ///     現在のConfigからセーブデータローダーを解決する。
        /// </summary>
        /// <returns> Configで選択されたローダー。未設定の場合は既定のローダー。 </returns>
        private static SaveDataLoaderStrategy ResolveSaveDataLoader()
        {
            SaveDataConfig config =
                SymphonyConfigLocator.GetConfig<SaveDataConfig>();
            return config?.Loader ?? new JsonUtilitySaveDataLoaderStrategy();
        }

        /// <summary> 初期化済みサブシステムの終了処理を構築順に記録する。 </summary>
        /// <param name="resetAction"> サブシステムの終了処理。 </param>
        private static void RecordInitializedSubsystem(Action resetAction)
        {
            _resetActions.Add(resetAction);
        }

        /// <summary> 初期化済みサブシステムを構築順の逆順で解放する。 </summary>
        private static void Shutdown()
        {
            if (_isShuttingDown)
            {
                return;
            }

            _isShuttingDown = true;
            List<Exception> exceptions = null;

            for (int i = _resetActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    _resetActions[i]();
                }
                catch (Exception exception)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(exception);
                }
            }

            _resetActions.Clear();
            try
            {
                _destroyRegistration.Dispose();
            }
            catch (Exception exception)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(exception);
            }
            finally
            {
                _destroyRegistration = default;
            }

            if (exceptions != null)
            {
                Debug.LogException(new AggregateException(
                    $"[{nameof(SymphonyOrchestrator)}] サブシステムの終了処理で例外が発生しました。",
                    exceptions));
            }
        }

        /// <summary> 既存のOrchestrator用GameObjectを破棄する。 </summary>
        private static void DestroySystemObject()
        {
            if (_systemObject)
            {
                UnityEngine.Object.Destroy(_systemObject.gameObject);
            }

            _systemObject = null;
        }
    }
}
