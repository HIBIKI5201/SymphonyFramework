using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     フォント設定から取得した値だけでビルドの不備を判定する。
    /// </summary>
    internal sealed class FontSnapshot
    {
        #region 外部向けAPI

        /// <summary> Text SettingsがDynamicデータをビルド時に消去するか。 </summary>
        internal bool IsTextSettingsClearOnBuild { get; set; }

        /// <summary> 既定フォントが設定されているか。 </summary>
        internal bool HasDefaultFont { get; set; }

        /// <summary> AtlasPopulationModeのシリアライズ値。 </summary>
        internal int PopulationMode { get; set; }

        /// <summary> フォントがDynamicデータをビルド時に消去するか。 </summary>
        internal bool IsFontClearOnBuild { get; set; }

        /// <summary> Multi Atlasが有効か。 </summary>
        internal bool IsMultiAtlasEnabled { get; set; }

        /// <summary> 元フォントが設定されているか。 </summary>
        internal bool HasSourceFont { get; set; }

        /// <summary> 既定フォントがText Settingsの依存に含まれるか。 </summary>
        internal bool IsDefaultFontDependency { get; set; }

        /// <summary> 元フォントがText Settingsの依存に含まれるか。 </summary>
        internal bool IsSourceFontDependency { get; set; }

        /// <summary> フォントアセットのCharacter TableにあるUnicode。 </summary>
        internal IReadOnlyCollection<uint> Characters { get; set; } = Array.Empty<uint>();

        /// <summary> 元フォントに収録されている必須文字のUnicode。 </summary>
        internal IReadOnlyCollection<uint> SourceCharacters { get; set; } = Array.Empty<uint>();

        /// <summary>
        ///     Unity APIを呼ばず、設定値の不備をすべて返す。
        /// </summary>
        /// <param name="requiredCharacters"> 必須文字。空なら文字の検査をしない。 </param>
        /// <param name="requireMultiAtlas"> DynamicフォントにMulti Atlasを要求するか。 </param>
        internal IReadOnlyList<string> CollectErrors(string requiredCharacters, bool requireMultiAtlas)
        {
            List<string> errors = new();

            // 既定フォントが無ければ、それ以降のフォント検査は成立しない。
            if (!HasDefaultFont)
            {
                errors.Add("Default Font Assetを設定してください。");
                return errors.AsReadOnly();
            }

            // Text Settingsの消去はDynamicフォントのデータだけに作用する。Staticの既定フォントでは問題にならない。
            bool isDynamic = PopulationMode == DYNAMIC_MODE || PopulationMode == DYNAMIC_OS_MODE;
            if (isDynamic && IsTextSettingsClearOnBuild)
            {
                errors.Add("Text SettingsのClear Dynamic Data On Buildを無効にしてください。");
            }

            if (isDynamic && IsFontClearOnBuild)
            {
                errors.Add("既定フォントのClear Dynamic Data On Buildを無効にしてください。");
            }

            if (isDynamic && requireMultiAtlas && !IsMultiAtlasEnabled)
            {
                errors.Add("既定フォントのMulti Atlas Texturesを有効にしてください。");
            }

            // DynamicOSはOSフォントを使うため、ルール5・6を適用しない。
            if (PopulationMode == DYNAMIC_MODE && !HasSourceFont)
            {
                errors.Add("DynamicフォントのSource Font Fileを設定してください。");
            }

            if (PopulationMode != DYNAMIC_OS_MODE)
            {
                if (!IsDefaultFontDependency)
                {
                    errors.Add("既定フォントがText Settingsのビルド依存に含まれていません。");
                }

                if (PopulationMode == DYNAMIC_MODE && HasSourceFont && !IsSourceFontDependency)
                {
                    errors.Add("Source Font FileがText Settingsのビルド依存に含まれていません。");
                }
            }

            // Staticでは元フォントから生成できないため、Character Tableだけを見る。
            HashSet<uint> available = new(Characters ?? Array.Empty<uint>());
            if (isDynamic && HasSourceFont) { available.UnionWith(SourceCharacters ?? Array.Empty<uint>()); }
            List<string> missing = new();
            foreach (uint unicode in EnumerateCharacters(requiredCharacters))
            {
                if (available.Contains(unicode)) { continue; }
                missing.Add($"{char.ConvertFromUtf32((int)unicode)} (U+{unicode:X4})");
            }

            if (missing.Count != 0) { errors.Add("必須文字がありません。Missing: " + string.Join(", ", missing)); }
            return errors.AsReadOnly();
        }

        /// <summary>
        ///     必須文字を重複しないUnicodeスカラー値へ分解する。
        /// </summary>
        internal static IEnumerable<uint> EnumerateCharacters(string text)
        {
            HashSet<uint> visited = new();
            string characters = text ?? string.Empty;
            for (int index = 0; index < characters.Length; index++)
            {
                uint unicode = characters[index];
                if (char.IsHighSurrogate(characters[index])
                    && index + 1 < characters.Length && char.IsLowSurrogate(characters[index + 1]))
                {
                    unicode = (uint)char.ConvertToUtf32(characters[index], characters[index + 1]);
                    index++;
                }
                else if (char.IsSurrogate(characters[index]))
                {
                    unicode = REPLACEMENT_CHARACTER;
                }

                if (visited.Add(unicode)) { yield return unicode; }
            }
        }

        #endregion

        #region 内部処理

        private const int DYNAMIC_MODE = 1;
        private const int DYNAMIC_OS_MODE = 2;
        private const uint REPLACEMENT_CHARACTER = 0xFFFD;

        #endregion
    }
}
