using System;
using System.IO;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     タグやレイヤーの変更を監視する
    /// </summary>
    public sealed class TagsAndLayersPostProcessor : AssetPostprocessor
    {
        /// <summary> host callbackによる変更が処理待ちになったときに通知する。 </summary>
        internal static event Action OnHostChangesPending;

        /// <summary> Build Settingsのシーン一覧に対する変更通知。 </summary>
        public static TagsAndLayersSettingData SceneList = new();

        /// <summary> TagManagerのタグ設定に対する変更通知。 </summary>
        public static TagsAndLayersSettingData Tags = new("ProjectSettings/TagManager.asset");

        /// <summary> TagManagerのレイヤー設定に対する変更通知。 </summary>
        public static TagsAndLayersSettingData Layers = new("ProjectSettings/TagManager.asset");

        private static bool _hasPendingSceneListChange;
        private static bool _hasPendingTagManagerChange;

        /// <summary> 監視対象設定ファイルの内容と変更通知を保持する。 </summary>
        public sealed class TagsAndLayersSettingData : IDisposable
        {
            private string _managerPath = string.Empty;
            /// <summary> 監視対象のProjectSettingsファイルパス。 </summary>
            public string TagManagerPath { get => _managerPath; }

            private string _lastManagerContent = string.Empty;
            /// <summary> 前回確認した設定ファイルの内容。 </summary>
            public string LastManagerContent { get => _lastManagerContent; }

            /// <summary> 監視対象の設定が変更されたときに発生する。 </summary>
            public event Action OnSettingChanged;

            /// <summary> ファイルを持たない手動通知用の監視データを生成する。 </summary>
            public TagsAndLayersSettingData() => _managerPath = string.Empty;

            /// <summary> 指定した設定ファイルの初期内容を読み込んで監視データを生成する。 </summary>
            public TagsAndLayersSettingData(string tagManagerPath)
            {
                if (File.Exists(tagManagerPath))
                {
                    _managerPath = tagManagerPath;
                    _lastManagerContent = File.ReadAllText(tagManagerPath);
                }
                else
                {
                    Debug.LogWarning($"{tagManagerPath}にアセットがありません。");
                }
            }

            /// <summary> 比較基準となる直近の設定ファイル内容を更新する。 </summary>
            public void SetLastManagerContent(string content) => _lastManagerContent = content;

            /// <summary> 設定変更イベントを通知する。 </summary>
            public void EventInvoke() => OnSettingChanged?.Invoke();

            /// <summary> 登録済みの変更通知をすべて解除する。 </summary>
            public void Dispose() => OnSettingChanged = null;
        }

        /// <summary> Build Settingsのシーン一覧変更を処理待ちとして記録する。 </summary>
        private static void SceneListChangedHandler()
        {
            _hasPendingSceneListChange = true;
            OnHostChangesPending?.Invoke();
        }

        /// <summary> TagManagerアセットの変更を処理待ちとして記録する。 </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
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

        /// <summary> Build Settingsのシーン一覧変更イベントを購読する。 </summary>
        internal static void Initialize()
        {
            EditorBuildSettings.sceneListChanged -= SceneListChangedHandler;
            EditorBuildSettings.sceneListChanged += SceneListChangedHandler;
        }

        /// <summary> Build Settingsのシーン一覧変更イベントを解除する。 </summary>
        internal static void Shutdown()
        {
            EditorBuildSettings.sceneListChanged -= SceneListChangedHandler;
            _hasPendingSceneListChange = false;
            _hasPendingTagManagerChange = false;
        }

        /// <summary> coalesceした設定変更を各変更通知へ1回ずつ中継する。 </summary>
        internal static void ProcessPendingChanges()
        {
            bool hasSceneListChange = _hasPendingSceneListChange;
            bool hasTagManagerChange = _hasPendingTagManagerChange;
            _hasPendingSceneListChange = false;
            _hasPendingTagManagerChange = false;

            if (hasSceneListChange)
            {
                SceneList.EventInvoke();
            }

            if (!hasTagManagerChange)
            {
                return;
            }

            foreach (var setting in new[] { Tags, Layers })
            {
                string newContent = File.ReadAllText(setting.TagManagerPath);
                if (newContent == setting.LastManagerContent)
                {
                    continue;
                }

                setting.SetLastManagerContent(newContent);
                setting.EventInvoke();
            }
        }
    }
}
