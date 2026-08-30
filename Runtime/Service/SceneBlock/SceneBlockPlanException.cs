using System;
using System.Collections.Generic;
using System.Text;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockの依存グラフが実行順を決められない場合に発生する例外。
    /// </summary>
    /// <remarks>
    ///     アセットの記述誤りを表す。**検出した異常をすべて <see cref="Descriptions" /> へ載せ、1件目で打ち切らない。**
    /// </remarks>
    public sealed class SceneBlockPlanException : Exception
    {
        #region 外部向けAPI

        /// <summary>
        ///     ブロック名と検出した異常の説明を指定して例外を生成する。
        /// </summary>
        /// <param name="blockName"> 異常を検出したブロック名。 </param>
        /// <param name="descriptions"> 検出した全異常の説明。 </param>
        /// <exception cref="ArgumentNullException"> descriptionsがnullの場合。 </exception>
        /// <remarks>
        ///     生成するのは依存グラフを検証したフレームワーク側だけであるため internal にする。
        /// </remarks>
        internal SceneBlockPlanException(string blockName, IReadOnlyList<string> descriptions)
            : base(BuildMessage(blockName, descriptions))
        {
            // メッセージと構造化プロパティへ同じ検証結果を保持する。
            BlockName = blockName;
            Descriptions = CopyAsReadOnly(descriptions);
        }

        /// <summary> 異常を検出したブロック名。 </summary>
        public string BlockName { get; }

        /// <summary> 検出した全異常の説明。 </summary>
        public IReadOnlyList<string> Descriptions { get; }

        #endregion

        #region 内部処理

        /// <summary> Consoleでサブシステムを判別するためのメッセージ接頭辞。 </summary>
        private const string LOG_PREFIX = "[SceneBlock]";

        /// <summary>
        ///     ブロック名と全異常を1つのメッセージへまとめる。
        /// </summary>
        /// <param name="blockName"> 異常を検出したブロック名。 </param>
        /// <param name="descriptions"> 検出した全異常の説明。 </param>
        /// <returns> 例外メッセージ。 </returns>
        /// <exception cref="ArgumentNullException"> descriptionsがnullの場合。 </exception>
        private static string BuildMessage(string blockName, IReadOnlyList<string> descriptions)
        {
            // 説明が無い例外は原因を伝えられないため、生成時点で拒否する。
            if (descriptions == null) { throw new ArgumentNullException(nameof(descriptions)); }

            StringBuilder builder = new();
            builder.Append($"{LOG_PREFIX} Scene Block {blockName} の依存関係を解決できませんでした。");

            // Consoleの1行目で件数が分かるようにしてから、全異常を改行区切りで並べる。
            builder.Append($" 検出した異常: {descriptions.Count}件。");
            foreach (string description in descriptions)
            {
                builder.AppendLine();
                builder.Append("- ");
                builder.Append(description);
            }

            return builder.ToString();
        }

        /// <summary>
        ///     説明一覧を変更不能なスナップショットへ変換する。
        /// </summary>
        /// <param name="source"> コピー元の一覧。 </param>
        /// <returns> 読み取り専用のコピー。 </returns>
        private static IReadOnlyList<string> CopyAsReadOnly(IReadOnlyList<string> source)
        {
            // 呼び出し側が後から一覧を書き換えても、例外の内容が変わらないようにする。
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
