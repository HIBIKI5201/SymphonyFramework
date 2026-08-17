using System;
using System.IO;

using SymphonyFrameWork.Debugger.Logger;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     シーン一覧、タグ、レイヤーの変更を監視する。
    /// </summary>
    /// <remarks>
    ///     host callbackでは変更を記録してSymphonyEditorOrchestratorへ通知し、
    ///     実際の変更通知はOrchestratorがcoalesceして処理する。
    /// </remarks>
    public sealed class TagsAndLayersPostProcessor : AssetPostprocessor
    {
        #region 外部向けAPI

        /// <summary> host callbackによる変更が処理待ちになったときに通知する。 </summary>
        internal static event Action OnHostChangesPending;

        /// <summary> Build Settingsのシーン一覧に対する変更通知。 </summary>
        public static TagsAndLayersSettingData SceneList = new();

        /// <summary> TagManagerのタグ設定に対する変更通知。 </summary>
        public static TagsAndLayersSettingData Tags = new("ProjectSettings/TagManager.asset");

        /// <summary> TagManagerのレイヤー設定に対する変更通知。 </summary>
        public static TagsAndLayersSettingData Layers = new("ProjectSettings/TagManager.asset");

        /// <summary>
        ///     Build Settingsのシーン一覧変更イベントを購読する。
        /// </summary>
        internal static void Initialize()
        {
            // Domain Reload無効時の再初期化でも購読が重複しないよう、解除してから登録する。
            EditorBuildSettings.sceneListChanged -= SceneListChangedHandler;
            EditorBuildSettings.sceneListChanged += SceneListChangedHandler;
        }

        /// <summary>
        ///     Build Settingsのシーン一覧変更イベントを解除する。
        /// </summary>
        internal static void Shutdown()
        {
            // assembly reload前に購読と処理待ちを破棄し、旧状態を次の初期化へ持ち越さない。
            EditorBuildSettings.sceneListChanged -= SceneListChangedHandler;
            _hasPendingSceneListChange = false;
            _hasPendingTagManagerChange = false;
        }

        /// <summary>
        ///     coalesceした設定変更を各変更通知へ1回ずつ中継する。
        /// </summary>
        internal static void ProcessPendingChanges()
        {
            // callback中の再入で同じ変更を再消費しないよう、処理対象を退避して保留状態を先に下ろす。
            bool hasSceneListChange = _hasPendingSceneListChange;
            bool hasTagManagerChange = _hasPendingTagManagerChange;
            _hasPendingSceneListChange = false;
            _hasPendingTagManagerChange = false;

            // Build Settingsの変更はファイル比較を持たないため、保留された通知をそのまま中継する。
            if (hasSceneListChange) { SceneList.EventInvoke(); }

            // TagManagerの変更が無いバッチでは、設定ファイルの読み込みを発生させない。
            if (!hasTagManagerChange) { return; }

            // 同じTagManager.assetを共有するタグとレイヤーを個別の前回値で比較する。
            foreach (TagsAndLayersSettingData setting in new[] { Tags, Layers })
            {
                string newContent = File.ReadAllText(setting.TagManagerPath);

                // 対象外の変更でファイル内容が同じなら、利用側へ誤った変更通知を送らない。
                if (newContent == setting.LastManagerContent) { continue; }

                // 次回比較の基準を更新してから通知し、購読者の再入でも同じ内容を再通知しない。
                setting.SetLastManagerContent(newContent);
                setting.EventInvoke();
            }
        }

        /// <summary>
        ///     監視対象設定ファイルの内容と変更通知を保持する。
        /// </summary>
        public sealed class TagsAndLayersSettingData : IDisposable
        {
            #region 外部向けAPI

            /// <summary>
            ///     ファイルを持たない手動通知用の監視データを生成する。
            /// </summary>
            public TagsAndLayersSettingData() => _managerPath = string.Empty;

            /// <summary>
            ///     指定した設定ファイルの初期内容を読み込んで監視データを生成する。
            /// </summary>
            public TagsAndLayersSettingData(string tagManagerPath)
            {
                // ファイルが存在すれば初期内容を比較基準にし、無ければ監視できないことを通知する。
                if (File.Exists(tagManagerPath))
                {
                    _managerPath = tagManagerPath;
                    _lastManagerContent = File.ReadAllText(tagManagerPath);
                }
                else { SymphonyDebugLogger.LogDirect($"{tagManagerPath}にアセットがありません。", LogKindEnum.Warning); }
            }

            /// <summary> 監視対象の設定が変更されたときに発生する。 </summary>
            public event Action OnSettingChanged;

            /// <summary> 監視対象のProjectSettingsファイルパス。 </summary>
            public string TagManagerPath { get => _managerPath; }

            /// <summary> 前回確認した設定ファイルの内容。 </summary>
            public string LastManagerContent { get => _lastManagerContent; }

            /// <summary>
            ///     比較基準となる直近の設定ファイル内容を更新する。
            /// </summary>
            public void SetLastManagerContent(string content) => _lastManagerContent = content;

            /// <summary>
            ///     設定変更イベントを通知する。
            /// </summary>
            public void EventInvoke() => OnSettingChanged?.Invoke();

            /// <summary>
            ///     登録済みの変更通知をすべて解除する。
            /// </summary>
            public void Dispose() => OnSettingChanged = null;

            #endregion

            #region 内部処理

            private string _managerPath = string.Empty;
            private string _lastManagerContent = string.Empty;

            #endregion
        }

        #endregion

        #region 内部処理

        private static bool _hasPendingSceneListChange;
        private static bool _hasPendingTagManagerChange;

        /// <summary>
        ///     Build Settingsのシーン一覧変更を処理待ちとして記録する。
        /// </summary>
        private static void SceneListChangedHandler()
        {
            // callback内で生成処理を行わず、Orchestratorが他のhost変更とまとめて処理できるよう通知する。
            _hasPendingSceneListChange = true;
            OnHostChangesPending?.Invoke();
        }

        /// <summary>
        ///     TagManagerアセットの変更を処理待ちとして記録する。
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // 同一バッチの複数importは1件へ集約し、対象ファイルを見つけた時点で走査を終える。
            foreach (string asset in importedAssets)
            {
                if (asset == Tags.TagManagerPath)
                {
                    _hasPendingTagManagerChange = true;
                    OnHostChangesPending?.Invoke();
                    break;
                }
            }
        }

        #endregion
    }
}
