using System.Collections.Generic;

using SymphonyFrameWork.Editor.SettingProvider;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     ビルド前検証のProject Settings画面を提供する。
    /// </summary>
    internal sealed class BuildValidationSettingProvider
    {
        #region 外部向けAPI

        /// <summary> 設定画面の表示名。 </summary>
        internal const string LABEL = "Build Validation";

        /// <summary> Project Settings上の設定パス。 </summary>
        internal const string SELF_PATH = SymphonySettingProvider.PROVIDER_PATH + LABEL;

        /// <summary>
        ///     表示期間に対応した編集状態を持つ画面を作成する。
        /// </summary>
        [SettingsProvider]
        internal static SettingsProvider CreateCustomSettingsProvider()
        {
            BuildValidationSettingProvider view = new();
            return new SettingsProvider(SELF_PATH, SettingsScope.Project)
            {
                label = LABEL,
                guiHandler = view.DrawSettings,
                activateHandler = (_, _) => view.ReloadSettings(),
                deactivateHandler = view.ReleaseSettings,
                keywords = new HashSet<string>(new[] { "build", "uxml", "uss", "font", "TMP", "validation" }),
            };
        }

        #endregion

        #region 内部処理

        private SerializedObject _serializedConfig;

        /// <summary>
        ///     画面を開くたびに設定を読み直す。
        /// </summary>
        private void ReloadSettings()
        {
            ReleaseSettings();
            BuildValidationConfig config = BuildValidationConfig.instance;
            _serializedConfig = new SerializedObject(config);
            // 初めて開いたときに共有設定を保存し、Editor起動時には生成しない。
            config.SaveSettings();
        }

        /// <summary>
        ///     画面を離れるときにシリアライズ編集用の状態を解放する。
        /// </summary>
        private void ReleaseSettings()
        {
            _serializedConfig?.Dispose();
            _serializedConfig = null;
        }

        /// <summary>
        ///     検証の対象とビルド時の有効設定を描画する。
        /// </summary>
        private void DrawSettings(string searchContext)
        {
            if (_serializedConfig == null) { ReloadSettings(); }
            // 別の編集経路で変更された値も、表示を続けたまま取り込む。
            _serializedConfig.Update();
            SymphonyDocumentationGUI.DrawOpenButton(SymphonyDocumentPageEnum.BuildValidation);
            EditorGUILayout.HelpBox(
                "Validate On Buildを有効にした検証だけがビルド前に実行されます。"
                + " 手動実行はTools > SymphonyFrameWork > Build Validationから行えます。", MessageType.Info);

            EditorGUILayout.LabelField("UXML", EditorStyles.boldLabel);
            DrawProperty("_uxmlValidateOnBuild", "Validate On Build");
            DrawProperty("_forceReimport", "Force Reimport");
            DrawProperty("_roots", "Roots");
            EditorGUILayout.HelpBox("Rootsが空の場合はAssets内の全UXMLを検証します。", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Font", EditorStyles.boldLabel);
            DrawProperty("_fontValidateOnBuild", "Validate On Build");
            DrawProperty("_textMeshPro", "TextMesh Pro");
            DrawProperty("_uiToolkit", "UI Toolkit");
            DrawProperty("_uiToolkitTextSettings", "UI Toolkit Text Settings");
            DrawProperty("_requiredCharacters", "Required Characters");
            DrawProperty("_requireMultiAtlas", "Require Multi Atlas");

            if (_serializedConfig.ApplyModifiedProperties()) { BuildValidationConfig.instance.SaveSettings(); }
        }

        /// <summary>
        ///     シリアライズ項目とその子を編集欄へ表示する。
        /// </summary>
        private void DrawProperty(string propertyName, string label)
        {
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty(propertyName), new GUIContent(label), true);
        }

        #endregion
    }
}
