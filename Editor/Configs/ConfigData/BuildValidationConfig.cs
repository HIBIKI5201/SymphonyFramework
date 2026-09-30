using System;
using System.Collections.Generic;

using SymphonyFrameWork.Core;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     プロジェクト共有のビルド前検証設定を保持する。
    /// </summary>
    [FilePath(
        EditorSymphonyConstant.PROJCET_SETTING_FILE_PATH + nameof(BuildValidationConfig) + ".asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class BuildValidationConfig : ScriptableSingleton<BuildValidationConfig>
    {
        #region 外部向けAPI

        /// <summary> ビルド前にUXMLを検証するか。 </summary>
        internal bool IsUxmlValidationOnBuild => _uxmlValidateOnBuild;

        /// <summary> 依存先から同期再インポートするか。 </summary>
        internal bool IsForceReimport => _forceReimport;

        /// <summary> 登録した起点の数。 </summary>
        internal int RootCount => _roots?.Length ?? 0;

        /// <summary> ビルド前にフォントを検証するか。 </summary>
        internal bool IsFontValidationOnBuild => _fontValidateOnBuild;

        /// <summary> TMP Settingsを検証するか。 </summary>
        internal bool IsTextMeshProValidationEnabled => _textMeshPro;

        /// <summary> UI Toolkitを検証するか。 </summary>
        internal bool IsUiToolkitValidationEnabled => _uiToolkit;

        /// <summary> 全Panel Settingsが参照するText Settings。 </summary>
        internal PanelTextSettings UiToolkitTextSettings => _uiToolkitTextSettings;

        /// <summary> 既定フォントに必要な文字。 </summary>
        internal string RequiredCharacters => _requiredCharacters ?? string.Empty;

        /// <summary> DynamicフォントにMulti Atlasを要求するか。 </summary>
        internal bool IsMultiAtlasRequired => _requireMultiAtlas;

        /// <summary>
        ///     指定位置のUXMLを取得する。
        /// </summary>
        internal VisualTreeAsset GetRoot(int index) => _roots[index]?.Asset;

        /// <summary>
        ///     指定位置の起点で必須とする要素名を取得する。
        /// </summary>
        internal IReadOnlyList<string> GetRequiredElementNames(int index) =>
            _roots[index]?.RequiredElementNames ?? Array.Empty<string>();

        /// <summary>
        ///     Project Settingsで編集した値を保存する。
        /// </summary>
        internal void SaveSettings()
        {
            Save(true);
        }

        #endregion

        #region 内部処理

        [SerializeField, Tooltip("有効な場合だけUXMLの不備でビルドを停止します。")]
        private bool _uxmlValidateOnBuild = false;

        [SerializeField, Tooltip("検証前に依存先からUXML/USSを同期再インポートします。")]
        private bool _forceReimport = false;

        [SerializeField, Tooltip("空の場合はAssets配下の全UXMLを検証します。")]
        private UxmlRoot[] _roots = Array.Empty<UxmlRoot>();

        [SerializeField, Tooltip("有効な場合だけフォント設定の不備でビルドを停止します。")]
        private bool _fontValidateOnBuild = false;

        [SerializeField, Tooltip("Assets内の最初のTMP Settingsを検証します。無ければ検査を省略します。")]
        private bool _textMeshPro = true;

        [SerializeField, Tooltip("UI ToolkitのText SettingsとPanel Settingsを検証します。")]
        private bool _uiToolkit = true;

        [SerializeField, Tooltip("未設定の場合は全Panel Settingsの参照の一致だけを検証します。")]
        private PanelTextSettings _uiToolkitTextSettings = null;

        [SerializeField, TextArea, Tooltip("空の場合は文字の検査を省略します。")]
        private string _requiredCharacters = string.Empty;

        [SerializeField, Tooltip("DynamicフォントにMulti Atlas Texturesを要求します。")]
        private bool _requireMultiAtlas = false;

        /// <summary>
        ///     起点のUXMLと必須要素名を組にして保存する。
        /// </summary>
        [Serializable]
        private sealed class UxmlRoot
        {
            #region 外部向けAPI

            /// <summary> 起点のUXML。 </summary>
            internal VisualTreeAsset Asset => _asset;

            /// <summary> 展開後に必要な要素名。 </summary>
            internal IReadOnlyList<string> RequiredElementNames =>
                Array.AsReadOnly(_requiredElementNames ?? Array.Empty<string>());

            #endregion

            #region 内部処理

            [SerializeField, Tooltip("検証の起点となるAssets内のUXML。")]
            private VisualTreeAsset _asset = null;

            [SerializeField, Tooltip("CloneTree後にも存在する必要がある要素のname。")]
            private string[] _requiredElementNames = Array.Empty<string>();

            #endregion
        }

        #endregion
    }
}
