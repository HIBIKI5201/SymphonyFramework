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
        public static void LogDirect(string text,
            LogKind kind = LogKind.Normal,
            UnityEngine.Object @object = null)
        {
            switch (kind)
            {
                case LogKind.Normal: Debug.Log(text, @object); break;
                case LogKind.Warning: Debug.LogWarning(text, @object); break;
                case LogKind.Error: Debug.LogError(text, @object); break;
            }
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
        /// <param name="kind"></param>
        /// <param name="text"></param>
        /// <param name="clearText"></param>
        [HideInCallstack]
        public static void LogText(LogKind kind = LogKind.Normal,
            string text = null,
            bool clearText = true,
            UnityEngine.Object @object = null)
        {
            if (_logText == null) return;
            if (!string.IsNullOrEmpty(text)) _logText.AppendLine(text);

            LogDirect(_logText.ToString().TrimEnd(), kind, @object);

            if (clearText) _logText = null;
        }

        /// <summary>
        ///     追加されたメッセージをログに出力する。
        ///     （エディタのみ）
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="text"></param>
        /// <param name="clearText"></param>
        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public static void LogTextForEditor(LogKind kind = LogKind.Normal,
            string text = null,
            bool clearText = true,
            UnityEngine.Object @object = null)
        {
#if UNITY_EDITOR
            LogText(kind, text, clearText, @object);
#endif
        }

        /// <summary>
        ///     ログのテキストにメッセージを追加する。
        /// </summary>
        /// <param name="text"></param>
        public static void AddText(string text)
        {
            if (_logText == null)
            {
                _logText = new(text);
            }
            else
            {
                _logText.AppendLine(text);
            }
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
        /// <param name="text"></param>
        /// <param name="isOldTextLog">古いテキストが残っていたら出力するかどうか</param>
        public static void NewText(string text = null, bool isOldTextLog = false)
        {
            // 古いテキストがあれば出力する。
            if (_logText != null && isOldTextLog) LogText();

            _logText = string.IsNullOrEmpty(text) ? new() : new(text);
        }

        /// <summary>
        ///     追加されたメッセージを削除し新しくする。
        ///     （エディタのみ）
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void NewTextForEditor(string text = null)
        {
#if UNITY_EDITOR
            NewText(text);
#endif
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
            LogDirect(text, kind);
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
            LogText(kind, clearText: clearText);
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