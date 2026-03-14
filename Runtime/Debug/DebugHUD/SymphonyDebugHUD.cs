using SymphonyFrameWork.Core;
using SymphonyFrameWork.System;
using System;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SymphonyFrameWork.Debugger.HUD
{
    /// <summary>
    ///     画面上にデバッグ用のHUDを表示するクラス
    /// </summary>
    [DefaultExecutionOrder(1000)] // 他のシステムよりも後に実行されるようにする。
    public class SymphonyDebugHUD : MonoBehaviour
    {
        /// <summary>
        ///     HUDを表示する。
        /// </summary>
#if UNITY_EDITOR
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(SymphonyDebugHUD) + "/" + nameof(Show))]
#endif
        public static void Show()
        {
            _ = _debugHUD.Value; // アクセスしてインスタンスを作成。
        }

        /// <summary>
        ///     HUDを非表示にする。
        /// </summary>
#if UNITY_EDITOR
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(SymphonyDebugHUD) + "/" + nameof(Hide))]
#endif
        public static void Hide()
        {
            Initialize();
        }

        public static void AddText(Func<string> textFunc)
        {
            _debugHUD.Value.Add(textFunc);
        }

        public static void RemoveText(Func<string> textFunc)
        {
            _debugHUD.Value.Remove(textFunc);
        }

        /// <summary>
        ///     SymphonyDebugHUDに追加のテキストを追加する。
        /// </summary>
        /// <param name="text"></param>
        public static async void AddText(string text, float duration, Color color = default, CancellationToken token = default)
        {
            if (color != default) //カラーを指定する
            {
                string colorHex = ColorUtility.ToHtmlStringRGB(color);
                text = $"<color=#{colorHex}>{text}</color>";
            }

            Func<string> textFunc = () => text;

            _debugHUD.Value.Add(textFunc);

            try
            {
                await Awaitable.WaitForSecondsAsync(duration, token);
            }
            finally
            {
                _debugHUD.Value.Remove(textFunc);
            }
        }

        internal static void Initialize()
        {
            if (_debugHUD?.IsValueCreated ?? false)
            {
                Destroy(_debugHUD.Value.gameObject);
            }

            _debugHUD = new Lazy<SymphonyHUDDrawer>(CreateDebugHUD);
        }

        private static Lazy<SymphonyHUDDrawer> _debugHUD;

        private static SymphonyHUDDrawer CreateDebugHUD()
        {
            return SymphonyCoreSystem.CreateSystemObject<SymphonyHUDDrawer>();
        }
    }
}