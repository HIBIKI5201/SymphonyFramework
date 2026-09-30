using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Editor;

using UnityEditor;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     実アセットとソースの食い違いをUXML検証が検出することを確かめる。
    /// </summary>
    internal sealed class UxmlDependencyValidatorTests
    {
        #region 外部向けAPI

        /// <summary>
        ///     テスト専用フォルダを確保する。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Assert.That(Directory.Exists(TEMP_DIRECTORY), Is.False, "既存フォルダを上書きしません。");
            AssetDatabase.CreateFolder("Assets", "SymphonyBuildValidationTests_Temp");
            _ownsDirectory = true;
        }

        /// <summary>
        ///     このテストが作ったアセットだけを削除する。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (_ownsDirectory) { AssetDatabase.DeleteAsset(TEMP_DIRECTORY); }
            _ownsDirectory = false;
        }

        /// <summary>
        ///     正常なStyle依存と必須要素は通過する。
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void Validate_ValidDependencies_ReturnsNoErrors(bool forceReimport)
        {
            string stylePath = CreateAsset("Style.uss", ".test { color: red; }");
            string guid = AssetDatabase.AssetPathToGUID(stylePath);
            string rootPath = CreateAsset("Root.uxml", Wrap(
                $"<Style src=\"project://database/{stylePath}?guid={guid}\" />"
                + "<ui:VisualElement name=\"required\" />"));
            BuildValidationReport report = UxmlDependencyValidator.Validate(rootPath, new[] { "required" }, forceReimport);
            Assert.That(report.Errors, Is.Empty, report.CreateMessage());
        }

        /// <summary>
        ///     実GUIDとソースのGUIDの不一致を検出する。
        /// </summary>
        [Test]
        public void Validate_MismatchedGuid_ReturnsGuidError()
        {
            CreateAsset("Style.uss", ".test { color: red; }");
            string rootPath = CreateAsset("Root.uxml", Wrap("<Style src=\"Style.uss\" />"));
            BuildValidationReport report = ValidateRewrittenSources(rootPath, new Dictionary<string, string>
            {
                [rootPath] = Wrap("<Style src=\"Style.uss?guid=00000000000000000000000000000000\" />"),
            });
            Assert.That(report.CreateMessage(), Does.Contain("GUIDが一致しません"));
        }

        /// <summary>
        ///     欠落した複数の依存ファイルをまとめて報告する。
        /// </summary>
        [Test]
        public void Validate_MissingDependencies_ReportsEveryMissingFile()
        {
            string rootPath = CreateAsset("Root.uxml", Wrap(""));
            BuildValidationReport report = ValidateRewrittenSources(rootPath, new Dictionary<string, string>
            {
                [rootPath] = Wrap("<Style src=\"MissingA.uss\" /><Style src=\"MissingB.uss\" />"),
            });
            Assert.That(report.CreateMessage(), Does.Contain("MissingA.uss"));
            Assert.That(report.CreateMessage(), Does.Contain("MissingB.uss"));
            Assert.That(report.Errors, Has.Count.EqualTo(4));
        }

        /// <summary>
        ///     再インポートしなくてもソースの循環を検出する。
        /// </summary>
        [Test]
        public void Validate_CyclicDependencies_ReturnsCycleError()
        {
            string firstPath = CreateAsset("First.uxml", Wrap(""));
            string secondPath = CreateAsset("Second.uxml", Wrap(""));
            BuildValidationReport report = ValidateRewrittenSources(firstPath, new Dictionary<string, string>
            {
                [firstPath] = Wrap("<ui:Template name=\"second\" src=\"Second.uxml\" />"),
                [secondPath] = Wrap("<ui:Template name=\"first\" src=\"First.uxml\" />"),
            });
            Assert.That(report.CreateMessage(), Does.Contain("UI依存が循環"));
        }

        /// <summary>
        ///     インポート結果に無い必須要素を検出する。
        /// </summary>
        [Test]
        public void Validate_MissingRequiredElement_ReturnsElementError()
        {
            string rootPath = CreateAsset("Root.uxml", Wrap("<ui:VisualElement name=\"present\" />"));
            BuildValidationReport report = UxmlDependencyValidator.Validate(rootPath, new[] { "absent" });
            Assert.That(report.Errors, Has.Count.EqualTo(1));
            Assert.That(report.CreateMessage(), Does.Contain("Name: absent"));
        }

        /// <summary>
        ///     ソースにだけ追加された名前付き要素を検出する。
        /// </summary>
        [Test]
        public void Validate_SourceElementMissingFromImport_ReturnsElementError()
        {
            string rootPath = CreateAsset("Root.uxml", Wrap(""));
            BuildValidationReport report = ValidateRewrittenSources(rootPath, new Dictionary<string, string>
            {
                [rootPath] = Wrap("<ui:VisualElement name=\"not-imported\" />"),
            });
            Assert.That(report.CreateMessage(), Does.Contain("Name: not-imported"));
        }

        /// <summary>
        ///     XML構文エラーも例外で中断せず結果へ含める。
        /// </summary>
        [Test]
        public void Validate_InvalidXml_ReturnsError()
        {
            string rootPath = CreateAsset("Root.uxml", Wrap(""));
            BuildValidationReport report = ValidateRewrittenSources(rootPath, new Dictionary<string, string>
            {
                [rootPath] = "<broken",
            });
            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.CreateMessage(), Does.Contain("Root.uxml"));
        }

        /// <summary>
        ///     パッケージ自身のSymphony AdministratorのUXMLが、実データとして検証を通過する。
        /// </summary>
        /// <remarks> 合成したアセットだけでは、実際のsrcの書式（project URI、GUID）を網羅できないため。 </remarks>
        [Test]
        public void Validate_AdministratorUxml_ReturnsNoErrors()
        {
            if (EditorSymphonyConstant.IsPackage())
            {
                Assert.Ignore("検証対象はAssets配下に限るため、Packages配下では検証できません。");
            }

            string uitkFolder = EditorSymphonyConstant.UITK_PATH.TrimEnd('/');
            string[] uxmlGuids = AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { uitkFolder });
            Assert.That(uxmlGuids, Is.Not.Empty, $"{uitkFolder} にUXMLが見つかりません。");

            foreach (string guid in uxmlGuids)
            {
                BuildValidationReport report = UxmlDependencyValidator.Validate(
                    AssetDatabase.GUIDToAssetPath(guid), Array.Empty<string>());
                Assert.That(report.Errors, Is.Empty, report.CreateMessage());
            }
        }

        #endregion

        #region 内部処理

        private const string TEMP_DIRECTORY = "Assets/SymphonyBuildValidationTests_Temp";
        private bool _ownsDirectory;

        /// <summary>
        ///     正常なアセットを同期インポートして作成する。
        /// </summary>
        private static string CreateAsset(string fileName, string source)
        {
            string path = TEMP_DIRECTORY + "/" + fileName;
            File.WriteAllText(path, source, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return path;
        }

        /// <summary>
        ///     UI要素を最小のUXMLへ包む。
        /// </summary>
        private static string Wrap(string elements) =>
            "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\">" + elements + "</ui:UXML>";

        /// <summary>
        ///     正常なインポート結果を保ったまま不正なソースを検証する。
        /// </summary>
        /// <remarks> Unityの不正アセットインポートログを抑止せず、検査後にソースを必ず戻す。 </remarks>
        private static BuildValidationReport ValidateRewrittenSources(
            string rootPath, IReadOnlyDictionary<string, string> sources)
        {
            Dictionary<string, string> originals = new(StringComparer.Ordinal);
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (KeyValuePair<string, string> source in sources)
                {
                    originals.Add(source.Key, File.ReadAllText(source.Key));
                    File.WriteAllText(source.Key, source.Value, new UTF8Encoding(false));
                }

                return UxmlDependencyValidator.Validate(rootPath, Array.Empty<string>());
            }
            finally
            {
                try
                {
                    foreach (KeyValuePair<string, string> source in originals)
                    {
                        File.WriteAllText(source.Key, source.Value, new UTF8Encoding(false));
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }

        #endregion
    }
}
