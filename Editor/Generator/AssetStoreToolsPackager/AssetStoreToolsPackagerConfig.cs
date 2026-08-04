using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     AssetStoreToolsフォルダのパッケージ化設定。
    ///     対象フォルダ直下のPackagerConfig.jsonとして保存される。
    /// </summary>
    internal sealed class AssetStoreToolsPackagerConfig
    {
        /// <summary> パッケージ対象から除外するフォルダ名。 </summary>
        [JsonProperty("ignoredDirectories")]
        public List<string> IgnoredDirectories = new();

        /// <summary> 依存関係に含まれなくても強制的にパッケージへ含める拡張子。 </summary>
        [JsonProperty("forceIncludeExtensions")]
        public List<string> ForceIncludeExtensions = new();

        /// <summary> 既定値の設定を生成する。 </summary>
        /// <returns> 除外フォルダが空で、強制包含拡張子が既定値の設定。 </returns>
        internal static AssetStoreToolsPackagerConfig CreateDefault() => new()
        {
            IgnoredDirectories = new List<string>(),
            ForceIncludeExtensions = new List<string>(DEFAULT_FORCE_INCLUDE_EXTENSIONS),
        };

        /// <summary>
        ///     空白や欠落を取り除いた正規化済みの設定を返す。元のインスタンスは変更しない。
        /// </summary>
        /// <returns> 前後の空白と空要素を除き、拡張子の先頭へドットを補った設定。 </returns>
        internal AssetStoreToolsPackagerConfig Normalize() => new()
        {
            IgnoredDirectories = NormalizeDirectories(IgnoredDirectories),
            ForceIncludeExtensions = NormalizeExtensions(ForceIncludeExtensions),
        };

        /// <summary>
        ///     強制包含拡張子の既定値。
        ///     .csは従来のハードコードの引き継ぎ、.asmdefと.asmrefはUnityの依存関係グラフに載らないため、
        ///     残りはネイティブプラグインのためにここへ含める。
        /// </summary>
        private static readonly string[] DEFAULT_FORCE_INCLUDE_EXTENSIONS =
        {
            ".cs", ".asmdef", ".asmref",
            ".dll", ".so", ".a", ".dylib", ".aar", ".bundle", ".framework", ".jslib",
        };

        /// <summary> 除外フォルダ名から空要素を捨て、前後の空白を落とす。 </summary>
        private static List<string> NormalizeDirectories(List<string> source)
        {
            if (source == null)
            {
                return new List<string>();
            }

            return source
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .ToList();
        }

        /// <summary> 拡張子から空要素を捨て、前後の空白を落として先頭のドットを補う。 </summary>
        private static List<string> NormalizeExtensions(List<string> source)
        {
            if (source == null)
            {
                return new List<string>();
            }

            return source
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Select(extension => extension.Trim())
                .Select(extension => extension.StartsWith(".", StringComparison.Ordinal)
                    ? extension
                    : "." + extension)
                .ToList();
        }
    }
}
