using UnityEngine;
using UnityEngine.InputSystem;

namespace SymphonyFrameWork.Config
{
    /// <summary>
    ///     Debug HUDの表示切り替えに使用するInput Actionを保持する。
    /// </summary>
    /// <remarks>
    ///     利用側コードへ設定型を公開せず、Project Settingsからだけ編集する。
    /// </remarks>
    internal sealed class DebugHUDConfig : ScriptableObject
    {
        #region 外部向けAPI

        /// <summary> HUDの表示と非表示を切り替えるInput Action。 </summary>
        public InputAction ToggleAction => _toggleAction;

        /// <summary>
        ///     Shift + D + Pを既定Bindingに持つInput Actionを生成する。
        /// </summary>
        /// <returns> 呼び出し側が所有する新しいInput Action。 </returns>
        internal static InputAction CreateDefaultToggleAction()
        {
            InputAction action = new("Toggle Debug HUD", InputActionType.Button);
            action.AddCompositeBinding("ButtonWithTwoModifiers")
                .With("Modifier1", "<Keyboard>/shift")
                .With("Modifier2", "<Keyboard>/d")
                .With("Button", "<Keyboard>/p");
            return action;
        }

        /// <summary>
        ///     シリアライズ済みActionを再構築し、古い内部ActionMapキャッシュを破棄する。
        /// </summary>
        /// <remarks>
        ///     Domain Reload無効時にSerializedObjectからBinding配列を変更すると、Input Systemが
        ///     非シリアライズで保持するActionMapだけが旧配列を参照し続けるため、保存直後に呼ぶ。
        /// </remarks>
        internal void RebuildToggleActionSerializationState()
        {
            if (_toggleAction == null) { return; }

            string serializedAction = JsonUtility.ToJson(_toggleAction);
            InputAction rebuiltAction = JsonUtility.FromJson<InputAction>(serializedAction);
            if (rebuiltAction == null)
            {
                throw new global::System.InvalidOperationException(
                    "Debug HUDのInput Actionを再構築できませんでした。");
            }

            _toggleAction.Dispose();
            _toggleAction = rebuiltAction;
        }

        #endregion

        #region 内部処理

        [SerializeField, Tooltip("Debug HUDの表示と非表示を切り替えるInput Action。")]
        private InputAction _toggleAction = CreateDefaultToggleAction();

        #endregion
    }
}
