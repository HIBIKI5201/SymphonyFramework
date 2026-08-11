using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SymphonyFrameWork.Debugger.Logger
{
    /// <summary>
    ///     用途と重要度に応じたデバッグログを発行する。
    /// </summary>
    public static class SymphonyDebugLogger
    {
        #region 外部向けAPI

        /// <summary>
        ///     直接出力されるデバッグログ。
        /// </summary>
        /// <param name="text"> 出力する文字列。 </param>
        /// <param name="kind"> 出力するログの重要度。 </param>
        /// <param name="context"> Consoleから追跡可能にするUnityオブジェクト。 </param>
        [HideInCallstack]
        public static void LogDirect(string text,
            LogKindEnum kind = LogKindEnum.Normal,
            UnityEngine.Object context = null)
        {
            // 呼び出し側が指定した重要度をUnity Consoleの対応するログ種別へそのまま反映する。
            switch (kind) 
            {
                case LogKindEnum.Normal: Debug.Log(text, context); break;
                case LogKindEnum.Warning: Debug.LogWarning(text, context); break;
                case LogKindEnum.Error: Debug.LogError(text, context); break;
            }

            // Runtime層ではファイル出力を担わず、Editor側の購読者へ同じ内容を通知する。
            OnLogDirect?.Invoke(text, kind);
        }

        /// <summary>
        ///     Editorでのみデバッグログを直接出力する。
        /// </summary>
        /// <param name="text"> Editorでのみ出力する文字列。 </param>
        /// <param name="kind"> 出力するログの重要度。 </param>
        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public static void LogDirectForEditor(string text, LogKindEnum kind = LogKindEnum.Normal)
        {
#if UNITY_EDITOR
            // Playerでは呼び出し自体を除去し、開発時だけ共通の重要度別出力を利用する。
            LogDirect(text, kind);
#endif
        }

        /// <summary>
        ///     追加されたメッセージをログに出力する。
        /// </summary>
        /// <param name="kind"> 出力するログの重要度。 </param>
        /// <param name="text"> 蓄積済み文字列の末尾へ追加する文字列。 </param>
        /// <param name="clearText"> 出力後に蓄積文字列を消去する場合はtrue。 </param>
        /// <param name="context"> Consoleから追跡可能にするUnityオブジェクト。 </param>
        [HideInCallstack]
        public static void LogText(LogKindEnum kind = LogKindEnum.Normal,
            string text = null,
            bool clearText = true,
            UnityEngine.Object context = null)
        {
            // 蓄積が開始されていない場合は、空のログを出力しない。
            if (_logTextBuilder == null) { return; }

            // 呼び出し時の追加文字列がある場合だけ、蓄積済み内容の末尾へ加える。
            if (!string.IsNullOrEmpty(text)) { _logTextBuilder.AppendLine(text); }

            // 末尾の改行を除いた一つのログとして、指定された重要度で出力する。
            LogDirect(_logTextBuilder.ToString().TrimEnd(), kind, context);

            // 継続して追記する契約の場合だけbuilderを保持する。
            if (clearText) { _logTextBuilder = null; }
        }

        /// <summary>
        ///     Editorでのみ蓄積済みメッセージをログへ出力する。
        /// </summary>
        /// <param name="kind"> 出力するログの重要度。 </param>
        /// <param name="text"> 蓄積済み文字列の末尾へ追加する文字列。 </param>
        /// <param name="clearText"> 出力後に蓄積文字列を消去する場合はtrue。 </param>
        /// <param name="context"> Consoleから追跡可能にするUnityオブジェクト。 </param>
        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public static void LogTextForEditor(LogKindEnum kind = LogKindEnum.Normal,
            string text = null,
            bool clearText = true,
            UnityEngine.Object context = null)
        {
#if UNITY_EDITOR
            // Playerでは呼び出し自体を除去し、Editorだけで蓄積ログを出力する。
            LogText(kind, text, clearText, context);
#endif
        }

        /// <summary>
        ///     ログのテキストにメッセージを追加する。
        /// </summary>
        /// <param name="text"> 蓄積する文字列。 </param>
        public static void AddText(string text)
        {
            // 表示内容を持たない文字列は、builderの生成や空行の追加を行わない。
            if (string.IsNullOrEmpty(text)) { return; }

            // 最初の文字列でbuilderを作り、以後は登録順に改行付きで蓄積する。
            if (_logTextBuilder == null) { _logTextBuilder = new($"{text}\n"); }
            else { _logTextBuilder.AppendLine(text); }
        }

        /// <summary>
        ///     Editorでのみログのテキストへメッセージを追加する。
        /// </summary>
        /// <param name="text"> Editorでのみ蓄積する文字列。 </param>
        [Conditional("UNITY_EDITOR")]
        public static void AddTextForEditor(string text)
        {
#if UNITY_EDITOR
            // Playerでは呼び出し自体を除去し、Editorだけでログ文字列を蓄積する。
            AddText(text);
#endif
        }

        /// <summary>
        ///     追加されたメッセージを削除し新しくする。
        /// </summary>
        /// <param name="text"> 初期値として新たに蓄積する文字列。 </param>
        public static void NewText(string text = null)
        {
            // 以前の蓄積内容を破棄してから、指定された初期文字列だけを登録する。
            _logTextBuilder = null;
            AddText(text);
        }

        /// <summary>
        ///     Editorでのみ蓄積済みメッセージを置き換える。
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void NewTextForEditor(string text = null)
        {
#if UNITY_EDITOR
            // Playerでは呼び出し自体を除去し、Editorだけで蓄積内容を置き換える。
            NewText(text);
#endif
        }

        /// <summary>
        ///     参照がnullかを確認し、nullの場合は警告を出力する。
        /// </summary>
        /// <typeparam name="T"> nullを確認する参照型。 </typeparam>
        /// <param name="object"> nullを確認する対象。 </param>
        /// <returns> nullの場合はtrue。 </returns>
        [HideInCallstack]
        public static bool LogAndCheckComponentNull<T>(this T @object)
        {
            bool isNull = @object == null;

            // Playerでも欠落参照を見逃さない診断APIのため、Unity標準の警告として常に出力する。
            if (isNull) { Debug.LogWarning($"<b>{typeof(T).Name}</b> is null"); }

            return isNull;
        }

        // 互換性維持のために残す旧API。

        /// <summary>
        ///     Editorでのみデバッグログを出力する。
        /// </summary>
        /// <param name="text"> Editorでのみ出力する文字列。 </param>
        /// <param name="kind"> 出力するログの重要度。 </param>
        [Obsolete("この機能は旧型式です。" + nameof(LogDirect) + "を使用してください)")]
        [Conditional("UNITY_EDITOR")]
        public static void DirectLog(string text, LogKindEnum kind = LogKindEnum.Normal)
        {
#if UNITY_EDITOR
            // 旧APIの出力条件を維持しつつ、重要度の処理は現行APIへ集約する。
            LogDirect(text, kind);
#endif
        }

        /// <summary>
        ///     Editorでのみ蓄積済みメッセージをログへ出力する。
        /// </summary>
        [Obsolete("この機能は旧型式です。" + nameof(LogText) + "を使用してください)")]
        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public static void TextLog(LogKindEnum kind = LogKindEnum.Normal, bool clearText = true)
        {
#if UNITY_EDITOR
            // 旧APIの出力条件を維持しつつ、蓄積ログの処理は現行APIへ集約する。
            LogText(kind, clearText: clearText);
#endif
        }

        /// <summary>
        ///     EditorでのみComponentのnullを確認して警告を出力する。
        /// </summary>
        /// <typeparam name="T"> 確認するComponentの型。 </typeparam>
        /// <param name="component"> nullを確認するComponent。 </param>
        [Obsolete("この機能は旧型式です。" + nameof(LogAndCheckComponentNull) + "を使用してください。")]
        [Conditional("UNITY_EDITOR")]
        public static void CheckComponentNull<T>(this T component) where T : Component
        {
#if UNITY_EDITOR
            // 旧APIの既存動作を維持するため、Editor限定のUnity標準警告を直接使用する。
            // TODO(#160): componentがnullの分岐でcomponent.nameを参照している。
            //             UnityEngine.Objectの==nullは破棄済みと真のnullの両方でtrueになり、
            //             後者ではNullReferenceExceptionになる。警告を出す関数が落ちるため、
            //             破棄済みと真のnullを分けて扱う。
            if (component == null) { Debug.LogWarning($"The component {typeof(T).Name} of {component.name} is null."); }
#endif
        }

        /// <summary>
        ///     Componentが有効か確認し、nullの場合は警告を出力する。
        /// </summary>
        [Obsolete("この機能は安全性が保障されていません。" + nameof(LogAndCheckComponentNull) + "を使用してください")]
        public static bool IsComponentNotNull<T>(this T component) where T : Component
        {
            // 旧APIはPlayerでも診断する契約のため、Unity標準の警告を直接使用する。
            if (component == null)
            {
                Debug.LogWarning($"The component of type {typeof(T).Name} is null.");
                return false;
            }

            return true;
        }

        #endregion

        #region 内部処理

        /// <summary> LogDirectで出力されたログを後続処理へ通知するイベント。 </summary>
        internal static event Action<string, LogKindEnum> OnLogDirect;

        /// <summary> 蓄積中のログ文字列。 </summary>
        private static StringBuilder _logTextBuilder = null;

        #endregion
    }
}
