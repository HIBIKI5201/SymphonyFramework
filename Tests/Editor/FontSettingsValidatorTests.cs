using System.IO;

using SymphonyFrameWork.Editor;

using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     実際のUI Toolkitフォントのシリアライズ項目を検証する。
    /// </summary>
    internal sealed class FontSettingsValidatorTests
    {
        #region 外部向けAPI

        /// <summary>
        ///     設定とフォントの実アセットを作る。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Assert.That(Directory.Exists(TEMP_DIRECTORY), Is.False, "既存フォルダを上書きしません。");
            AssetDatabase.CreateFolder("Assets", "SymphonyFontValidationTests_Temp");
            _ownsDirectory = true;
            Font source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _font = FontAsset.CreateFontAsset(source);
            Assert.That(_font, Is.Not.Null);

            // 生成直後のFontAssetと子オブジェクトは保存しない指定のため、アセット化する前に外す。
            _font.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(_font, TEMP_DIRECTORY + "/Font.asset");

            // FontAssetが作成した子アセットも同じファイルへ保存する。
            if (_font.material != null)
            {
                _font.material.hideFlags = HideFlags.None;
                AssetDatabase.AddObjectToAsset(_font.material, _font);
            }

            foreach (Texture2D texture in _font.atlasTextures)
            {
                if (texture == null) { continue; }
                texture.hideFlags = HideFlags.None;
                AssetDatabase.AddObjectToAsset(texture, _font);
            }

            _settings = ScriptableObject.CreateInstance<PanelTextSettings>();
            AssetDatabase.CreateAsset(_settings, TEMP_DIRECTORY + "/TextSettings.asset");
            using SerializedObject serializedSettings = new(_settings);
            serializedSettings.FindProperty("m_DefaultFontAsset").objectReferenceValue = _font;
            serializedSettings.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        ///     このテストが作成した設定とフォントを削除する。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (_ownsDirectory) { AssetDatabase.DeleteAsset(TEMP_DIRECTORY); }
            _ownsDirectory = false;
        }

        /// <summary>
        ///     フォントのモードとフラグをSerializedObject経由で読み取る。
        /// </summary>
        [TestCase(0, false, false)]
        [TestCase(1, true, true)]
        [TestCase(2, false, true)]
        public void ReadSnapshot_RealFontAsset_ReturnsSerializedValues(int mode, bool multiAtlas, bool clearOnBuild)
        {
            using SerializedObject serializedFont = new(_font);
            serializedFont.FindProperty("m_AtlasPopulationMode").intValue = mode;
            serializedFont.FindProperty("m_IsMultiAtlasTexturesEnabled").boolValue = multiAtlas;
            serializedFont.FindProperty("m_ClearDynamicDataOnBuild").boolValue = clearOnBuild;
            serializedFont.ApplyModifiedPropertiesWithoutUndo();
            BuildValidationReport report = new("Test");
            FontSnapshot snapshot = FontSettingsValidator.ReadSnapshot(_settings, "m_DefaultFontAsset", "", report);

            Assert.That(report.Errors, Is.Empty, report.CreateMessage());
            Assert.That(snapshot.HasDefaultFont, Is.True);
            Assert.That(snapshot.PopulationMode, Is.EqualTo(mode));
            Assert.That(snapshot.IsMultiAtlasEnabled, Is.EqualTo(multiAtlas));
            Assert.That(snapshot.IsFontClearOnBuild, Is.EqualTo(clearOnBuild));
            Assert.That(snapshot.IsTextSettingsClearOnBuild, Is.False);
        }

        /// <summary>
        ///     Character TableのUnicodeを読み取る。
        /// </summary>
        [Test]
        public void ReadSnapshot_CharacterTable_ReturnsUnicodeValues()
        {
            using SerializedObject serializedFont = new(_font);
            SerializedProperty table = serializedFont.FindProperty("m_CharacterTable");
            table.arraySize = 1;
            table.GetArrayElementAtIndex(0).FindPropertyRelative("m_Unicode").uintValue = 0x65E5;
            serializedFont.ApplyModifiedPropertiesWithoutUndo();
            BuildValidationReport report = new("Test");
            FontSnapshot snapshot = FontSettingsValidator.ReadSnapshot(_settings, "m_DefaultFontAsset", "", report);

            Assert.That(report.Errors, Is.Empty, report.CreateMessage());
            Assert.That(snapshot.Characters, Does.Contain(0x65E5u));
        }

        /// <summary>
        ///     Text Settings側の消去設定と元フォント参照を読み取る。
        /// </summary>
        [Test]
        public void ReadSnapshot_TextSettingsAndSource_ReturnsValues()
        {
            using SerializedObject serializedSettings = new(_settings);
            serializedSettings.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            using SerializedObject serializedFont = new(_font);
            serializedFont.FindProperty("m_SourceFontFile").objectReferenceValue = null;
            serializedFont.ApplyModifiedPropertiesWithoutUndo();
            BuildValidationReport report = new("Test");
            FontSnapshot snapshot = FontSettingsValidator.ReadSnapshot(_settings, "m_DefaultFontAsset", "", report);

            Assert.That(report.Errors, Is.Empty, report.CreateMessage());
            Assert.That(snapshot.IsTextSettingsClearOnBuild, Is.True);
            Assert.That(snapshot.HasSourceFont, Is.False);
        }

        /// <summary>
        ///     名前が変わったプロパティを未設定扱いで黙って通さない。
        /// </summary>
        [Test]
        public void ReadSnapshot_MissingProperty_ReportsCannotValidate()
        {
            BuildValidationReport report = new("Test");
            FontSettingsValidator.ReadSnapshot(_settings, "m_missingDefaultFont", "", report);
            Assert.That(report.CreateMessage(), Does.Contain("m_missingDefaultFont"));
            Assert.That(report.CreateMessage(), Does.Contain("検証できません"));
        }

        #endregion

        #region 内部処理

        private const string TEMP_DIRECTORY = "Assets/SymphonyFontValidationTests_Temp";
        private FontAsset _font;
        private PanelTextSettings _settings;
        private bool _ownsDirectory;

        #endregion
    }
}
