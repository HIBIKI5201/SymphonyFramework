using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     SerializedObjectからフォント設定を読み取り、純粋な規則へ渡す。
    /// </summary>
    internal static class FontSettingsValidator
    {
        #region 外部向けAPI

        /// <summary>
        ///     有効なフォント検証を実行して全エラーを返す。
        /// </summary>
        internal static BuildValidationReport Validate(BuildValidationConfig config)
        {
            BuildValidationReport report = new("Font Settings");
            if (config.IsTextMeshProValidationEnabled)
            {
                // TMPの型を直接参照せず、Assets内の設定だけを探す。
                string[] guids = AssetDatabase.FindAssets("t:TMP_Settings", new[] { "Assets" });
                if (guids.Length == 0)
                {
                    report.AddNote("TMP SettingsがAssets内に無いため、TextMesh Proの検査を省略しました。");
                }
                else
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    ValidateTextSettings(AssetDatabase.LoadMainAssetAtPath(path), path,
                        "m_defaultFontAsset", config, report);
                }
            }

            if (config.IsUiToolkitValidationEnabled)
            {
                Object expectedSettings = config.UiToolkitTextSettings;
                if (expectedSettings != null)
                {
                    string path = AssetDatabase.GetAssetPath(expectedSettings);
                    ValidateTextSettings(expectedSettings, path, "m_DefaultFontAsset", config, report);
                }

                ValidatePanelReferences(expectedSettings, report);
            }

            return report;
        }

        /// <summary>
        ///     Text Settingsと既定フォントをUnityに依存しない値へ変換する。
        /// </summary>
        /// <param name="settings"> 検査対象のText Settings。 </param>
        /// <param name="defaultFontProperty"> TMPまたはUI Toolkitの既定フォントのプロパティ名。 </param>
        /// <param name="requiredCharacters"> 元フォントで収録を確認する文字。 </param>
        /// <param name="report"> プロパティ欠落など、読み取り自体のエラーの記録先。 </param>
        /// <returns> 読み取った値。既定フォントがPackages内の場合は検査を省略してnull。 </returns>
        internal static FontSnapshot ReadSnapshot(
            Object settings, string defaultFontProperty, string requiredCharacters, BuildValidationReport report)
        {
            FontSnapshot snapshot = new();
            string settingsPath = AssetDatabase.GetAssetPath(settings);
            using SerializedObject serializedSettings = new(settings);
            SerializedProperty settingsClear = FindProperty(
                serializedSettings, "m_ClearDynamicDataOnBuild", SerializedPropertyType.Boolean, report);
            snapshot.IsTextSettingsClearOnBuild = settingsClear?.boolValue ?? false;
            SerializedProperty defaultFont = FindProperty(
                serializedSettings, defaultFontProperty, SerializedPropertyType.ObjectReference, report);
            Object font = defaultFont?.objectReferenceValue;
            snapshot.HasDefaultFont = font != null;
            if (font == null) { return snapshot; }

            // Packagesのフォント自体を検証しない。Assets内の設定から参照されていても対象外にする。
            string fontPath = AssetDatabase.GetAssetPath(font);
            if (fontPath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                report.AddNote($"既定フォントがPackages内のため検査を省略しました。Path: {settingsPath}, Font: {fontPath}");
                return null;
            }

            using SerializedObject serializedFont = new(font);
            SerializedProperty populationMode = FindProperty(
                serializedFont, "m_AtlasPopulationMode", SerializedPropertyType.Enum, report);
            SerializedProperty multiAtlas = FindProperty(
                serializedFont, "m_IsMultiAtlasTexturesEnabled", SerializedPropertyType.Boolean, report);
            SerializedProperty fontClear = FindProperty(
                serializedFont, "m_ClearDynamicDataOnBuild", SerializedPropertyType.Boolean, report);
            SerializedProperty sourceProperty = FindProperty(
                serializedFont, "m_SourceFontFile", SerializedPropertyType.ObjectReference, report);

            snapshot.PopulationMode = populationMode?.intValue ?? 0;
            snapshot.IsMultiAtlasEnabled = multiAtlas?.boolValue ?? false;
            snapshot.IsFontClearOnBuild = fontClear?.boolValue ?? false;
            Font sourceFont = sourceProperty?.objectReferenceValue as Font;
            snapshot.HasSourceFont = sourceFont != null;
            snapshot.Characters = ReadCharacterTable(serializedFont, report);

            // OSフォントはビルド依存に含めない。元フォントがある場合だけ収録を調べる。
            List<uint> sourceCharacters = new();
            if (sourceFont != null)
            {
                foreach (uint unicode in FontSnapshot.EnumerateCharacters(requiredCharacters))
                {
                    if (unicode <= char.MaxValue && sourceFont.HasCharacter((char)unicode))
                    {
                        sourceCharacters.Add(unicode);
                    }
                }
            }

            snapshot.SourceCharacters = sourceCharacters.AsReadOnly();
            HashSet<string> dependencies = new(AssetDatabase.GetDependencies(settingsPath, true), StringComparer.Ordinal);
            snapshot.IsDefaultFontDependency = !string.IsNullOrEmpty(fontPath) && dependencies.Contains(fontPath);
            string sourcePath = AssetDatabase.GetAssetPath(sourceFont);
            snapshot.IsSourceFontDependency = !string.IsNullOrEmpty(sourcePath) && dependencies.Contains(sourcePath);
            return snapshot;
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     1つのText Settingsの読み取り失敗も結果に含める。
        /// </summary>
        private static void ValidateTextSettings(
            Object settings, string path, string defaultFontProperty,
            BuildValidationConfig config, BuildValidationReport report)
        {
            if (settings == null)
            {
                report.AddError(path, "Text Settingsを読み込めません。");
                return;
            }

            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                report.AddNote($"Text SettingsがAssets外のため検査を省略しました。Path: {path}");
                return;
            }

            try
            {
                FontSnapshot snapshot = ReadSnapshot(settings, defaultFontProperty, config.RequiredCharacters, report);
                if (snapshot == null) { return; }
                foreach (string error in snapshot.CollectErrors(config.RequiredCharacters, config.IsMultiAtlasRequired))
                {
                    report.AddError(path, error);
                }
            }
            catch (Exception exception)
            {
                report.AddError(path, $"フォント設定を検証できません。{exception.Message}");
            }
        }

        /// <summary>
        ///     全Panel Settingsの参照を指定値または最初の参照と比較する。
        /// </summary>
        private static void ValidatePanelReferences(Object expectedSettings, BuildValidationReport report)
        {
            bool hasBaseline = expectedSettings != null;
            string[] guids = AssetDatabase.FindAssets("t:PanelSettings", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                try
                {
                    Object panel = AssetDatabase.LoadMainAssetAtPath(path);
                    if (panel == null)
                    {
                        report.AddError(path, "Panel Settingsを読み込めません。");
                        continue;
                    }

                    using SerializedObject serializedPanel = new(panel);
                    SerializedProperty property = FindProperty(
                        serializedPanel, "textSettings", SerializedPropertyType.ObjectReference, report);
                    if (property == null) { continue; }
                    Object textSettings = property.objectReferenceValue;
                    if (!hasBaseline)
                    {
                        expectedSettings = textSettings;
                        hasBaseline = true;
                    }

                    // 未設定の起点では、既定フォント検証へ暗黙に進まず参照の一致だけを見る。
                    if (textSettings != expectedSettings)
                    {
                        report.AddError(path, "Panel Settingsに共通のUI Toolkit Text Settingsを設定してください。");
                    }
                }
                catch (Exception exception)
                {
                    report.AddError(path, $"Panel Settingsを検証できません。{exception.Message}");
                }
            }
        }

        /// <summary>
        ///     名前と型が一致するシリアライズ項目だけを読み取る。
        /// </summary>
        private static SerializedProperty FindProperty(
            SerializedObject serialized, string name, SerializedPropertyType type, BuildValidationReport report)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null && property.propertyType == type) { return property; }
            report.AddError(AssetDatabase.GetAssetPath(serialized.targetObject),
                $"プロパティ{name}（{type}）が見つからず検証できません。");
            return null;
        }

        /// <summary>
        ///     Character Tableに保存されたUnicodeを読み取る。
        /// </summary>
        private static IReadOnlyCollection<uint> ReadCharacterTable(
            SerializedObject serializedFont, BuildValidationReport report)
        {
            List<uint> characters = new();
            SerializedProperty table = serializedFont.FindProperty("m_CharacterTable");
            string path = AssetDatabase.GetAssetPath(serializedFont.targetObject);
            if (table == null || !table.isArray)
            {
                report.AddError(path, "m_CharacterTableが見つからず検証できません。");
                return characters.AsReadOnly();
            }

            for (int index = 0; index < table.arraySize; index++)
            {
                SerializedProperty unicode = table.GetArrayElementAtIndex(index).FindPropertyRelative("m_Unicode");
                if (unicode == null || unicode.propertyType != SerializedPropertyType.Integer)
                {
                    report.AddError(path, $"m_CharacterTable[{index}].m_Unicodeが見つからず検証できません。");
                    continue;
                }

                characters.Add(unicode.uintValue);
            }

            return characters.AsReadOnly();
        }

        #endregion
    }
}
