using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

using UnityEditor;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     UXMLソースの依存とインポート結果を検証する。
    /// </summary>
    internal static class UxmlDependencyValidator
    {
        #region 外部向けAPI

        /// <summary>
        ///     設定した起点、またはAssets内の全UXMLを検証する。
        /// </summary>
        internal static BuildValidationReport Validate(BuildValidationConfig config)
        {
            BuildValidationReport report = new("UXML Dependencies");
            HashSet<string> completed = new(StringComparer.Ordinal);
            HashSet<string> visiting = new(StringComparer.Ordinal);
            if (config.RootCount == 0)
            {
                // PackagesやUnityのインポート対象外フォルダを検索しない。
                foreach (string guid in AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    ValidateRoot(path, Array.Empty<string>(), config.IsForceReimport,
                        completed, visiting, report);
                }
            }
            else
            {
                for (int index = 0; index < config.RootCount; index++)
                {
                    VisualTreeAsset root = config.GetRoot(index);
                    if (root == null)
                    {
                        report.AddError($"Roots[{index}]", "起点のUXMLが未設定です。");
                        continue;
                    }

                    ValidateRoot(AssetDatabase.GetAssetPath(root), config.GetRequiredElementNames(index),
                        config.IsForceReimport, completed, visiting, report);
                }
            }

            return report;
        }

        /// <summary>
        ///     指定した起点を設定アセットの変更なしで検証する。
        /// </summary>
        internal static BuildValidationReport Validate(
            string rootPath, IReadOnlyList<string> requiredElementNames, bool forceReimport = false)
        {
            BuildValidationReport report = new("UXML Dependencies");
            ValidateRoot(rootPath, requiredElementNames ?? Array.Empty<string>(), forceReimport,
                new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal), report);
            return report;
        }

        #endregion

        #region 内部処理

        private const ImportAssetOptions IMPORT_OPTIONS = ImportAssetOptions.ForceUpdate
            | ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.DontDownloadFromCacheServer;

        /// <summary>
        ///     起点の例外を記録し、他の起点の検証を継続できるようにする。
        /// </summary>
        private static void ValidateRoot(
            string rootPath, IReadOnlyList<string> requiredElementNames, bool forceReimport,
            HashSet<string> completed, HashSet<string> visiting, BuildValidationReport report)
        {
            try
            {
                if (!UxmlSourceReference.Resolve(rootPath, rootPath, out string path, out string error))
                {
                    report.AddError(rootPath, error);
                    return;
                }

                if (!path.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase))
                {
                    report.AddError(path, "起点にはUXMLを指定してください。");
                    return;
                }

                ValidateDependency(path, forceReimport, completed, visiting, report);
                if (requiredElementNames.Count == 0) { return; }
                VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                if (asset == null || asset.importedWithErrors) { return; }
                ValidateNames(path, asset.CloneTree(), requiredElementNames, "必須のUI要素がありません。", report);
            }
            catch (Exception exception)
            {
                report.AddError(rootPath, $"起点のUXMLを検証できません。{exception.Message}");
            }
        }

        /// <summary>
        ///     ソースを再帰的にたどり、依存先から検証する。
        /// </summary>
        private static void ValidateDependency(
            string path, bool forceReimport, HashSet<string> completed,
            HashSet<string> visiting, BuildValidationReport report)
        {
            if (completed.Contains(path)) { return; }
            if (!visiting.Add(path))
            {
                report.AddError(path, "UI依存が循環しています。");
                return;
            }

            try
            {
                int errorCountBefore = report.Errors.Count;
                bool hasSource = File.Exists(path);
                if (!hasSource) { report.AddError(path, "UIソースが存在しません。"); }
                if (!File.Exists(path + ".meta")) { report.AddError(path, ".metaが存在しません。"); }
                if (!hasSource) { return; }

                XDocument document = null;
                if (path.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase))
                {
                    document = XDocument.Load(path);
                    foreach (XElement element in document.Descendants())
                    {
                        if (element.Name.LocalName != "Template" && element.Name.LocalName != "Style") { continue; }
                        string source = (string)element.Attribute("src");
                        if (!UxmlSourceReference.Resolve(path, source, out string dependencyPath, out string error))
                        {
                            report.AddError(path, error);
                            continue;
                        }

                        ValidateDependency(dependencyPath, forceReimport, completed, visiting, report);
                        if (UxmlSourceReference.TryGetGuid(source, out string expectedGuid))
                        {
                            string actualGuid = AssetDatabase.AssetPathToGUID(dependencyPath);
                            if (!string.Equals(expectedGuid, actualGuid, StringComparison.OrdinalIgnoreCase)
                                || string.IsNullOrEmpty(expectedGuid))
                            {
                                report.AddError(path, $"UI依存のGUIDが一致しません。Dependency: {dependencyPath}, "
                                    + $"Expected: {expectedGuid}, Actual: {actualGuid}");
                            }
                        }
                    }
                }

                // 欠落や循環があっても他の枝を検査し、再インポートは明示指定時だけ行う。
                if (forceReimport && report.Errors.Count == errorCountBefore)
                {
                    AssetDatabase.ImportAsset(path, IMPORT_OPTIONS);
                }
                ValidateImportedAsset(path, document, report);
            }
            catch (Exception exception)
            {
                report.AddError(path, $"UI依存を検証できません。{exception.Message}");
            }
            finally
            {
                visiting.Remove(path);
                completed.Add(path);
            }
        }

        /// <summary>
        ///     インポートエラーとソースにある名前付き要素の欠落を検査する。
        /// </summary>
        private static void ValidateImportedAsset(string path, XDocument document, BuildValidationReport report)
        {
            if (document == null)
            {
                StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (styleSheet == null || styleSheet.importedWithErrors)
                {
                    report.AddError(path, "USSのインポートに失敗しました。");
                }

                return;
            }

            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            if (asset == null || asset.importedWithErrors)
            {
                report.AddError(path, "UXMLのインポートに失敗しました。");
                return;
            }

            List<string> sourceNames = new();
            foreach (XElement element in document.Descendants())
            {
                string name = (string)element.Attribute("name");
                if (string.IsNullOrEmpty(name) || element.Name.LocalName == "Template") { continue; }
                sourceNames.Add(name);
            }

            ValidateNames(path, asset.CloneTree(), sourceNames, "ソースにあるUI要素が欠落しています。", report);
        }

        /// <summary>
        ///     名前付き要素が展開結果に存在するか調べる。
        /// </summary>
        private static void ValidateNames(
            string path, VisualElement root, IReadOnlyList<string> names, string message, BuildValidationReport report)
        {
            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(name) || root.Q<VisualElement>(name) == null)
                {
                    report.AddError(path, $"{message} Name: {name}");
                }
            }
        }

        #endregion
    }
}
