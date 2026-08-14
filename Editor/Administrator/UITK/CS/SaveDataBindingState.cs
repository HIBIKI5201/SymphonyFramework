using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Save Data Inspectorのバインド元と表示値を解決する。
    /// </summary>
    internal static class SaveDataBindingState
    {
        #region 外部向けAPI

        /// <summary> 状態切り替え時に除去する全ランプUSSクラス名。 </summary>
        internal static IReadOnlyList<string> AllLampUssClassNames => LAMP_USS_CLASS_NAMES;

        /// <summary>
        ///     選択状態とインスタンスの所有元からバインド状態を解決する。
        /// </summary>
        /// <param name="hasSelection"> 対象型が選択されているか。 </param>
        /// <param name="isRegistryLoaded"> Registry正本が読み込み済みか。 </param>
        /// <param name="isLocalContentLoaded"> Window専用インスタンスへ永続化データを読み込み済みか。 </param>
        /// <returns> 解決したバインド状態。 </returns>
        internal static SaveDataBindingSourceEnum Resolve(
            bool hasSelection,
            bool isRegistryLoaded,
            bool isLocalContentLoaded)
        {
            // 未選択では、背後の読み込み状態に関係なくInspectorへ何も接続しない。
            if (!hasSelection) { return SaveDataBindingSourceEnum.None; }

            // Registry正本はWindow専用インスタンスより優先して接続する。
            if (isRegistryLoaded) { return SaveDataBindingSourceEnum.Registry; }

            // 非接続時は、永続化データの読み込み完了前後を区別する。
            return isLocalContentLoaded
                ? SaveDataBindingSourceEnum.Loaded
                : SaveDataBindingSourceEnum.Instance;
        }

        /// <summary>
        ///     状態に対応するUSSクラス名を取得する。
        /// </summary>
        /// <param name="source"> バインド状態。 </param>
        /// <returns> 状態別のUSSクラス名。 </returns>
        internal static string GetLampUssClassName(SaveDataBindingSourceEnum source)
        {
            // UXMLと共有する接頭辞へ状態名を対応させる。
            return source switch
            {
                SaveDataBindingSourceEnum.None => "save-binding--none",
                SaveDataBindingSourceEnum.Instance => "save-binding--instance",
                SaveDataBindingSourceEnum.Loaded => "save-binding--loaded",
                SaveDataBindingSourceEnum.Registry => "save-binding--registry",
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
        }

        /// <summary>
        ///     状態に対応する表示文言を取得する。
        /// </summary>
        /// <param name="source"> バインド状態。 </param>
        /// <returns> 状態を説明する表示文言。 </returns>
        internal static string GetDisplayText(SaveDataBindingSourceEnum source)
        {
            // インスタンスの所有者が画面から判別できる短い文言へ変換する。
            return source switch
            {
                SaveDataBindingSourceEnum.None => "No selection",
                SaveDataBindingSourceEnum.Instance => "Window instance",
                SaveDataBindingSourceEnum.Loaded => "Loaded window instance",
                SaveDataBindingSourceEnum.Registry => "Connected to runtime instance",
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
        }

        /// <summary>
        ///     読み込み中または未保存状態を示す補足文言を組み立てる。
        /// </summary>
        /// <param name="isLoading"> Window専用インスタンスへ読み込み中か。 </param>
        /// <param name="isDirty"> Window専用インスタンスに未保存の変更があるか。 </param>
        /// <returns> 状態ラベルへ付加する文言。 </returns>
        internal static string BuildStatusSuffix(bool isLoading, bool isDirty)
        {
            // ロード中は編集不可でDirtyと同時成立しないため、進行表示を優先する。
            if (isLoading) { return "Loading…"; }

            return isDirty ? "未保存の変更あり" : string.Empty;
        }

        #endregion

        #region 内部処理

        private static readonly IReadOnlyList<string> LAMP_USS_CLASS_NAMES =
            Array.AsReadOnly(new[]
            {
                "save-binding--none",
                "save-binding--instance",
                "save-binding--loaded",
                "save-binding--registry"
            });

        #endregion
    }
}
