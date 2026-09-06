using System;
using System.Collections.Generic;
using System.Threading;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.HUD;
using SymphonyFrameWork.Debugger.Logger;
using SymphonyFrameWork.System;
using SymphonyFrameWork.System.SaveSystem;
using SymphonyFrameWork.System.SceneBlock;
using SymphonyFrameWork.System.SceneLoad;
using SymphonyFrameWork.System.ServiceLocate;

using UnityEngine;

namespace SymphonyFrameWork.Orchestrator
{
    /// <summary>
    ///     SymphonyFrameWorkの永続オブジェクトを管理するComposition Root。
    /// </summary>
    internal static class SymphonyOrchestrator
    {
        #region 外部向けAPI

        /// <summary>
        ///     永続オブジェクトを生成し、各サブシステムを初期化する。
        /// </summary>
        /// <remarks>
        ///     Runtime初期化属性をこのComposition Rootだけに限定し、全サブシステムの順序と終了登録を一元管理する。
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void GameBeforeSceneLoaded()
        {
            // Domain Reload無効では前回のstatic状態が残るため、既存状態と永続オブジェクトを破棄してから再構築する。
            Shutdown();
            DestroySystemObject();
            _isShuttingDown = false;

            ISystemObjectFactory systemObjectFactory = new SystemObjectFactory();

            // Coreは下位アセンブリにありRuntimeのロガーを参照できない。中継口へ出力先を注入し、
            // Core層のログもフレームワーク共通の集約先（Editorのファイル出力）へ載せる。
            // **Shutdownでは解除しない。** Editor側も同じ中継口へ注入しており、Play Mode終了で
            // 解除すると、Edit Modeで動くEditorウィンドウのCoreログが集約先から外れる。
            // Editorでの解除はassembly reloadに合わせてPackageInitializerが行う。
            CoreLogRelay.ExceptionHandler = exception => SymphonyDebugLogger.LogException(exception);

            try
            {
                // 全サブシステムの終了を1つのUnityライフタイムへ集約する。
                GameObject systemGameObject = new(nameof(SymphonyOrchestrator));
                _systemObject = systemGameObject.AddComponent<SymphonyLifetimeComponent>();
                UnityEngine.Object.DontDestroyOnLoad(systemGameObject);

                // OnEnableとStartの間で実行するフレームワークの同期フェーズをPlayerLoopへ挿入する。
                // ここへ登録した処理は、その回のUpdate.ScriptRunBehaviourUpdate（Start呼び出しを含む）より必ず先に走る。
                SymphonyFrameworkLifecycleLoop.Install(RunFrameworkLifecycleTick);

                // Save → Pause → Service → Scene → Audio → HUDの起動契約を保ち、途中失敗時も成功済み処理を逆順で戻せるようにする。
                SaveDataInitializer.Initialize(ResolveSaveDataLoader);
                RecordInitializedSubsystem(SaveStore.ResetRuntimeState);

                PauseManager.Initialize();
                RecordInitializedSubsystem(PauseManager.ResetRuntimeState);

                ServiceHostComponent serviceHost =
                    systemObjectFactory.CreateComponent<ServiceHostComponent>(
                        nameof(ServiceHostComponent));
                ServiceLocator.Initialize(serviceHost);
                RecordInitializedSubsystem(ServiceLocator.ResetRuntimeState);
                RecordFrameworkTick(ServiceLocator.FlushPendingRegistrations);

                SceneLoader.Initialize();
                RecordInitializedSubsystem(SceneLoader.ResetRuntimeState);

                // Scene BlockはScene Loadの操作を土台にするため、必ずその後段で初期化する。
                SceneBlockLoader.Initialize(SceneLoader.CurrentService);
                RecordInitializedSubsystem(SceneBlockLoader.ResetRuntimeState);

                AudioManager.Initialize(
                    SymphonyConfigLocator.GetConfig<AudioConfig>(),
                    systemObjectFactory);
                RecordInitializedSubsystem(AudioManager.ResetRuntimeState);

                DebugHUDConfig debugHUDConfig =
                    SymphonyConfigLocator.GetConfig<DebugHUDConfig>();
                SymphonyDebugHUD.Initialize(
                    systemObjectFactory,
                    debugHUDConfig?.ToggleAction,
                    Debug.isDebugBuild,
                    new DebugHUDViewModel());
                RecordInitializedSubsystem(SymphonyDebugHUD.ResetRuntimeState);

                // package-wideな終了通知をOrchestratorだけが購読し、全サブシステムを一括して逆順に終了する。
                _destroyRegistration =
                    _systemObject.destroyCancellationToken.Register(Shutdown);

                // 起動時の一時オブジェクトを初期シーン処理の前に回収する。
                GC.Collect();
            }
            catch
            {
                // 初期化途中の失敗でも、成功済みサブシステムと永続オブジェクトを残さない。
                Shutdown();
                DestroySystemObject();
                throw;
            }
        }

        /// <summary>
        ///     初期シーンのロード後にシーン管理を開始する。
        /// </summary>
        /// <remarks>
        ///     Runtime初期化属性をこのComposition Rootだけに限定し、シーン管理の開始順を一元管理する。
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void GameAfterSceneLoaded()
        {
            // InspectorとProject Settingsで構成された内部設定を、初期シーンの準備完了後に適用する。
            SceneLoadConfig config =
                SymphonyConfigLocator.GetConfig<SceneLoadConfig>();

            // Runtime初期化コールバックは同期入口のため、以後の非同期処理をSceneLoaderへ引き渡す。
            _ = SceneLoader.AfterSceneLoad(config);
        }

        #endregion

        #region 内部処理

        private static readonly List<Action> _resetActions = new();
        private static readonly List<Action> _frameworkTickActions = new();

        private static CancellationTokenRegistration _destroyRegistration;
        private static bool _isShuttingDown;
        private static SymphonyLifetimeComponent _systemObject;

        /// <summary>
        ///     現在のConfigからセーブデータローダーを解決する。
        /// </summary>
        /// <returns> Configで選択されたローダー。未設定の場合は既定のローダー。 </returns>
        private static SaveDataLoaderStrategy ResolveSaveDataLoader()
        {
            // 利用側コードへinternalなConfigを公開せず、InspectorとProject Settingsの選択結果だけを解決する。
            SaveDataConfig config =
                SymphonyConfigLocator.GetConfig<SaveDataConfig>();

            // ConfigまたはLoaderが未設定の場合も、既定実装で初期化を継続する。
            return config?.Loader ?? new JsonUtilitySaveDataLoaderStrategy();
        }

        /// <summary>
        ///     初期化済みサブシステムの終了処理を構築順に記録する。
        /// </summary>
        /// <param name="resetAction"> サブシステムの終了処理。 </param>
        private static void RecordInitializedSubsystem(Action resetAction)
        {
            // 後から逆順に辿ることで、利用側を依存先より先に解放できるよう構築順を保持する。
            _resetActions.Add(resetAction);
        }

        /// <summary>
        ///     OnEnableとStartの間で毎フレーム実行する処理を登録順に記録する。
        /// </summary>
        /// <param name="tickAction"> Start呼び出しの直前に実行する処理。 </param>
        private static void RecordFrameworkTick(Action tickAction)
        {
            _frameworkTickActions.Add(tickAction);
        }

        /// <summary>
        ///     登録済みの同期フェーズ処理を順に実行する。
        /// </summary>
        /// <remarks>
        ///     <see cref="SymphonyFrameworkLifecycleLoop"/>からUpdate.ScriptRunBehaviourUpdateの直前に呼ばれる。
        /// </remarks>
        private static void RunFrameworkLifecycleTick()
        {
            // 1件の失敗で残りの同期処理を止めない。Shutdownの逆順解放と同じ例外集約方針にする。
            for (int i = 0; i < _frameworkTickActions.Count; i++)
            {
                try
                {
                    _frameworkTickActions[i]();
                }
                catch (Exception exception)
                {
                    SymphonyDebugLogger.LogException(exception);
                }
            }
        }

        /// <summary>
        ///     初期化済みサブシステムを構築順の逆順で解放する。
        /// </summary>
        private static void Shutdown()
        {
            // 終了通知の重複や終了処理からの再入では、同じサブシステムを二重に解放しない。
            if (_isShuttingDown) { return; }

            _isShuttingDown = true;
            List<Exception> exceptions = null;

            // 同期フェーズを先に取り除き、解放処理の途中でフレームワークのtickが割り込まないようにする。
            SymphonyFrameworkLifecycleLoop.Uninstall();
            _frameworkTickActions.Clear();

            // 構築時の依存関係を壊さないよう、初期化に成功した順序の逆から解放する。
            for (int i = _resetActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    _resetActions[i]();
                }
                catch (Exception exception)
                {
                    // 1つの終了失敗で残りの解放を止めず、全処理後にまとめて記録する。
                    exceptions ??= new List<Exception>();
                    exceptions.Add(exception);
                }
            }

            _resetActions.Clear();

            // 終了通知の購読を解除し、Domain Reload無効でも次回Play Modeへ登録を持ち越さない。
            try
            {
                _destroyRegistration.Dispose();
            }
            catch (Exception exception)
            {
                // 購読解除の失敗も他の終了例外と同じ集約へ含める。
                exceptions ??= new List<Exception>();
                exceptions.Add(exception);
            }
            finally
            {
                // Disposeの成否にかかわらず、次回初期化が古い登録を参照しない状態へ戻す。
                _destroyRegistration = default;
            }

            // すべての終了処理を試した後に、発生した例外を1件のログへ集約する。
            if (exceptions != null)
            {
                SymphonyDebugLogger.LogException(new AggregateException(
                    $"[{nameof(SymphonyOrchestrator)}] サブシステムの終了処理で例外が発生しました。",
                    exceptions));
            }

        }

        /// <summary>
        ///     既存のOrchestrator用GameObjectを破棄する。
        /// </summary>
        private static void DestroySystemObject()
        {
            // Domain Reload無効で前回の永続オブジェクトが残っている場合だけ破棄する。
            if (_systemObject) { UnityEngine.Object.Destroy(_systemObject.gameObject); }

            // Unityの遅延破棄完了前でも、static参照を次回Play Modeへ持ち越さない。
            _systemObject = null;
        }

        #endregion
    }
}
