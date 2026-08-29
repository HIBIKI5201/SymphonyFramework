using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockの依存グラフで検出した異常を表す。
    /// </summary>
    internal readonly struct SceneBlockPlanError
    {
        #region 外部向けAPI

        /// <summary>
        ///     異常の種類と関連するノード識別子から検証エラーを生成する。
        /// </summary>
        /// <param name="kind"> 異常の種類。 </param>
        /// <param name="nodeIds"> 異常に関連するノード識別子。 </param>
        /// <exception cref="ArgumentNullException"> nodeIdsがnullの場合。 </exception>
        internal SceneBlockPlanError(
            SceneBlockPlanErrorEnum kind,
            IReadOnlyList<string> nodeIds)
        {
            // 呼び出し側の一覧変更で検証結果が変わらないよう、読み取り専用スナップショットを保持する。
            Kind = kind;
            NodeIds = CopyAsReadOnly(nodeIds);
        }

        /// <summary> 異常の種類。 </summary>
        internal SceneBlockPlanErrorEnum Kind { get; }

        /// <summary> 異常に関連するノード識別子。 </summary>
        internal IReadOnlyList<string> NodeIds { get; }

        #endregion

        #region 内部処理

        /// <summary>
        ///     文字列一覧を変更不能なスナップショットへ変換する。
        /// </summary>
        /// <param name="source"> コピー元の一覧。 </param>
        /// <returns> 読み取り専用のコピー。 </returns>
        /// <exception cref="ArgumentNullException"> sourceがnullの場合。 </exception>
        private static IReadOnlyList<string> CopyAsReadOnly(IReadOnlyList<string> source)
        {
            // エラーの関連ノード一覧は必須の値として扱う。
            if (source == null) { throw new ArgumentNullException(nameof(source)); }

            // 元の一覧と配列のどちらからも変更できない読み取り専用コピーを返す。
            string[] snapshot = new string[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                snapshot[index] = source[index];
            }

            return Array.AsReadOnly(snapshot);
        }

        #endregion
    }
}
