using System;
using System.Collections.Generic;
using System.Text;

using SymphonyFrameWork.Editor.Debugger.Logger;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Unity Editorのhost lifecycleをEditorモジュールの初期化と終了へ変換する。
    /// </summary>
    [InitializeOnLoad]
    internal static class SymphonyEditorOrchestrator
    {
        static SymphonyEditorOrchestrator()
        {
            Initialize();
        }

        private static readonly List<EditorModuleRegistration> _initializedModules = new();

        private static EditorOrchestratorStateEnum _state =
            EditorOrchestratorStateEnum.Uninitialized;
        private static bool _requiresAssetDatabaseRefresh;
        private static bool _isProcessingHostChanges;

        /// <summary> Unity Editorのhost callbackとAssetPostprocessor通知を購読する。 </summary>
        private static void SubscribeHostCallbacks()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= AssemblyReloadHandler;
            AssemblyReloadEvents.beforeAssemblyReload += AssemblyReloadHandler;
            EditorApplication.quitting -= EditorQuittingHandler;
            EditorApplication.quitting += EditorQuittingHandler;
            EditorApplication.playModeStateChanged -= PlayModeStateChangedHandler;
            EditorApplication.playModeStateChanged += PlayModeStateChangedHandler;
            SymphonyAssetProtector.OnHostChangesPending -= HostChangesPendingHandler;
            SymphonyAssetProtector.OnHostChangesPending += HostChangesPendingHandler;
            TagsAndLayersPostProcessor.OnHostChangesPending -= HostChangesPendingHandler;
            TagsAndLayersPostProcessor.OnHostChangesPending += HostChangesPendingHandler;
            AssetStoreToolsVersionPostProcessor.OnHostChangesPending -= HostChangesPendingHandler;
            AssetStoreToolsVersionPostProcessor.OnHostChangesPending += HostChangesPendingHandler;
        }

        /// <summary> Unity Editorのhost callbackとAssetPostprocessor通知を解除する。 </summary>
        private static void UnsubscribeHostCallbacks()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= AssemblyReloadHandler;
            EditorApplication.quitting -= EditorQuittingHandler;
            EditorApplication.playModeStateChanged -= PlayModeStateChangedHandler;
            SymphonyAssetProtector.OnHostChangesPending -= HostChangesPendingHandler;
            TagsAndLayersPostProcessor.OnHostChangesPending -= HostChangesPendingHandler;
            AssetStoreToolsVersionPostProcessor.OnHostChangesPending -= HostChangesPendingHandler;
        }

        /// <summary> assembly reload前にEditorモジュールを終了する。 </summary>
        private static void AssemblyReloadHandler()
        {
            Shutdown();
        }

        /// <summary> Unity Editor終了時にEditorモジュールを終了する。 </summary>
        private static void EditorQuittingHandler()
        {
            Shutdown();
        }

        /// <summary> 後続RoundでPlay Mode遷移を処理するための入口。 </summary>
        /// <param name="stateChange"> Unity EditorのPlay Mode遷移状態。 </param>
        private static void PlayModeStateChangedHandler(PlayModeStateChange stateChange)
        {
        }

        /// <summary> host callbackの処理待ち通知をReady後の処理へ中継する。 </summary>
        private static void HostChangesPendingHandler()
        {
            if (_state != EditorOrchestratorStateEnum.Ready || _isProcessingHostChanges)
            {
                return;
            }

            try
            {
                ProcessPendingHostChanges();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[{nameof(SymphonyEditorOrchestrator)}] host callbackの処理に失敗しました。" +
                    $" type: '{exception.GetType().FullName}', reason: '{exception.Message}'\n{exception}");
            }
        }

        /// <summary> Editorモジュールを依存順に初期化してReadyへ遷移する。 </summary>
        private static void Initialize()
        {
            if (_state != EditorOrchestratorStateEnum.Uninitialized)
            {
                return;
            }

            _state = EditorOrchestratorStateEnum.Initializing;
            _requiresAssetDatabaseRefresh = false;
            string initializingModuleName = nameof(SymphonyEditorOrchestrator);

            try
            {
                SubscribeHostCallbacks();

                initializingModuleName = nameof(PackageInitializer);
                _requiresAssetDatabaseRefresh |= PackageInitializer.Initialize();
                RecordInitializedModule(nameof(PackageInitializer), PackageInitializer.Shutdown);

                initializingModuleName = nameof(TagsAndLayersPostProcessor);
                TagsAndLayersPostProcessor.Initialize();
                RecordInitializedModule(
                    nameof(TagsAndLayersPostProcessor),
                    TagsAndLayersPostProcessor.Shutdown);

                initializingModuleName = nameof(AutoEnumGenerator);
                AutoEnumGenerator.Initialize();
                RecordInitializedModule(nameof(AutoEnumGenerator), AutoEnumGenerator.Shutdown);

                initializingModuleName = nameof(AssetStoreToolsVersionPostProcessor);
                AssetStoreToolsVersionPostProcessor.Initialize();
                RecordInitializedModule(
                    nameof(AssetStoreToolsVersionPostProcessor),
                    AssetStoreToolsVersionPostProcessor.Shutdown);

                initializingModuleName = nameof(SymphonyDebugLogFileWriter);
                SymphonyDebugLogFileWriter.Initialize();
                RecordInitializedModule(
                    nameof(SymphonyDebugLogFileWriter),
                    SymphonyDebugLogFileWriter.Shutdown);

                _state = EditorOrchestratorStateEnum.Ready;
                initializingModuleName = nameof(TagsAndLayersPostProcessor);
                ProcessPendingHostChanges();

                Debug.Log("Symphony Framework Initialized");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[{nameof(SymphonyEditorOrchestrator)}] " +
                    $"{initializingModuleName}の初期化に失敗しました。" +
                    $" type: '{exception.GetType().FullName}', reason: '{exception.Message}'\n{exception}");
                RollbackInitialization();
            }
        }

        /// <summary> 初期化済みモジュールを逆順で終了する。 </summary>
        private static void Shutdown()
        {
            if (_state == EditorOrchestratorStateEnum.Uninitialized ||
                _state == EditorOrchestratorStateEnum.ShuttingDown)
            {
                return;
            }

            _state = EditorOrchestratorStateEnum.ShuttingDown;
            UnsubscribeHostCallbacks();
            List<ModuleShutdownFailure> failures = ShutdownInitializedModules();
            ResetState();
            LogShutdownFailures(failures);
        }

        /// <summary> 初期化失敗時に成功済みモジュールだけを逆順で終了する。 </summary>
        private static void RollbackInitialization()
        {
            _state = EditorOrchestratorStateEnum.ShuttingDown;
            UnsubscribeHostCallbacks();
            List<ModuleShutdownFailure> failures = ShutdownInitializedModules();
            ResetState();
            LogShutdownFailures(failures);
        }

        /// <summary> 初期化済みモジュールを逆順で終了し、失敗を収集する。 </summary>
        /// <returns> 各モジュールの終了失敗。 </returns>
        private static List<ModuleShutdownFailure> ShutdownInitializedModules()
        {
            var failures = new List<ModuleShutdownFailure>();
            for (int index = _initializedModules.Count - 1; index >= 0; index--)
            {
                EditorModuleRegistration module = _initializedModules[index];
                try
                {
                    module.Shutdown();
                }
                catch (Exception exception)
                {
                    failures.Add(new ModuleShutdownFailure(module.Name, exception));
                }
            }

            _initializedModules.Clear();
            return failures;
        }

        /// <summary> coalesceしたhost callbackを処理して必要なRefreshを1回だけ実行する。 </summary>
        private static void ProcessPendingHostChanges()
        {
            _isProcessingHostChanges = true;
            try
            {
                TagsAndLayersPostProcessor.ProcessPendingChanges();
                _requiresAssetDatabaseRefresh |= AutoEnumGenerator.ConsumeAssetChanges();
                _requiresAssetDatabaseRefresh |=
                    AssetStoreToolsVersionPostProcessor.ProcessPendingChanges();
                SymphonyAssetProtector.ProcessPendingChanges();
                RefreshAssetDatabaseIfRequired();
                SymphonyAssetProtector.ProcessPendingChanges();
            }
            finally
            {
                _isProcessingHostChanges = false;
            }
        }

        /// <summary> 実際のAsset変更が記録されている場合だけAssetDatabaseを更新する。 </summary>
        private static void RefreshAssetDatabaseIfRequired()
        {
            if (!_requiresAssetDatabaseRefresh)
            {
                return;
            }

            _requiresAssetDatabaseRefresh = false;
            AssetDatabase.Refresh();
        }

        /// <summary> Orchestratorの内部状態を未初期化へ戻す。 </summary>
        private static void ResetState()
        {
            _requiresAssetDatabaseRefresh = false;
            _isProcessingHostChanges = false;
            _state = EditorOrchestratorStateEnum.Uninitialized;
        }

        /// <summary> 複数モジュールの終了失敗を1件のエラーとして記録する。 </summary>
        /// <param name="failures"> 記録する終了失敗。 </param>
        private static void LogShutdownFailures(IReadOnlyList<ModuleShutdownFailure> failures)
        {
            if (failures.Count == 0)
            {
                return;
            }

            var message = new StringBuilder();
            message.Append($"[{nameof(SymphonyEditorOrchestrator)}] ");
            message.AppendLine("Editorモジュールの終了処理で例外が発生しました。");
            foreach (ModuleShutdownFailure failure in failures)
            {
                message.Append("- ");
                message.Append(failure.ModuleName);
                message.Append(": type='");
                message.Append(failure.Exception.GetType().FullName);
                message.Append("', reason='");
                message.Append(failure.Exception.Message);
                message.AppendLine("'");
            }

            Debug.LogError(message.ToString());
        }

        /// <summary> 初期化済みモジュールと終了処理を順序付きで記録する。 </summary>
        /// <param name="name"> モジュールの型名。 </param>
        /// <param name="shutdown"> モジュールの終了処理。 </param>
        private static void RecordInitializedModule(string name, Action shutdown)
        {
            _initializedModules.Add(new EditorModuleRegistration(name, shutdown));
        }

        private enum EditorOrchestratorStateEnum
        {
            Uninitialized,
            Initializing,
            Ready,
            ShuttingDown,
        }

        private sealed class EditorModuleRegistration
        {
            /// <summary> 初期化済みモジュールの記録を生成する。 </summary>
            /// <param name="name"> モジュールの型名。 </param>
            /// <param name="shutdown"> モジュールの終了処理。 </param>
            internal EditorModuleRegistration(string name, Action shutdown)
            {
                Name = name;
                Shutdown = shutdown;
            }

            internal string Name { get; }
            internal Action Shutdown { get; }
        }

        private readonly struct ModuleShutdownFailure
        {
            /// <summary> モジュールの終了失敗を生成する。 </summary>
            /// <param name="moduleName"> モジュールの型名。 </param>
            /// <param name="exception"> 終了処理が送出した例外。 </param>
            internal ModuleShutdownFailure(string moduleName, Exception exception)
            {
                ModuleName = moduleName;
                Exception = exception;
            }

            internal string ModuleName { get; }
            internal Exception Exception { get; }
        }
    }
}
