using System;

using SymphonyFrameWork.Core;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Framework配下のアセット移動を保護する。
    /// </summary>
    /// <remarks>
    ///     postprocess callbackでは変更を記録してSymphonyEditorOrchestratorへ通知し、
    ///     操作状態のリセットはOrchestratorがcoalesceして処理する。
    /// </remarks>
    public sealed class SymphonyAssetProtector : AssetPostprocessor
    {
        #region 外部向けAPI

        /// <summary> host callbackによる状態リセットが処理待ちになったときに通知する。 </summary>
        internal static event Action OnHostChangesPending;

        /// <summary>
        ///     coalesceしたアセット操作状態のリセットを1回だけ処理する。
        /// </summary>
        internal static void ProcessPendingChanges()
        {
            // postprocess通知が無ければ、進行中の移動操作で共有する選択状態を維持する。
            if (!_hasPendingOperationReset) { return; }

            // 1回のアセット操作が終わった後だけ、次の操作でダイアログを再表示できる状態へ戻す。
            _hasPendingOperationReset = false;
            _hasDisplayedEnabledDialog = false;
            _warningMoveResult = null;
        }

        #endregion

        #region 内部処理

        private static bool _hasDisplayedEnabledDialog;
        private static bool _hasPendingOperationReset;
        private static AssetMoveResult? _warningMoveResult;

        /// <summary>
        ///     アセット操作後の状態リセットを処理待ちとして記録する。
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // AssetPostprocessor内で後処理せず、再入を集約するOrchestratorへリセット要求だけを渡す。
            _hasPendingOperationReset = true;
            OnHostChangesPending?.Invoke();
        }

        /// <summary>
        ///     Framework配下のアセット移動に現在の保護モードを適用する。
        /// </summary>
        /// <param name="sourcePath"> 移動前のアセットパス。 </param>
        /// <param name="destinationPath"> 移動先のアセットパス。 </param>
        /// <returns> Unityの通常移動へ委譲するか、移動を失敗させるかを示す値。 </returns>
        private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            // Framework外の移動は保護対象ではないため、Unityの通常処理へ委ねる。
            if (!IsFrameworkAsset(sourcePath)) { return AssetMoveResult.DidNotMove; }

            // 利用者ごとの保護モードを移動判定時に読み、設定変更を即座に反映する。
            SymphonyUserSettingConfig config =
                SymphonyEditorConfigLocator.GetConfig<SymphonyUserSettingConfig>();

            // 保護を強制するか、操作単位で確認するか、Unityへ委譲するかを設定どおりに選ぶ。
            switch (config.AssetProtectionMode)
            {
                case AssetProtectionModeEnum.Enabled:
                    return PreventMove(sourcePath);
                case AssetProtectionModeEnum.Warning:
                    return ConfirmMove(sourcePath);
                case AssetProtectionModeEnum.Disabled:
                    // 無効時も意図しない移動を追跡できるよう、移動元と移動先を記録する。
                    Debug.Log(
                        $"[{nameof(SymphonyAssetProtector)}] SymphonyFrameWork配下のアセットを移動します。" +
                        $" path: '{sourcePath}', destination: '{destinationPath}'");
                    return AssetMoveResult.DidNotMove;
                default:
                    return AssetMoveResult.FailedMove;
            }
        }

        /// <summary>
        ///     Enabledモードの通知を1回だけ表示して移動を拒否する。
        /// </summary>
        /// <param name="sourcePath"> 移動前のアセットパス。 </param>
        /// <returns> 移動を失敗させる値。 </returns>
        private static AssetMoveResult PreventMove(string sourcePath)
        {
            // 複数アセットを含む同一操作ではダイアログを1回に抑え、すべての移動を拒否する。
            if (!_hasDisplayedEnabledDialog)
            {
                EditorUtility.DisplayDialog(
                    "移動禁止",
                    $"SymphonyFrameWorkは移動できません。\npath: '{sourcePath}'",
                    "OK");
                _hasDisplayedEnabledDialog = true;
            }

            return AssetMoveResult.FailedMove;
        }

        /// <summary>
        ///     Warningモードの選択を1回だけ取得し、同じ操作内で再利用する。
        /// </summary>
        /// <param name="sourcePath"> 移動前のアセットパス。 </param>
        /// <returns> 利用者の選択に対応する移動結果。 </returns>
        private static AssetMoveResult ConfirmMove(string sourcePath)
        {
            // 複数アセットを含む同一操作では、最初の選択を残りの移動にも適用する。
            if (_warningMoveResult.HasValue) { return _warningMoveResult.Value; }

            // Unityへ移動を委ねるか拒否するかを、利用者の明示的な選択へ対応付ける。
            bool shouldMove = EditorUtility.DisplayDialog(
                "移動注意",
                $"SymphonyFrameWorkを移動しようとしています。\n本当に移動しますか？\npath: '{sourcePath}'",
                "移動する",
                "元に戻す");
            _warningMoveResult = shouldMove
                ? AssetMoveResult.DidNotMove
                : AssetMoveResult.FailedMove;

            return _warningMoveResult.Value;
        }

        /// <summary>
        ///     指定したパスがFrameworkルートまたはその配下かを判定する。
        /// </summary>
        /// <param name="sourcePath"> 判定するアセットパス。 </param>
        /// <returns> Framework配下の場合はtrue。 </returns>
        private static bool IsFrameworkAsset(string sourcePath)
        {
            // 同名の別ディレクトリを誤検出しないよう、ルート完全一致か区切り文字付きの配下だけを許可する。
            string frameworkPath = EditorSymphonyConstant.FRAMEWORK_PATH;
            return sourcePath.Equals(frameworkPath, StringComparison.Ordinal) ||
                   sourcePath.StartsWith(frameworkPath + "/", StringComparison.Ordinal);
        }

        #endregion
    }
}
