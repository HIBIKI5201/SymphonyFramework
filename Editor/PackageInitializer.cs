using System.IO;

using SymphonyFramework.Config;
using SymphonyFramework.Core;
using SymphonyFramework.Debugger.Logger;
using SymphonyFramework.System.SaveSystem;
using SymphonyFramework.Services;
using SymphonyFramework.Utility;

using UnityEditor;
using UnityEngine.UIElements;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     Config、生成enum、Editor用依存を初期化する。
    /// </summary>
    /// <remarks>
    ///     自動初期化属性を持たず、初期化順を管理するSymphonyEditorOrchestratorから明示的に呼び出す。
    /// </remarks>
    internal static class PackageInitializer
    {
        #region 外部向けAPI

        /// <summary>
        ///     Config、生成enum、Editor用セーブローダーを整備する。
        /// </summary>
        /// <returns> AssetDatabaseの更新が必要な変更を行った場合はtrue。 </returns>
        internal static bool Initialize()
        {
            // 後続処理が参照するConfigを最初に生成し、Asset変更の有無をOrchestratorへ返す。
            bool hasAssetChanges = SymphonyConfigManager.AllConfigCheck();

            // CoreはRuntimeのロガーを参照できない。Edit Modeで動くEditorウィンドウのCoreログも
            // 集約先へ載せるため、Runtimeの初期化を待たずここでも中継口へ注入する。
            CoreLogRelay.ExceptionHandler = exception => SymphonyDebugLogger.LogException(exception);

            // Editor設定とAssetDatabase依存をRuntime側の差し替え口へ明示的に注入する。
            ApplyPartituraLogOptions();
            SymphonyVisualElement.EditorAssetLoader =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>;
            SaveStore.ConfigureLoaderResolver(ResolveSaveDataLoader);

            // Config生成後にenumとAssembly Definitionを整え、必要なRefreshを同じ要求へ集約する。
            hasAssetChanges |= EnumInitialize();

            return hasAssetChanges;
        }

        /// <summary>
        ///     Editor用に注入したアセットローダーを解除する。
        /// </summary>
        internal static void Shutdown()
        {
            // assembly reload後に旧Editorアセンブリのデリゲートを残さない。
            SymphonyVisualElement.EditorAssetLoader = null;
            CoreLogRelay.Reset();
        }

        /// <summary>
        ///     UserSettingsの設定値をRuntimeのPartituraログ設定へ注入する。
        /// </summary>
        internal static void ApplyPartituraLogOptions()
        {
            // 利用者ごとのEditor設定を読み、Runtime側がUnityEditorへ依存しない形で反映する。
            SymphonyUserSettingConfig config =
                SymphonyEditorConfigProvider.GetConfig<SymphonyUserSettingConfig>();

            PartituraLogOption.IsSetInstanceLogEnabled =
                config.IsPartituraRegisterLogEnabled;
            PartituraLogOption.IsGetInstanceLogEnabled =
                config.IsPartituraGetLogEnabled;
            PartituraLogOption.IsDestroyInstanceLogEnabled =
                config.IsPartituraUnregisterLogEnabled;
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     自動生成enumとAssembly Definitionの配置・参照を整備する。
        /// </summary>
        /// <returns> AssetDatabaseの更新が必要な変更を行った場合はtrue。 </returns>
        private static bool EnumInitialize()
        {
            bool hasAssetChanges = false;

            // 利用側プロジェクトにenum出力先が無ければ、初回分をまとめて生成する。
            if (!Directory.Exists(EditorSymphonyConstant.ENUM_PATH))
            {
                AutoEnumGenerator.SceneListEnumGenerate(false);
                AutoEnumGenerator.TagsEnumGenerate(false);
                AutoEnumGenerator.LayersEnumGenerate(false);
                AutoEnumGenerator.AudioEnumGenerate(false);
                hasAssetChanges = true;
            }

            // Package導入時の読み取り専用領域にenumを残さず、Assets側の生成物だけを正本にする。
            string path = $"Packages/{SymphonyConstant.SYMPHONY_PACKAGE}/Enum";
            if (Directory.Exists(path))
            {
                FileUtil.DeleteFileOrDirectory(path);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
                hasAssetChanges = true;
            }

            // 生成enumのasmdefをFramework本体から参照できる状態へ揃える。
            string enumAsmdefPath = EditorSymphonyConstant.ENUM_PATH + "/SymphonyFramework.Enum.asmdef";
            string mainAsmdefPath = EditorSymphonyConstant.FRAMEWORK_PATH + "/SymphonyFramework.asmdef";
            string previousMainAsmdef = File.Exists(mainAsmdefPath)
                ? File.ReadAllText(mainAsmdefPath)
                : null;
            AssemblyGenerator.AddAsssemblyReference(mainAsmdefPath, enumAsmdefPath);
            hasAssetChanges |= File.Exists(mainAsmdefPath) &&
                               previousMainAsmdef != File.ReadAllText(mainAsmdefPath);

            return hasAssetChanges;
        }

        /// <summary>
        ///     Editor上の現在のConfigからセーブデータローダーを解決する。
        /// </summary>
        /// <returns> Configで選択されたローダー。未設定の場合は既定のローダー。 </returns>
        private static SaveDataLoaderStrategy ResolveSaveDataLoader()
        {
            // Configがまだ利用できない場合も、既定のJSONローダーでEditor上の保存処理を成立させる。
            SaveDataConfig config =
                SymphonyConfigProvider.GetConfig<SaveDataConfig>();
            return config?.Loader ?? new JsonUtilitySaveDataLoaderStrategy();
        }

        #endregion
    }
}
