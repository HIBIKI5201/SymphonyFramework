using System;
using System.IO;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     設定アセットの存在を保証する。
    /// </summary>
    public static class SymphonyConfigManager
    {
        #region 外部向けAPI

        /// <summary>
        ///     Runtime用とEditor用のすべての設定アセットが存在することを保証する。
        /// </summary>
        /// <returns> AssetDatabaseの更新が必要な変更を行った場合はtrue。 </returns>
        internal static bool AllConfigCheck()
        {
            // 戻り値はAssetDatabaseへ追加するRuntime設定だけを表し、ScriptableSingletonの取得は含めない。
            bool hasAssetChanges = false;

            // PackageInitializerの起動時処理に加え、SaveData設定画面で不足を検出した場合もこの生成処理へ到達する。
            // Runtime設定はResources配下、共有Editor設定はProjectSettings/Packages配下へ保存する。
            hasAssetChanges |= FileCheck<SceneLoadConfig>();
            hasAssetChanges |= FileCheck<AudioConfig>();
            hasAssetChanges |= FileCheck<SaveDataConfig>();

            // 個人設定はEditorPrefsではなく、UserSettings/SymphonyFrameWork配下へ保存する。
            // ScriptableSingletonはinstanceの取得時に既存アセットをロードし、無ければ保存可能なインスタンスを用意する。
            CreateUserSettingFolder();
            EditorFileCheck<AutoEnumGeneratorConfig>();
            EditorFileCheck<SymphonyUserSettingConfig>();

            return hasAssetChanges;
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     Resources内のRuntime設定アセットが存在することを保証する。
        /// </summary>
        /// <typeparam name="T"> 存在を保証するRuntime設定アセットの型。 </typeparam>
        /// <returns> 設定アセットまたは配置先フォルダを生成した場合はtrue。 </returns>
        private static bool FileCheck<T>() where T : ScriptableObject
        {
            string path = SymphonyConfigLocator.GetFullPath<T>();
            // Config型に対応する既知の保存先が無い場合は、推測したパスへ生成しない。
            if (path == null)
            {
                SymphonyDebugLogger.LogDirect(typeof(T).Name + " doesn't exist!", LogKindEnum.Warning);
                return false;
            }

            // 既存アセットがある場合はシリアライズ済みの設定値を維持する。
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) { return false; }

            string directory = SymphonyConstant.RESOURCES_RUNTIME_PATH;

            // 初回導入時でもアセットを配置できるよう、Resourcesの保存先を先に用意する。
            CreateResourcesFolder(directory);

            // 新規設定をResourcesへ作成し、AssetDatabaseへ永続化してから完了を通知する。
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            SymphonyDebugLogger.LogDirect($"'{path}' に新しい {typeof(T).Name} を作成しました。");
            return true;
        }

        /// <summary>
        ///     Editor用ScriptableSingletonを取得して必要に応じて生成する。
        /// </summary>
        private static void EditorFileCheck<T>() where T : ScriptableSingleton<T>
        {
            _ = SymphonyEditorConfigLocator.GetConfig<T>();
        }

        /// <summary>
        ///     Resourcesの設定保存先が無ければ生成する。
        /// </summary>
        private static void CreateResourcesFolder(string resourcesPath)
        {
            // 保存先が既にある場合は、ファイルシステムへ不要な変更を加えない。
            if (Directory.Exists(resourcesPath)) { return; }

            Directory.CreateDirectory(resourcesPath);
        }

        /// <summary>
        ///     UserSettings内にFramework設定の保存先フォルダを生成する。
        /// </summary>
        private static void CreateUserSettingFolder()
        {
            // 個人設定はバージョン管理対象外のUserSettingsへ置き、共有設定と混在させない。
            try
            {
                Directory.CreateDirectory(EditorSymphonyConstant.USER_SETTING_FILE_PATH);
            }
            catch (Exception exception)
            {
                SymphonyDebugLogger.LogDirect(
                    $"[{nameof(SymphonyConfigManager)}] UserSettingsの保存先を生成できませんでした。" +
                    $" path: '{EditorSymphonyConstant.USER_SETTING_FILE_PATH}', reason: '{exception.Message}'", LogKindEnum.Warning);
            }
        }

        #endregion
    }
}
