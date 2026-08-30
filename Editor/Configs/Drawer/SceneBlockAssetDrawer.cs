using System.Collections.Generic;
using System.Text;

using SymphonyFrameWork.System.SceneBlock;

using UnityEditor;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     SceneBlockAssetの依存グラフの検証結果と実行層をInspectorへ表示する。
    /// </summary>
    /// <remarks>
    ///     **Play Modeに入る前に循環依存や欠落参照へ気づけるようにするための表示である。**
    ///     判定はRuntimeと同じ <c>SceneBlockEntryReader</c> を使い、Editorへ規則を複製しない。
    /// </remarks>
    [CustomEditor(typeof(SceneBlockAsset))]
    public sealed class SceneBlockAssetDrawer : UnityEditor.Editor
    {
        #region 外部向けAPI

        /// <summary>
        ///     既定のInspectorに続けて依存グラフの検証結果を描画する。
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // 対象を取得できない場合は、追加表示だけを省いて既定の描画に留める。
            if (target is not SceneBlockAsset asset) { return; }

            EditorGUILayout.Space();

            // 空のブロックはロードしても何も起きないため、検証ではなく案内を出す。
            if (asset.Entries.Count == 0)
            {
                EditorGUILayout.HelpBox("エントリを1件以上追加してください。", MessageType.Info);
                return;
            }

            if (SceneBlockEntryReader.TryCreateLayers(
                    asset.Entries,
                    out IReadOnlyList<IReadOnlyList<string>> layers,
                    out IReadOnlyList<string> errorDescriptions))
            {
                EditorGUILayout.HelpBox(BuildLayerText(layers), MessageType.Info);
                return;
            }

            // 検出した異常は1件目で打ち切らず、直す順序を選べるようにすべて並べる。
            EditorGUILayout.HelpBox(BuildErrorText(errorDescriptions), MessageType.Error);
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     算出した実行層を表示用テキストへまとめる。
        /// </summary>
        /// <param name="layers"> 依存順に並んだ実行層。 </param>
        /// <returns> 層ごとのシーン名を並べたテキスト。 </returns>
        private static string BuildLayerText(IReadOnlyList<IReadOnlyList<string>> layers)
        {
            StringBuilder builder = new();
            builder.Append($"依存関係を解決しました。{layers.Count}段階でロードします。");

            // 同じ段階のシーンは同時にロードされることが読み取れる形にする。
            for (int index = 0; index < layers.Count; index++)
            {
                builder.AppendLine();
                builder.Append($"{index + 1}. {string.Join(", ", layers[index])}");
            }

            return builder.ToString();
        }

        /// <summary>
        ///     検出した異常を表示用テキストへまとめる。
        /// </summary>
        /// <param name="errorDescriptions"> 検出した全異常の説明。 </param>
        /// <returns> 異常を並べたテキスト。 </returns>
        private static string BuildErrorText(IReadOnlyList<string> errorDescriptions)
        {
            StringBuilder builder = new();
            builder.Append($"依存関係を解決できません。異常が{errorDescriptions.Count}件あります。");

            foreach (string description in errorDescriptions)
            {
                builder.AppendLine();
                builder.Append($"- {description}");
            }

            return builder.ToString();
        }

        #endregion
    }
}
