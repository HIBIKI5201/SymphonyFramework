using System;

using SymphonyFrameWork.Core;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     SymphonyFrameWorkのディレクトリを保護するクラス。
    /// </summary>
    public sealed class SymphonyAssetProtector : AssetPostprocessor
    {
        /// <summary> host callbackによる状態リセットが処理待ちになったときに通知する。 </summary>
        internal static event Action OnHostChangesPending;

        private static bool _hasDisplayedEnabledDialog;
        private static bool _hasPendingOperationReset;
        private static AssetMoveResult? _warningMoveResult;

        /// <summary> 1回のアセット操作で保持したダイアログ状態を破棄する。 </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
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
            if (!IsFrameworkAsset(sourcePath))
            {
                return AssetMoveResult.DidNotMove;
            }

            SymphonyUserSettingConfig config =
                SymphonyEditorConfigLocator.GetConfig<SymphonyUserSettingConfig>();

            switch (config.AssetProtectionMode)
            {
                case AssetProtectionModeEnum.Enabled:
                    return PreventMove(sourcePath);
                case AssetProtectionModeEnum.Warning:
                    return ConfirmMove(sourcePath);
                case AssetProtectionModeEnum.Disabled:
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
            if (_warningMoveResult.HasValue)
            {
                return _warningMoveResult.Value;
            }

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

        /// <summary> 指定したパスがFrameworkルートまたはその配下かを判定する。 </summary>
        /// <param name="sourcePath"> 判定するアセットパス。 </param>
        /// <returns> Framework配下の場合はtrue。 </returns>
        private static bool IsFrameworkAsset(string sourcePath)
        {
            string frameworkPath = EditorSymphonyConstant.FRAMEWORK_PATH;
            return sourcePath.Equals(frameworkPath, StringComparison.Ordinal) ||
                   sourcePath.StartsWith(frameworkPath + "/", StringComparison.Ordinal);
        }

        /// <summary> coalesceしたアセット操作状態のリセットを1回だけ処理する。 </summary>
        internal static void ProcessPendingChanges()
        {
            if (!_hasPendingOperationReset)
            {
                return;
            }

            _hasPendingOperationReset = false;
            _hasDisplayedEnabledDialog = false;
            _warningMoveResult = null;
        }
    }
}
