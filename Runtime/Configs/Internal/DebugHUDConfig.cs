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
            // CompositeSyntax.With は内部で InputSystem.manager を参照するため、Input System の初期化前に呼ぶと
            // NullReferenceException になる。このメソッドはフィールド初期化子からも呼ばれ、Domain Reload 直後の
            // デシリアライズでは Input System より先に実行されることがあるため、Binding 構造体を直接追加する。
            // 未有効化の新規 Action への AddBinding は InputSystem.manager を参照しない。
            InputAction action = new("Toggle Debug HUD", InputActionType.Button);
            action.AddBinding(new InputBinding
            {
                name = "ButtonWithTwoModifiers",
                path = "ButtonWithTwoModifiers",
                isComposite = true,
            });
            AddCompositePart(action, "Modifier1", "<Keyboard>/shift");
            AddCompositePart(action, "Modifier2", "<Keyboard>/d");
            AddCompositePart(action, "Button", "<Keyboard>/p");
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

        /// <summary>
        ///     Input System の初期化状態に依存せず、Composite の part Binding を追加する。
        /// </summary>
        /// <param name="action"> part を追加する Input Action。 </param>
        /// <param name="partName"> Composite の part 名。 </param>
        /// <param name="path"> part が参照する Control path。 </param>
        private static void AddCompositePart(InputAction action, string partName, string path)
        {
            action.AddBinding(new InputBinding
            {
                name = partName,
                path = path,
                isPartOfComposite = true,
            });
        }

        [SerializeField, Tooltip("Debug HUDの表示と非表示を切り替えるInput Action。")]
        private InputAction _toggleAction = CreateDefaultToggleAction();

        #endregion
    }
}
