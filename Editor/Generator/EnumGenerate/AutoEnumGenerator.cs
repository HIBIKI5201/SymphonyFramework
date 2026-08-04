using System;
using System.IO;
using System.Linq;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary> プロジェクト設定変更に応じてシーン、タグ、レイヤーのenumを自動生成する。 </summary>
    public static class AutoEnumGenerator
    {
        /// <summary> シーンリスト変更時にenumを生成する。 </summary>
        public static void SceneListEnumGenerate()
        {
            SceneListEnumGenerate(true);
        }

        /// <summary> 登録済みタグからenumを生成する。 </summary>
        public static void TagsEnumGenerate()
        {
            TagsEnumGenerate(true);
        }

        /// <summary> 登録済みレイヤーからフラグenumを生成する。 </summary>
        public static void LayersEnumGenerate()
        {
            LayersEnumGenerate(true);
        }

        /// <summary> AudioConfigのグループ名からenumを生成する。 </summary>
        public static void AudioEnumGenerate()
        {
            AudioEnumGenerate(true);
        }

        private static AutoEnumGeneratorConfig _config;
        private static bool _hasAssetChanges;

        /// <summary> シーンリスト変更時に設定に従ってenumを生成する。 </summary>
        private static void SceneListChangedHandler()
        {
            if (_config == null || !_config.AutoSceneListUpdate)
            {
                return;
            }

            SceneListEnumGenerate(false);
            _hasAssetChanges = true;
        }

        /// <summary> タグ変更時に設定に従ってenumを生成する。 </summary>
        private static void TagsChangedHandler()
        {
            if (_config == null || !_config.AutoTagsUpdate)
            {
                return;
            }

            TagsEnumGenerate(false);
            _hasAssetChanges = true;
        }

        /// <summary> レイヤー変更時に設定に従ってenumを生成する。 </summary>
        private static void LayersChangedHandler()
        {
            if (_config == null || !_config.AutoLayerUpdate)
            {
                return;
            }

            LayersEnumGenerate(false);
            _hasAssetChanges = true;
        }

        /// <summary> 各設定変更イベントを自動生成処理へ接続する。 </summary>
        internal static void Initialize()
        {
            _config = SymphonyEditorConfigLocator.GetConfig<AutoEnumGeneratorConfig>();
            _hasAssetChanges = false;

            TagsAndLayersPostProcessor.SceneList.Dispose();
            TagsAndLayersPostProcessor.SceneList.OnSettingChanged += SceneListChangedHandler;

            TagsAndLayersPostProcessor.Tags.Dispose();
            TagsAndLayersPostProcessor.Tags.OnSettingChanged += TagsChangedHandler;

            TagsAndLayersPostProcessor.Layers.Dispose();
            TagsAndLayersPostProcessor.Layers.OnSettingChanged += LayersChangedHandler;
        }

        /// <summary> 各設定変更イベントとの接続を解除する。 </summary>
        internal static void Shutdown()
        {
            TagsAndLayersPostProcessor.SceneList.Dispose();
            TagsAndLayersPostProcessor.Tags.Dispose();
            TagsAndLayersPostProcessor.Layers.Dispose();
            _config = null;
            _hasAssetChanges = false;
        }

        /// <summary> 自動生成処理によるAsset変更の有無を取得して記録を消費する。 </summary>
        /// <returns> 前回の消費後にenum生成を開始した場合はtrue。 </returns>
        internal static bool ConsumeAssetChanges()
        {
            bool hasAssetChanges = _hasAssetChanges;
            _hasAssetChanges = false;
            return hasAssetChanges;
        }

        /// <summary> シーン一覧からenumを生成する。 </summary>
        /// <param name="refreshAssetDatabase"> 生成後にAssetDatabaseを更新する場合はtrue。 </param>
        internal static void SceneListEnumGenerate(bool refreshAssetDatabase)
        {
            var sceneList = EditorBuildSettings.scenes
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                .ToArray();

            EnumGenerator.EnumGenerate(
                sceneList,
                EditorSymphonyConstant.SceneListEnumFileName,
                false,
                refreshAssetDatabase);
        }

        /// <summary> 登録済みタグからenumを生成する。 </summary>
        /// <param name="refreshAssetDatabase"> 生成後にAssetDatabaseを更新する場合はtrue。 </param>
        internal static void TagsEnumGenerate(bool refreshAssetDatabase)
        {
            EnumGenerator.EnumGenerate(
                InternalEditorUtility.tags,
                EditorSymphonyConstant.TagsEnumFileName,
                true,
                refreshAssetDatabase);
        }

        /// <summary> 登録済みレイヤーからフラグenumを生成する。 </summary>
        /// <param name="refreshAssetDatabase"> 生成後にAssetDatabaseを更新する場合はtrue。 </param>
        internal static void LayersEnumGenerate(bool refreshAssetDatabase)
        {
            EnumGenerator.EnumGenerate(
                InternalEditorUtility.layers,
                EditorSymphonyConstant.LayersEnumFileName,
                true,
                refreshAssetDatabase);
        }

        /// <summary> AudioConfigのグループ名からenumを生成する。 </summary>
        /// <param name="refreshAssetDatabase"> 生成後にAssetDatabaseを更新する場合はtrue。 </param>
        internal static void AudioEnumGenerate(bool refreshAssetDatabase)
        {
            var config = SymphonyConfigLocator.GetConfig<AudioConfig>();
            string[] list;
            if (config)
            {
                list = config.AudioGroupSettingList
                    .Select(setting => setting.AudioGroupName)
                    .ToArray();
            }
            else
            {
                Debug.LogWarning("Audio group settings not found.");
                list = Array.Empty<string>();
            }

            EnumGenerator.EnumGenerate(
                list,
                EditorSymphonyConstant.AudioGroupTypeEnumName,
                false,
                refreshAssetDatabase);
        }
    }
}
