using System;
using System.Diagnostics;
using System.Text;
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
        ///     直接出力されるデバッグログ。
        /// </summary>
        /// <param name="text"></param>
        [HideInCallstack]
        public static void LogDirect(string text, LogKind kind = LogKind.Normal)
        {
            GetDebugActionByKind(kind)?.Invoke(text);
        }

        /// <summary>
        ///     直接出力されるデバッグログ。
        ///     （エディタのみ）
        /// </summary>
        /// <param name="text"></param>
        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public static void LogDirectForEditor(string text, LogKind kind = LogKind.Normal)
        {
#if UNITY_EDITOR
            LogDirect(text, kind);
#endif
        }

        /// <summary>
        ///     追加されたメッセージをログに出力する。
        /// </summary>
        [HideInCallstack]
        public static void LogText(LogKind kind = LogKind.Normal,
            string text = null, 
            bool clearText = true)
        {
            if (!string.IsNullOrEmpty(text)) _logText.AppendLine(text);

            LogDirect(_logText.ToString().TrimEnd(), kind);

            if (clearText) NewText();
        }

        /// <summary>
        ///     ログのテキストにメッセージを追加する。
        /// </summary>
        /// <param name="text"></param>
        public static void AddText(string text)
        {
            if (_logText == null) NewText();
            _logText.AppendLine(text);
        }

        /// <summary>
        ///     ログのテキストにメッセージを追加する。
        ///     （エディタのみ）
        /// </summary>
        /// <param name="text"></param>
        [Conditional("UNITY_EDITOR")]
        public static void AddTextForEditor(string text)
        {
#if UNITY_EDITOR
            AddText(text);
#endif
        }

        /// <summary>
        ///     追加されたメッセージを削除し新しくする。
        /// </summary>
        public static void NewText(string text = null)
        {
            _logText = string.IsNullOrEmpty(text) ? new() : new(text);
        }

        /// <summary>
        ///     コンポーネントがnullだった場合に警告を表示する。
        ///     戻り値にnullだったかを返す。
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

        /// <summary> ログを管理する </summary>
        private static StringBuilder _logText = null;

        /// <summary>
        ///     LogKindからデバッグログのメソッドを返す。
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        private static Action<object> GetDebugActionByKind(LogKind kind) =>
            kind switch
            {
                LogKind.Normal => Debug.Log,
                LogKind.Warning => Debug.LogWarning,
                LogKind.Error => Debug.LogError,
                _ => Debug.Log
            };

        #region Obsolete機能
        /// <summary>
        ///     エディタ上でのみ出力されるデバッグログ
        /// </summary>
        /// <param name="text"></param>
        [Obsolete("この機能は旧型式です。" + nameof(LogDirect) + "を使用してください)")]
        [Conditional("UNITY_EDITOR")]
        public static void DirectLog(string text, LogKind kind = LogKind.Normal)
        {
#if UNITY_EDITOR
            GetDebugActionByKind(kind)?.Invoke(text);
#endif
        }

        /// <summary>
        ///     追加されたメッセージをログに出力する
        /// </summary>
        [Obsolete("この機能は旧型式です。" + nameof(LogText) + "を使用してください)")]
        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public static void TextLog(LogKind kind = LogKind.Normal, bool clearText = true)
        {
#if UNITY_EDITOR
            GetDebugActionByKind(kind)?.Invoke(_logText);
            if (clearText) NewText();
#endif
        }

        /// <summary>
        ///     コンポーネントだった場合に警告を表示する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        [Obsolete("この機能は旧型式です。" + nameof(LogAndCheckComponentNull) + "を使用してください。")]
        [Conditional("UNITY_EDITOR")]
        public static void CheckComponentNull<T>(this T component) where T : Component
        {
#if UNITY_EDITOR
            if (component == null) Debug.LogWarning($"The component {typeof(T).Name} of {component.name} is null.");
#endif
        }

        [Obsolete("この機能は安全性が保障されていません。" + nameof(LogAndCheckComponentNull) + "を使用してください")]
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