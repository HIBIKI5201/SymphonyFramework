using SymphonyFrameWork.Core;
using SymphonyFrameWork.System;
using System;
using System.Threading;
using System.Threading.Tasks;
using SymphonyFrameWork.Exceptions;


#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SymphonyFrameWork.Debugger.HUD
{
    /// <summary>
    ///     画面上にデバッグ用のHUDを表示するクラス
    /// </summary>
    public static class SymphonyDebugHUD
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

        /// <summary>
        ///     SymphonyDebugHUDに追加のテキストを登録する。
        /// </summary>
        /// <param name="textFunc"> 毎フレーム表示文字列を返す処理。 </param>
        public static void AddText(Func<string> textFunc)
        {
            if (_debugHUD == null)
            {
                throw new SymphonyNotInitializedException(typeof(SymphonyDebugHUD));
            }

                _debugHUD.Value.Add(textFunc);
        }

        /// <summary>
        ///     SymphonyDebugHUDから追加のテキストを解除する。
        /// </summary>
        /// <param name="textFunc"> 解除する文字列生成処理。 </param>
        public static void RemoveText(Func<string> textFunc)
        {
            if (_debugHUD == null)
            {
                throw new SymphonyNotInitializedException(typeof(SymphonyDebugHUD));
            }

            _debugHUD.Value.Remove(textFunc);
        }

        /// <summary>
        ///     SymphonyDebugHUDに追加のテキストを表示する。
        /// </summary>
        /// <param name="text"> 一時表示する文字列。 </param>
        /// <param name="duration"> 表示を継続する秒数。 </param>
        /// <param name="color"> 文字へ適用する色。既定値の場合は色指定なし。 </param>
        /// <param name="token"> 表示待機を中断するためのトークン。 </param>
        public static async ValueTask AddText(string text, float duration = 3, Color color = default, CancellationToken token = default)
        {
            if (_debugHUD == null)
            {
                throw new SymphonyNotInitializedException(typeof(SymphonyDebugHUD));
            }


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

        /// <summary> 既存HUDを破棄し、遅延生成状態を初期化する。 </summary>
        internal static void Initialize()
        {
            if (_debugHUD?.IsValueCreated ?? false)
            {
                UnityEngine.Object.Destroy(_debugHUD.Value.gameObject);
                _debugHUD = null;
            }

            _debugHUD = new Lazy<SymphonyHUDDrawer>(CreateDebugHUD);
        }

        private static Lazy<SymphonyHUDDrawer> _debugHUD;

        /// <summary> SymphonyのシステムオブジェクトとしてHUD描画コンポーネントを生成する。 </summary>
        /// <returns> 生成したHUD描画コンポーネント。 </returns>
        private static SymphonyHUDDrawer CreateDebugHUD()
        {
            return SymphonyCoreSystem.CreateSystemObject<SymphonyHUDDrawer>();
        }
    }
}
