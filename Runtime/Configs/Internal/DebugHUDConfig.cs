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

        #endregion

        #region 内部処理

        [SerializeField, Tooltip("Debug HUDの表示と非表示を切り替えるInput Action。")]
        private InputAction _toggleAction = CreateDefaultToggleAction();

        #endregion
    }
}
