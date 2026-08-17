using System;
using System.IO;
using System.Linq;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     プロジェクト設定変更に応じてシーン、タグ、レイヤーのenumを自動生成する。
    /// </summary>
    public static class AutoEnumGenerator
    {
        #region 外部向けAPI

        /// <summary>
        ///     シーンリスト変更時にenumを生成する。
        /// </summary>
        public static void SceneListEnumGenerate()
        {
            SceneListEnumGenerate(true);
        }

        /// <summary>
        ///     登録済みタグからenumを生成する。
        /// </summary>
        public static void TagsEnumGenerate()
        {
            TagsEnumGenerate(true);
        }

        /// <summary>
        ///     登録済みレイヤーからフラグenumを生成する。
        /// </summary>
        public static void LayersEnumGenerate()
        {
            LayersEnumGenerate(true);
        }

        /// <summary>
        ///     AudioConfigのグループ名からenumを生成する。
        /// </summary>
        public static void AudioEnumGenerate()
        {
            AudioEnumGenerate(true);
        }

        #endregion

        #region 内部処理

        private static AutoEnumGeneratorConfig _config;
        private static bool _hasAssetChanges;

        /// <summary>
        ///     シーンリスト変更時に設定に従ってenumを生成する。
        /// </summary>
        private static void SceneListChangedHandler()
        {
            // 設定を読み込めない場合と自動更新が無効な場合は、生成物を変更しない。
            if (_config == null || !_config.AutoSceneListUpdate) { return; }

            // 一連の設定変更後に呼び出し側がまとめてRefreshできるよう、ここでは更新を保留する。
            SceneListEnumGenerate(false);
            _hasAssetChanges = true;
        }

        /// <summary>
        ///     タグ変更時に設定に従ってenumを生成する。
        /// </summary>
        private static void TagsChangedHandler()
        {
            // 設定を読み込めない場合と自動更新が無効な場合は、生成物を変更しない。
            if (_config == null || !_config.AutoTagsUpdate) { return; }

            // 一連の設定変更後に呼び出し側がまとめてRefreshできるよう、ここでは更新を保留する。
            TagsEnumGenerate(false);
            _hasAssetChanges = true;
        }

        /// <summary>
        ///     レイヤー変更時に設定に従ってenumを生成する。
        /// </summary>
        private static void LayersChangedHandler()
        {
            // 設定を読み込めない場合と自動更新が無効な場合は、生成物を変更しない。
            if (_config == null || !_config.AutoLayerUpdate) { return; }

            // 一連の設定変更後に呼び出し側がまとめてRefreshできるよう、ここでは更新を保留する。
            LayersEnumGenerate(false);
            _hasAssetChanges = true;
        }

        /// <summary>
        ///     各設定変更イベントを自動生成処理へ接続する。
        /// </summary>
        internal static void Initialize()
        {
            _config = SymphonyEditorConfigLocator.GetConfig<AutoEnumGeneratorConfig>();
            _hasAssetChanges = false;

            // Domain Reloadなしで再初期化されても購読が重複しないよう、既存接続を破棄してから登録する。
            TagsAndLayersPostProcessor.SceneList.Dispose();
            TagsAndLayersPostProcessor.SceneList.OnSettingChanged += SceneListChangedHandler;

            TagsAndLayersPostProcessor.Tags.Dispose();
            TagsAndLayersPostProcessor.Tags.OnSettingChanged += TagsChangedHandler;

            TagsAndLayersPostProcessor.Layers.Dispose();
            TagsAndLayersPostProcessor.Layers.OnSettingChanged += LayersChangedHandler;
        }

        /// <summary>
        ///     各設定変更イベントとの接続を解除する。
        /// </summary>
        internal static void Shutdown()
        {
            // Editor終了後に設定変更通知が残らないよう、すべての購読と保持状態を破棄する。
            TagsAndLayersPostProcessor.SceneList.Dispose();
            TagsAndLayersPostProcessor.Tags.Dispose();
            TagsAndLayersPostProcessor.Layers.Dispose();
            _config = null;
            _hasAssetChanges = false;
        }

        /// <summary>
        ///     自動生成処理によるAsset変更の有無を取得して記録を消費する。
        /// </summary>
        /// <returns> 前回の消費後にenum生成を開始した場合はtrue。 </returns>
        internal static bool ConsumeAssetChanges()
        {
            // 同じ変更を複数回Refreshしないよう、読み出しと同時に記録を消費する。
            bool hasAssetChanges = _hasAssetChanges;
            _hasAssetChanges = false;
            return hasAssetChanges;
        }

        /// <summary>
        ///     シーン一覧からenumを生成する。
        /// </summary>
        /// <param name="refreshAssetDatabase"> 生成後にAssetDatabaseを更新する場合はtrue。 </param>
        internal static void SceneListEnumGenerate(bool refreshAssetDatabase)
        {
            // SceneListEnumはBuild Settingsの登録順をそのまま列挙子の順序へ反映する。
            string[] sceneList = EditorBuildSettings.scenes
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                .ToArray();

            // 生成先は利用側のAssets配下であり、生成物は再生成されるため手で編集しない。
            EnumGenerator.EnumGenerate(
                sceneList,
                EditorSymphonyConstant.SceneListEnumFileName,
                false,
                refreshAssetDatabase);
        }

        /// <summary>
        ///     登録済みタグからenumを生成する。
        /// </summary>
        /// <param name="refreshAssetDatabase"> 生成後にAssetDatabaseを更新する場合はtrue。 </param>
        internal static void TagsEnumGenerate(bool refreshAssetDatabase)
        {
            // Project Settingsの登録済みタグを入力とし、利用側の自動生成物へ書き出す。
            EnumGenerator.EnumGenerate(
                InternalEditorUtility.tags,
                EditorSymphonyConstant.TagsEnumFileName,
                true,
                refreshAssetDatabase);
        }

        /// <summary>
        ///     登録済みレイヤーからフラグenumを生成する。
        /// </summary>
        /// <param name="refreshAssetDatabase"> 生成後にAssetDatabaseを更新する場合はtrue。 </param>
        internal static void LayersEnumGenerate(bool refreshAssetDatabase)
        {
            // Project Settingsの登録済みレイヤーを入力とし、利用側の自動生成物へ書き出す。
            EnumGenerator.EnumGenerate(
                InternalEditorUtility.layers,
                EditorSymphonyConstant.LayersEnumFileName,
                true,
                refreshAssetDatabase);
        }

        /// <summary>
        ///     AudioConfigのグループ名からenumを生成する。
        /// </summary>
        /// <param name="refreshAssetDatabase"> 生成後にAssetDatabaseを更新する場合はtrue。 </param>
        internal static void AudioEnumGenerate(bool refreshAssetDatabase)
        {
            AudioConfig config = SymphonyConfigLocator.GetConfig<AudioConfig>();
            string[] list;

            // AudioConfigがあれば設定順のグループ名を使い、無ければ空のenumとして安全に再生成する。
            if (config)
            {
                list = config.AudioGroupSettingList
                    .Select(setting => setting.AudioGroupName)
                    .ToArray();
            }
            else
            {
                SymphonyDebugLogger.LogDirect("Audio group settings not found.", LogKindEnum.Warning);
                list = Array.Empty<string>();
            }

            // 生成先は利用側のAssets配下であり、設定変更時に上書きされるため手で編集しない。
            EnumGenerator.EnumGenerate(
                list,
                EditorSymphonyConstant.AudioGroupTypeEnumName,
                false,
                refreshAssetDatabase);
        }

        #endregion
    }
}
