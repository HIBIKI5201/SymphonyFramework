using SymphonyFrameWork.Editor;

using UnityEditor;
using UnityEngine;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     ビルド検証設定の既定値を検証する。
    /// </summary>
    internal sealed class BuildValidationConfigTests
    {
        #region 外部向けAPI

        /// <summary>
        ///     利用中の設定を退避し、保存しない新規設定を作る。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            BuildValidationConfig original = BuildValidationConfig.instance;
            _originalJson = EditorJsonUtility.ToJson(original);
            // ScriptableSingletonを二重生成せず、既存の保存ファイルには触れない。
            Object.DestroyImmediate(original);
            _config = ScriptableObject.CreateInstance<BuildValidationConfig>();
        }

        /// <summary>
        ///     テスト前の設定をメモリ上へ戻す。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (_config != null) { Object.DestroyImmediate(_config); }
            if (_originalJson != null)
            {
                EditorJsonUtility.FromJsonOverwrite(_originalJson, BuildValidationConfig.instance);
            }
        }

        /// <summary>
        ///     既定ではどちらの検証もビルドを止めない。
        /// </summary>
        [Test]
        public void Defaults_NewConfig_DisablesBothBuildValidators()
        {
            Assert.That(_config.IsUxmlValidationOnBuild, Is.False);
            Assert.That(_config.IsFontValidationOnBuild, Is.False);
            Assert.DoesNotThrow(() => new BuildValidationBuildProcessor().OnPreprocessBuild(null));
        }

        /// <summary>
        ///     任意検査は要求せず、手動のフォント検証対象は有効にする。
        /// </summary>
        [Test]
        public void Defaults_NewConfig_UsesOptInOptionalChecks()
        {
            Assert.That(_config.IsForceReimport, Is.False);
            Assert.That(_config.RootCount, Is.Zero);
            Assert.That(_config.IsTextMeshProValidationEnabled, Is.True);
            Assert.That(_config.IsUiToolkitValidationEnabled, Is.True);
            Assert.That(_config.UiToolkitTextSettings, Is.Null);
            Assert.That(_config.RequiredCharacters, Is.Empty);
            Assert.That(_config.IsMultiAtlasRequired, Is.False);
        }

        /// <summary>
        ///     起点と必須名が同じシリアライズ要素に保存される。
        /// </summary>
        [Test]
        public void Roots_SerializedEntry_RetainsRequiredNames()
        {
            using SerializedObject serialized = new(_config);
            SerializedProperty roots = serialized.FindProperty("_roots");
            roots.arraySize = 1;
            SerializedProperty root = roots.GetArrayElementAtIndex(0);
            Assert.That(root.FindPropertyRelative("_asset"), Is.Not.Null);
            SerializedProperty names = root.FindPropertyRelative("_requiredElementNames");
            names.arraySize = 1;
            names.GetArrayElementAtIndex(0).stringValue = "required";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(_config.RootCount, Is.EqualTo(1));
            Assert.That(_config.GetRequiredElementNames(0), Is.EqualTo(new[] { "required" }));
        }

        #endregion

        #region 内部処理

        private BuildValidationConfig _config;
        private string _originalJson;

        #endregion
    }
}
