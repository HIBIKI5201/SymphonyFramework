using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SymphonyFrameWork.Debugger
{
    /// <summary>
    ///     UnityEditor上のみのログを発行する
    /// </summary>
    public static class SymphonyDebugLogger
    {
        public enum LogKind
        {
            Normal,
            Warning,
            Error,
        }

        /// <summary>
        ///     エディタ上でのみ出力されるデバッグログ
        /// </summary>
        /// <param name="text"></param>
        [Conditional("UNITY_EDITOR")]
        public static void DirectLog(string text, LogKind kind = LogKind.Normal)
        {
#if UNITY_EDITOR
            GetDebugActionByKind(kind)?.Invoke(text);
#endif
        }

        /// <summary>
        ///     ログのテキストにメッセージを追加する
        /// </summary>
        /// <param name="text"></param>
        [Conditional("UNITY_EDITOR")]
        public static void AddText(string text)
        {
#if UNITY_EDITOR
            _logText += $"{text}\n";
#endif
        }

        /// <summary>
        ///     追加されたメッセージを削除する
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void ClearText()
        {
#if UNITY_EDITOR
            _logText = string.Empty;
#endif
        }

        /// <summary>
        ///     追加されたメッセージをログに出力する
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void TextLog(LogKind kind = LogKind.Normal, bool clearText = true)
        {
#if UNITY_EDITOR
            GetDebugActionByKind(kind)?.Invoke(_logText);
            if (clearText) ClearText();
#endif
        }

        /// <summary>
        ///     コンポーネントがnullだった場合に警告を表示する。
        ///     戻り値にnullだったかを返す
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object"></param>
        /// <returns>nullならtrue、nullではないならfalse</returns>
        [HideInCallstack]
        public static bool LogAndCheckComponentNull<T>(this T @object)
        {
            bool isNull = @object == null;

            if (isNull)
            {
                Debug.LogWarning($"<b>{typeof(T).Name}</b> is null");
            }

            return isNull;
        }

#if UNITY_EDITOR
        private static string _logText = string.Empty;

        private static Action<object> GetDebugActionByKind(LogKind kind) =>
            kind switch
            {
                LogKind.Normal => Debug.Log,
                LogKind.Warning => Debug.LogWarning,
                LogKind.Error => Debug.LogError,
                _ => Debug.Log
            };
#endif

        /// <summary>
        ///     コンポーネントだった場合に警告を表示する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        [Conditional("UNITY_EDITOR")]
        public static void CheckComponentNull<T>(this T component) where T : Component
        {
#if UNITY_EDITOR
            if (component == null) Debug.LogWarning($"The component {typeof(T).Name} of {component.name} is null.");
#endif
        }

        #region Obsolete機能
        [Obsolete("この機能は安全性が保障されていません。CheckComponentNullを使用してください")]
        public static bool IsComponentNotNull<T>(this T component) where T : Component
        {
            if (component == null)
            {
                Debug.LogWarning($"The component of type {typeof(T).Name} is null.");
                return false;
            }

            return true;
        }
        #endregion
    }
}