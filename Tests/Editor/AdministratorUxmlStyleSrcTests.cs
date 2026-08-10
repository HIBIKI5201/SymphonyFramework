using System.IO;
using System.Text.RegularExpressions;

using NUnit.Framework;

using SymphonyFrameWork.Core;

using UnityEditor;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     Symphony AdministratorのUXMLが参照するUSSについて、
    ///     srcへ書かれたアセットパスとGUIDが同じアセットを指すことを検証する。
    /// </summary>
    public sealed class AdministratorUxmlStyleSrcTests
    {
        /// <summary> Style要素のsrcからアセットパスとGUIDを取り出す。 </summary>
        /// <remarks>
        ///     srcは <c>project://database/&lt;パス&gt;?fileID=...&amp;amp;guid=...&amp;amp;type=3#&lt;名前&gt;</c> の形式で、
        ///     パス部分は最初の <c>?</c> までである。
        /// </remarks>
        private static readonly Regex StyleSourcePattern = new Regex(
            "src=\"project://database/(?<path>[^?\"]+)\\?[^\"]*?guid=(?<guid>[0-9a-fA-F]{32})",
            RegexOptions.Compiled);

        /// <summary> Style要素の開始タグ。srcを取り出せなかったものを検出するために数える。 </summary>
        private static readonly Regex StyleElementPattern = new Regex("<Style\\b", RegexOptions.Compiled);

        /// <summary>
        ///     管理ウィンドウを構成する全UXMLについて、Style要素のsrcのパスがGUIDの解決先と一致することを検証する。
        /// </summary>
        /// <remarks>
        ///     UnityはGUIDでUSSを解決するため、パスだけが古くても表示は壊れない。
        ///     GUIDが失われた場合とパスで解決する経路のために、両者が一致していることを保つ。
        /// </remarks>
        [Test]
        public void StyleSrc_AllAdministratorUxml_PathAgreesWithGuid()
        {
            if (EditorSymphonyConstant.IsPackage())
            {
                Assert.Ignore(
                    "srcのパスはAssets直下配置の形式でしか書けないため、Packages配下では検証できません。");
            }

            string uitkFolder = EditorSymphonyConstant.UITK_PATH.TrimEnd('/');
            string[] uxmlGuids = AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { uitkFolder });

            Assert.That(uxmlGuids, Is.Not.Empty,
                $"{uitkFolder} にUXMLが1つも見つかりません。検証対象のパスが変わった可能性があります。");

            foreach (string uxmlGuid in uxmlGuids)
            {
                string uxmlPath = AssetDatabase.GUIDToAssetPath(uxmlGuid);
                string uxmlText = File.ReadAllText(uxmlPath);

                MatchCollection sources = StyleSourcePattern.Matches(uxmlText);
                Assert.That(sources.Count, Is.EqualTo(StyleElementPattern.Matches(uxmlText).Count),
                    $"{uxmlPath} に、パスとGUIDを取り出せないStyle要素があります。");

                foreach (Match source in sources)
                {
                    string declaredPath = source.Groups["path"].Value;
                    string resolvedPath = AssetDatabase.GUIDToAssetPath(source.Groups["guid"].Value);

                    Assert.That(resolvedPath, Is.Not.Empty,
                        $"{uxmlPath} のStyle要素のGUIDがどのアセットにも解決できません。");
                    Assert.That(declaredPath, Is.EqualTo(resolvedPath),
                        $"{uxmlPath} のStyle要素のsrcが、GUIDの解決先と違うパスを指しています。");
                }
            }
        }
    }
}
