using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockの依存グラフから算出した実行層または検証エラーを表す。
    /// </summary>
    internal readonly struct SceneBlockPlanResult
    {
        #region 外部向けAPI

        /// <summary> 計画の作成に成功した場合はtrue。 </summary>
        internal bool IsSuccess { get; }

        /// <summary> 依存順に並んだトポロジカル層。 </summary>
        internal IReadOnlyList<IReadOnlyList<string>> Layers { get; }

        /// <summary> 依存グラフから検出した異常。 </summary>
        internal IReadOnlyList<SceneBlockPlanError> Errors { get; }

        /// <summary>
        ///     トポロジカル層から成功結果を生成する。
        /// </summary>
        /// <param name="layers"> 依存順に並んだトポロジカル層。 </param>
        /// <returns> 成功結果。 </returns>
        /// <exception cref="ArgumentNullException"> layersがnullの場合。 </exception>
        internal static SceneBlockPlanResult Success(IReadOnlyList<IReadOnlyList<string>> layers)
        {
            // 成功結果にはエラーを含めず、実行層だけを不変なスナップショットとして保持する。
            return new SceneBlockPlanResult(
                true,
                CopyLayersAsReadOnly(layers),
                Array.Empty<SceneBlockPlanError>());
        }

        /// <summary>
        ///     検証エラーから失敗結果を生成する。
        /// </summary>
        /// <param name="errors"> 依存グラフから検出した異常。 </param>
        /// <returns> 失敗結果。 </returns>
        /// <exception cref="ArgumentNullException"> errorsがnullの場合。 </exception>
        internal static SceneBlockPlanResult Failure(IReadOnlyList<SceneBlockPlanError> errors)
        {
            // 失敗結果には実行層を含めず、検出した全エラーだけを保持する。
            return new SceneBlockPlanResult(
                false,
                Array.Empty<IReadOnlyList<string>>(),
                CopyErrorsAsReadOnly(errors));
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     成否と対応する値から計画結果を生成する。
        /// </summary>
        /// <param name="isSuccess"> 成功結果の場合はtrue。 </param>
        /// <param name="layers"> 成功時のトポロジカル層。 </param>
        /// <param name="errors"> 失敗時の検証エラー。 </param>
        private SceneBlockPlanResult(
            bool isSuccess,
            IReadOnlyList<IReadOnlyList<string>> layers,
            IReadOnlyList<SceneBlockPlanError> errors)
        {
            // Factoryが確定した成功値または失敗値をそのまま保持する。
            IsSuccess = isSuccess;
            Layers = layers;
            Errors = errors;
        }

        /// <summary>
        ///     トポロジカル層を変更不能なスナップショットへ変換する。
        /// </summary>
        /// <param name="layers"> コピー元のトポロジカル層。 </param>
        /// <returns> 読み取り専用のコピー。 </returns>
        /// <exception cref="ArgumentNullException"> layersまたは層内の一覧がnullの場合。 </exception>
        private static IReadOnlyList<IReadOnlyList<string>> CopyLayersAsReadOnly(
            IReadOnlyList<IReadOnlyList<string>> layers)
        {
            // トポロジカル層は成功結果の必須値として扱う。
            if (layers == null) { throw new ArgumentNullException(nameof(layers)); }

            // 層と各層のノード一覧をどちらも呼び出し側から変更できない形へコピーする。
            IReadOnlyList<string>[] layerSnapshots = new IReadOnlyList<string>[layers.Count];
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                IReadOnlyList<string> layer = layers[layerIndex];
                if (layer == null) { throw new ArgumentNullException(nameof(layers)); }

                string[] nodeIds = new string[layer.Count];
                for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++)
                {
                    nodeIds[nodeIndex] = layer[nodeIndex];
                }

                layerSnapshots[layerIndex] = Array.AsReadOnly(nodeIds);
            }

            return Array.AsReadOnly(layerSnapshots);
        }

        /// <summary>
        ///     検証エラーを変更不能なスナップショットへ変換する。
        /// </summary>
        /// <param name="errors"> コピー元の検証エラー。 </param>
        /// <returns> 読み取り専用のコピー。 </returns>
        /// <exception cref="ArgumentNullException"> errorsがnullの場合。 </exception>
        private static IReadOnlyList<SceneBlockPlanError> CopyErrorsAsReadOnly(
            IReadOnlyList<SceneBlockPlanError> errors)
        {
            // 検証エラー一覧は失敗結果の必須値として扱う。
            if (errors == null) { throw new ArgumentNullException(nameof(errors)); }

            // 呼び出し側の一覧変更が失敗結果へ反映されないようコピーする。
            SceneBlockPlanError[] snapshot = new SceneBlockPlanError[errors.Count];
            for (int index = 0; index < errors.Count; index++)
            {
                snapshot[index] = errors[index];
            }

            return Array.AsReadOnly(snapshot);
        }

        #endregion
    }
}
