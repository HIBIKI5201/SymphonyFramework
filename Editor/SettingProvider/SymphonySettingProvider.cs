using System.Collections.Generic;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor.SettingProvider
{
    /// <summary>
    ///     Symphony FrameworkのルートProject Settings画面を提供する。
    /// </summary>
    public sealed class SymphonySettingProvider
    {
        #region 外部向けAPI

        /// <summary> Project Settingsに表示するルート設定項目名。 </summary>
        public const string LABEL = SymphonyConstant.SYMPHONY_FRAMEWORK;

        /// <summary> ルートSettingsProviderの完全な設定パス。 </summary>
        public const string SELF_PATH = EditorSymphonyConstant.PROJECT_SETTING_PATH + LABEL;

        /// <summary> 子SettingsProviderが使用する設定パスの接頭辞。 </summary>
        public const string PROVIDER_PATH = EditorSymphonyConstant.PROJECT_SETTING_PATH + LABEL + "/";

        /// <summary>
        ///     Framework設定用のSettingsProviderを生成する。
        /// </summary>
        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
        {
            // Project単位の入口へ、個人設定とRuntime設定を編集するIMGUI画面を登録する。
            SettingsProvider provider = new(SELF_PATH, SettingsScope.Project)
            {
                label = LABEL,
                guiHandler = IMGUI,
                deactivateHandler = ReleaseDebugHUDSerializedObject,
                keywords = new HashSet<string>(new[]
                {
                    "symphony",
                    "framework",
                    "asset",
                    "protection",
                    "service locator",
                    "log",
                    "debug",
                    "hud",
                    "shortcut",
                    "input action",
                }),
            };

            return provider;
        }

        #endregion

        #region 内部処理

        private static DebugHUDConfig _serializedDebugHUDConfig;
        private static SerializedObject _debugHUDSerializedObject;

        /// <summary>
        ///     Frameworkのアセット保護、ログ、Debug HUD Shortcut設定を描画する。
        /// </summary>
        private static void IMGUI(string searchContext)
        {
            SymphonyDocumentationGUI.DrawOpenButton(SymphonyDocumentPageEnum.EditorTools);

            SymphonyUserSettingConfig config =
                SymphonyEditorConfigLocator.GetConfig<SymphonyUserSettingConfig>();

            // 個人ごとの保護設定をUserSettings/SymphonyFrameWork配下のアセットへ即時保存する。
            EditorGUILayout.LabelField("Asset Protection", EditorStyles.boldLabel);
            AssetProtectionModeEnum protectionMode =
                (AssetProtectionModeEnum)EditorGUILayout.EnumPopup(
                    "Protection Mode",
                    config.AssetProtectionMode);
            if (protectionMode != config.AssetProtectionMode) { config.AssetProtectionMode = protectionMode; }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Service Locator Logs", EditorStyles.boldLabel);

            bool isSetInstanceLogEnabled = EditorGUILayout.Toggle(
                "Set Instance",
                config.IsServiceLocatorSetInstanceLogEnabled);
            bool isGetInstanceLogEnabled = EditorGUILayout.Toggle(
                "Get Instance",
                config.IsServiceLocatorGetInstanceLogEnabled);
            bool isDestroyInstanceLogEnabled = EditorGUILayout.Toggle(
                "Destroy Instance",
                config.IsServiceLocatorDestroyInstanceLogEnabled);

            bool hasLogOptionChanged =
                isSetInstanceLogEnabled != config.IsServiceLocatorSetInstanceLogEnabled ||
                isGetInstanceLogEnabled != config.IsServiceLocatorGetInstanceLogEnabled ||
                isDestroyInstanceLogEnabled != config.IsServiceLocatorDestroyInstanceLogEnabled;
            if (hasLogOptionChanged)
            {
                // 個人設定を保存した後、現在のEditorセッションで使うログオプションへ反映する。
                config.IsServiceLocatorSetInstanceLogEnabled = isSetInstanceLogEnabled;
                config.IsServiceLocatorGetInstanceLogEnabled = isGetInstanceLogEnabled;
                config.IsServiceLocatorDestroyInstanceLogEnabled = isDestroyInstanceLogEnabled;
                PackageInitializer.ApplyServiceLocateLogOptions();
            }

            EditorGUILayout.Space();
            DrawDebugHUDShortcut();
        }

        /// <summary> Debug HUDの表示切り替えInput Actionを描画する。 </summary>
        private static void DrawDebugHUDShortcut()
        {
            EditorGUILayout.LabelField("Debug HUD Shortcut", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "EditorとDevelopment BuildでDebug HUDの表示を切り替えるInput Actionです。"
                + " 複数プラットフォーム向けのBindingを追加できます。",
                MessageType.Info);

            // 画面を開いただけでRuntime Configの生成を開始せず、取得できたものだけを描画する。
            DebugHUDConfig config = SymphonyConfigLocator.GetConfig<DebugHUDConfig>();
            if (config == null)
            {
                DrawMissingDebugHUDConfig();
                return;
            }

            SerializedObject serializedObject = GetDebugHUDSerializedObject(config);
            SerializedProperty toggleActionProperty = serializedObject.FindProperty("_toggleAction");

            serializedObject.UpdateIfRequiredOrScript();
            string serializedBeforeEdit = EditorJsonUtility.ToJson(config);
            EditorGUILayout.PropertyField(
                toggleActionProperty,
                new GUIContent("Toggle Action"),
                true);

            // Input SystemのPropertyDrawerは一部の操作を内部でApplyするため、未適用分だけを反映する。
            if (serializedObject.hasModifiedProperties) { serializedObject.ApplyModifiedProperties(); }

            // 展開や選択だけでは保存せず、Actionのシリアライズ内容が変わった場合だけ再構築する。
            if (serializedBeforeEdit == EditorJsonUtility.ToJson(config)) { return; }

            config.RebuildToggleActionSerializationState();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            serializedObject.UpdateIfRequiredOrScript();
        }

        /// <summary> 指定Configを対象とするDebug HUD設定用SerializedObjectを取得する。 </summary>
        /// <param name="config"> 編集対象のDebug HUD設定。 </param>
        /// <returns> 同じConfigの表示中は再利用するSerializedObject。 </returns>
        internal static SerializedObject GetDebugHUDSerializedObject(DebugHUDConfig config)
        {
            if (config == null) { throw new global::System.ArgumentNullException(nameof(config)); }

            if (_debugHUDSerializedObject != null && _serializedDebugHUDConfig == config)
            {
                return _debugHUDSerializedObject;
            }

            ReleaseDebugHUDSerializedObject();
            _serializedDebugHUDConfig = config;
            _debugHUDSerializedObject = new SerializedObject(config);
            return _debugHUDSerializedObject;
        }

        /// <summary> Debug HUD設定用SerializedObjectを解放する。 </summary>
        internal static void ReleaseDebugHUDSerializedObject()
        {
            _debugHUDSerializedObject?.Dispose();
            _debugHUDSerializedObject = null;
            _serializedDebugHUDConfig = null;
        }

        /// <summary> DebugHUDConfigが未生成の場合に、生成を要求する導線を描画する。 </summary>
        private static void DrawMissingDebugHUDConfig()
        {
            EditorGUILayout.HelpBox(
                "DebugHUDConfig がまだ生成されていません。通常はEditor起動時に生成されます。",
                MessageType.Warning);

            if (!GUILayout.Button("設定アセットを生成")) { return; }

            if (!SymphonyEditorOrchestrator.RequestPackageSetup())
            {
                SymphonyDebugLogger.LogDirect(
                    "Symphony Frameworkの初期化中のため、DebugHUDConfigの生成を要求できませんでした。",
                    LogKindEnum.Warning);
            }
        }

        #endregion
    }
}
