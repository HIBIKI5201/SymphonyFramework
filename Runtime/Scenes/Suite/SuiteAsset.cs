using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Scenes
{
    /// <summary>
    ///     まとめてロードするシーンと、その依存関係を定義するアセット。
    /// </summary>
    /// <remarks>
    ///     利用側が <c>Assets &gt; Create &gt; Symphony Framework &gt; Suite</c> から作成する。
    ///     値の変更はInspectorからだけ行い、コードからは読み取りだけを行う。
    /// </remarks>
    [CreateAssetMenu(
        menuName = "Symphony Framework/Suite",
        fileName = "Suite")]
    // 6.x の名前（SymphonyFrameWork.System.SceneBlock.SceneBlockAsset）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.System.SceneBlock",
        sourceAssembly: "SymphonyFrameWork",
        sourceClassName: "SceneBlockAsset")]
    public sealed class SuiteAsset : ScriptableObject
    {
        #region 外部向けAPI

        /// <summary> ブロックの識別名。未設定の場合はアセット名。 </summary>
        public string SuiteName =>
            string.IsNullOrWhiteSpace(_suiteName) ? name : _suiteName;

        /// <summary> このブロックがロードするシーンの一覧。 </summary>
        public IReadOnlyList<SuiteEntry> Entries =>
            _entries ?? (IReadOnlyList<SuiteEntry>)Array.Empty<SuiteEntry>();

        #endregion

        #region 内部処理

        // 6.x の SceneBlockAsset で保存されたアセットの値を読むため、旧シリアライズ名を残す。
        [SerializeField, FormerlySerializedAs("_blockName"), Tooltip("Suiteの識別名。空の場合はアセット名を使用する。")]
        private string _suiteName;

        [SerializeField, Tooltip("このブロックがロードするシーンと、その先行依存。")]
        private List<SuiteEntry> _entries = new();

        /// <summary>
        ///     依存先として選べるシーン名かを判定する。
        /// </summary>
        /// <param name="sceneName"> 判定するシーン名。 </param>
        /// <returns> 依存先の候補に残す場合はtrue。 </returns>
        /// <remarks>
        ///     <see cref="SuiteEntry" /> の依存欄が <c>SceneNameSelector</c> のフィルターとして呼ぶ。
        ///     **ブロックの外にあるシーンは依存先にできないため、そもそも選べないようにする。**
        ///     Build Settings の全シーンを並べると、選んだ後に
        ///     <c>MissingReference</c> で弾かれることに気づく形になる。
        ///     既に依存として保存済みのシーン名も候補に残す。**候補から外すと、
        ///     Drawerが保存済みの値を先頭の候補へ書き換えてしまい、記述が黙って変わる。**
        ///     外れた依存であること自体は <see cref="SuiteEntryReader" /> の検証が知らせる。
        /// </remarks>
        internal bool CanDependOnScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) { return false; }

            foreach (SuiteEntry entry in Entries)
            {
                if (entry == null) { continue; }
                if (string.Equals(entry.SceneName, sceneName, StringComparison.Ordinal)) { return true; }

                foreach (string dependency in entry.DependsOn)
                {
                    if (string.Equals(dependency, sceneName, StringComparison.Ordinal)) { return true; }
                }
            }

            return false;
        }

        #endregion
    }
}
