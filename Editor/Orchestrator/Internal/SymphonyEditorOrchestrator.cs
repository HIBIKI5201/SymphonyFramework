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
        #region 外部向けAPI

        /// <summary>
        ///     Editorモジュールの初期化を開始する。
        /// </summary>
        static SymphonyEditorOrchestrator()
        {
            // InitializeOnLoadからComposition Rootだけを起動し、各モジュールの初期化順を一元管理する。
            Initialize();
        }

        /// <summary>
        ///     設定アセットと生成物の整備を、Orchestratorの集約経路で実行する。
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         設定画面のように、Editor起動後に不足へ気づく入口のための再実行口である。
        ///         発見用属性のcallbackから直接 <c>PackageInitializer</c> を呼ぶと、
        ///         Asset変更が集約の外で起きて <c>AssetDatabase.Refresh</c> が余分に走る。
        ///     </para>
        ///     <para>
        ///         処理中の再入は dirty flag へ積むだけにして、実行中のバッチへ合流させる。
        ///         初期化中は初期化自身が同じ整備を行うため、ここでは何もしない。
        ///     </para>
        /// </remarks>
        /// <returns> 整備を実行できた場合はtrue。初期化が完了していない場合はfalse。 </returns>
        internal static bool RequestPackageSetup()
        {
            // 初期化中の呼び出しは、その初期化自身が同じ整備を行うため二重に走らせない。
            if (_state != EditorOrchestratorStateEnum.Ready) { return false; }

            _requiresAssetDatabaseRefresh |= PackageInitializer.Initialize();

            // 処理中ならフラグへ積むだけにする。実行中のバッチが同じRefreshで拾う。
            if (_isProcessingHostChanges) { return true; }

            ProcessPendingHostChanges();
            return true;
        }

        #endregion

        #region 内部処理

        private static readonly List<EditorModuleRegistration> _initializedModules = new();

        private static EditorOrchestratorStateEnum _state =
            EditorOrchestratorStateEnum.Uninitialized;
        private static bool _requiresAssetDatabaseRefresh;
        private static bool _isProcessingHostChanges;

        /// <summary>
        ///     Unity Editorのhost callbackとAssetPostprocessor通知を購読する。
        /// </summary>
        private static void SubscribeHostCallbacks()
        {
            // Domain Reload無効時の再初期化でも購読が重複しないよう、解除してから登録する。
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

        /// <summary>
        ///     Unity Editorのhost callbackとAssetPostprocessor通知を解除する。
        /// </summary>
        private static void UnsubscribeHostCallbacks()
        {
            // assembly reload前に旧デリゲートを切り離し、再ロード後の重複呼び出しを防ぐ。
            AssemblyReloadEvents.beforeAssemblyReload -= AssemblyReloadHandler;
            EditorApplication.quitting -= EditorQuittingHandler;
            EditorApplication.playModeStateChanged -= PlayModeStateChangedHandler;
            SymphonyAssetProtector.OnHostChangesPending -= HostChangesPendingHandler;
            TagsAndLayersPostProcessor.OnHostChangesPending -= HostChangesPendingHandler;
            AssetStoreToolsVersionPostProcessor.OnHostChangesPending -= HostChangesPendingHandler;
        }

        /// <summary>
        ///     assembly reload前にEditorモジュールを終了する。
        /// </summary>
        private static void AssemblyReloadHandler()
        {
            Shutdown();
        }

        /// <summary>
        ///     Unity Editor終了時にEditorモジュールを終了する。
        /// </summary>
        private static void EditorQuittingHandler()
        {
            Shutdown();
        }

        /// <summary>
        ///     後続RoundでPlay Mode遷移を処理するための入口。
        /// </summary>
        /// <param name="stateChange"> Unity EditorのPlay Mode遷移状態。 </param>
        private static void PlayModeStateChangedHandler(PlayModeStateChange stateChange)
        {
            // 現時点では購読経路だけを確立し、Play Mode遷移時の処理は行わない。
        }

        /// <summary>
        ///     host callbackの処理待ち通知をReady後の処理へ中継する。
        /// </summary>
        private static void HostChangesPendingHandler()
        {
            // 初期化中または処理中の再通知は、各モジュールの保留状態へ残して処理の再入を防ぐ。
            if (_state != EditorOrchestratorStateEnum.Ready || _isProcessingHostChanges) { return; }

            // host callbackへ例外を漏らさず、失敗したモジュールをConsoleで追跡可能にする。
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

        /// <summary>
        ///     Editorモジュールを依存順に初期化してReadyへ遷移する。
        /// </summary>
        private static void Initialize()
        {
            // InitializeOnLoadが再入しても、進行中または稼働中の初期化を重ねない。
            if (_state != EditorOrchestratorStateEnum.Uninitialized) { return; }

            // 失敗箇所を特定できるよう、状態遷移前から現在のモジュール名を追跡する。
            _state = EditorOrchestratorStateEnum.Initializing;
            _requiresAssetDatabaseRefresh = false;
            string initializingModuleName = nameof(SymphonyEditorOrchestrator);

            try
            {
                // 初期化中に発生したhost変更も失わないよう、モジュールより先に通知経路を確立する。
                SubscribeHostCallbacks();

                // Configと生成enum、Editor用依存を先に整え、後続モジュールが利用できる状態にする。
                initializingModuleName = nameof(PackageInitializer);
                _requiresAssetDatabaseRefresh |= PackageInitializer.Initialize();
                RecordInitializedModule(nameof(PackageInitializer), PackageInitializer.Shutdown);

                // 設定変更の監視を開始してから、変更を消費するenum生成側を初期化する。
                initializingModuleName = nameof(TagsAndLayersPostProcessor);
                TagsAndLayersPostProcessor.Initialize();
                RecordInitializedModule(
                    nameof(TagsAndLayersPostProcessor),
                    TagsAndLayersPostProcessor.Shutdown);

                initializingModuleName = nameof(AutoEnumGenerator);
                AutoEnumGenerator.Initialize();
                RecordInitializedModule(nameof(AutoEnumGenerator), AutoEnumGenerator.Shutdown);

                // パッケージ変更とログ出力は基盤の初期化後に開始し、終了時は逆順で解放する。
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

                // 全モジュールが揃ってからReadyへ遷移し、初期化中に積まれた変更をまとめて処理する。
                _state = EditorOrchestratorStateEnum.Ready;
                initializingModuleName = nameof(TagsAndLayersPostProcessor);
                ProcessPendingHostChanges();

                Debug.Log("Symphony Framework Initialized");
            }
            catch (Exception exception)
            {
                // 部分初期化のままEditorへ残さず、成功済みのモジュールだけを逆順で戻す。
                Debug.LogError(
                    $"[{nameof(SymphonyEditorOrchestrator)}] " +
                    $"{initializingModuleName}の初期化に失敗しました。" +
                    $" type: '{exception.GetType().FullName}', reason: '{exception.Message}'\n{exception}");
                RollbackInitialization();
            }
        }

        /// <summary>
        ///     初期化済みモジュールを逆順で終了する。
        /// </summary>
        private static void Shutdown()
        {
            // 未初期化または終了処理中なら、同じモジュールを重複して終了しない。
            if (_state == EditorOrchestratorStateEnum.Uninitialized ||
                _state == EditorOrchestratorStateEnum.ShuttingDown)
            {
                return;
            }

            // assembly reload前に購読を解除してから、依存関係と逆順にモジュールを終了する。
            _state = EditorOrchestratorStateEnum.ShuttingDown;
            UnsubscribeHostCallbacks();
            List<ModuleShutdownFailure> failures = ShutdownInitializedModules();
            ResetState();
            LogShutdownFailures(failures);
        }

        /// <summary>
        ///     初期化失敗時に成功済みモジュールだけを逆順で終了する。
        /// </summary>
        private static void RollbackInitialization()
        {
            // 新しい通知を止めてから、記録済みのモジュールだけを巻き戻す。
            _state = EditorOrchestratorStateEnum.ShuttingDown;
            UnsubscribeHostCallbacks();
            List<ModuleShutdownFailure> failures = ShutdownInitializedModules();
            ResetState();
            LogShutdownFailures(failures);
        }

        /// <summary>
        ///     初期化済みモジュールを逆順で終了し、失敗を収集する。
        /// </summary>
        /// <returns> 各モジュールの終了失敗。 </returns>
        private static List<ModuleShutdownFailure> ShutdownInitializedModules()
        {
            List<ModuleShutdownFailure> failures = new();
            // 初期化時の依存関係を壊さないよう、登録と逆順に終了する。
            for (int index = _initializedModules.Count - 1; index >= 0; index--)
            {
                EditorModuleRegistration module = _initializedModules[index];
                try
                {
                    module.Shutdown();
                }
                catch (Exception exception)
                {
                    // 1モジュールの失敗で後続の終了を止めず、最後にまとめて報告する。
                    failures.Add(new ModuleShutdownFailure(module.Name, exception));
                }
            }

            _initializedModules.Clear();
            return failures;
        }

        /// <summary>
        ///     coalesceしたhost callbackを処理して必要なRefreshを1回だけ実行する。
        /// </summary>
        private static void ProcessPendingHostChanges()
        {
            // AssetPostprocessorの再入通知で処理がネストしないよう、処理中フラグを先に立てる。
            _isProcessingHostChanges = true;
            try
            {
                // 設定変更から派生する生成処理を先に行い、必要なAssetDatabase.Refreshを1回へまとめる。
                TagsAndLayersPostProcessor.ProcessPendingChanges();
                _requiresAssetDatabaseRefresh |= AutoEnumGenerator.ConsumeAssetChanges();
                _requiresAssetDatabaseRefresh |=
                    AssetStoreToolsVersionPostProcessor.ProcessPendingChanges();

                // Refresh前の操作状態を破棄し、Refreshが生むcallbackも同じバッチ内で再度消費する。
                SymphonyAssetProtector.ProcessPendingChanges();
                RefreshAssetDatabaseIfRequired();
                SymphonyAssetProtector.ProcessPendingChanges();
            }
            finally
            {
                // 例外時も次のhost変更を処理できるよう、再入防止状態を必ず解除する。
                _isProcessingHostChanges = false;
            }
        }

        /// <summary>
        ///     実際のAsset変更が記録されている場合だけAssetDatabaseを更新する。
        /// </summary>
        private static void RefreshAssetDatabaseIfRequired()
        {
            // 変更が無いhost callbackでは、再インポートと追加callbackを発生させない。
            if (!_requiresAssetDatabaseRefresh) { return; }

            // Refreshが再入しても同じ要求を再消費しないよう、実行前にフラグを下ろす。
            _requiresAssetDatabaseRefresh = false;
            AssetDatabase.Refresh();
        }

        /// <summary>
        ///     Orchestratorの内部状態を未初期化へ戻す。
        /// </summary>
        private static void ResetState()
        {
            // 次のInitializeを初回と同じ状態から開始できるよう、host処理の状態をまとめて破棄する。
            _requiresAssetDatabaseRefresh = false;
            _isProcessingHostChanges = false;
            _state = EditorOrchestratorStateEnum.Uninitialized;
        }

        /// <summary>
        ///     複数モジュールの終了失敗を1件のエラーとして記録する。
        /// </summary>
        /// <param name="failures"> 記録する終了失敗。 </param>
        private static void LogShutdownFailures(IReadOnlyList<ModuleShutdownFailure> failures)
        {
            // 全モジュールを正常に終了できた場合は、不要なConsole出力を行わない。
            if (failures.Count == 0) { return; }

            // 終了処理を止めずに収集した失敗を、原因を失わない1件のログへまとめる。
            StringBuilder message = new();
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

        /// <summary>
        ///     初期化済みモジュールと終了処理を順序付きで記録する。
        /// </summary>
        /// <param name="name"> モジュールの型名。 </param>
        /// <param name="shutdown"> モジュールの終了処理。 </param>
        private static void RecordInitializedModule(string name, Action shutdown)
        {
            // 初期化に成功した順序を保持し、終了時に逆順で解放できるようにする。
            _initializedModules.Add(new EditorModuleRegistration(name, shutdown));
        }

        private enum EditorOrchestratorStateEnum
        {
            #region 外部向けAPI

            Uninitialized,
            Initializing,
            Ready,
            ShuttingDown,

            #endregion
        }

        private sealed class EditorModuleRegistration
        {
            #region 外部向けAPI

            /// <summary>
            ///     初期化済みモジュールの記録を生成する。
            /// </summary>
            /// <param name="name"> モジュールの型名。 </param>
            /// <param name="shutdown"> モジュールの終了処理。 </param>
            internal EditorModuleRegistration(string name, Action shutdown)
            {
                // 終了処理と診断用の名前を同じ登録単位へ保持する。
                Name = name;
                Shutdown = shutdown;
            }

            internal string Name { get; }
            internal Action Shutdown { get; }

            #endregion
        }

        private readonly struct ModuleShutdownFailure
        {
            #region 外部向けAPI

            /// <summary>
            ///     モジュールの終了失敗を生成する。
            /// </summary>
            /// <param name="moduleName"> モジュールの型名。 </param>
            /// <param name="exception"> 終了処理が送出した例外。 </param>
            internal ModuleShutdownFailure(string moduleName, Exception exception)
            {
                // 失敗したモジュールと例外を、終了処理後まで保持する。
                ModuleName = moduleName;
                Exception = exception;
            }

            internal string ModuleName { get; }
            internal Exception Exception { get; }

            #endregion
        }

        #endregion
    }
}
