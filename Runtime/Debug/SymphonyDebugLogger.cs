using System;
using System.Diagnostics;
using System.Text;

using SymphonyFrameWork.Core;

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
            // エラーだけはバージョンを添える。報告を受ける側が最初に必要とする情報である。
            string output = kind == LogKindEnum.Error ? $"{VersionTag} {text}" : text;

            // 呼び出し側が指定した重要度をUnity Consoleの対応するログ種別へそのまま反映する。
            switch (kind)
            {
                case LogKindEnum.Normal: Debug.Log(output, context); break;
                case LogKindEnum.Warning: Debug.LogWarning(output, context); break;
                case LogKindEnum.Error: Debug.LogError(output, context); break;
            }

            // Runtime層ではファイル出力を担わず、Editor側の購読者へ同じ内容を通知する。
            OnLogDirect?.Invoke(output, kind);
        }

        /// <summary>
        ///     例外をログへ出力する。
        /// </summary>
        /// <remarks>
        ///     Unity Consoleへはスタックトレース付きの例外として出力し、購読者へは
        ///     型と理由を含む1行のテキストとして通知する。Consoleの表示を犠牲にせず、
        ///     ファイル出力にも例外の内容を残すための分担である。
        /// </remarks>
        /// <param name="exception"> 出力する例外。 </param>
        /// <param name="context"> Consoleから追跡可能にするUnityオブジェクト。 </param>
        [HideInCallstack]
        public static void LogException(Exception exception, UnityEngine.Object context = null)
        {
            // 呼び出し側の判定漏れでログ機構自体が落ちないよう、nullを診断として扱う。
            if (exception == null)
            {
                LogDirect("nullの例外がLogExceptionへ渡されました。", LogKindEnum.Error, context);
                return;
            }

            // Consoleでは例外として扱わせ、スタックトレースの表示とジャンプを維持する。
            // ここへバージョンを足すには例外を包む必要があり、Consoleの先頭行が
            // 本来の例外型でなくなる。表示を壊さないため、Consoleは素の例外のままにする。
            Debug.LogException(exception, context);

            // Runtime層ではファイル出力を担わず、Editor側の購読者へ同じ内容を通知する。
            OnLogDirect?.Invoke($"{VersionTag} {DescribeException(exception)}", LogKindEnum.Error);
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
            bool isNull = IsNullReference(@object);

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
            // 破棄済みと真のnullではnameを読めないため、参照せずに種別だけを伝える。
            if (component == null)
            {
                Debug.LogWarning($"The component {typeof(T).Name} is null. ({DescribeNullKind(component)})");
            }
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

        /// <summary>
        ///     例外を1行のテキストへ要約する。
        /// </summary>
        /// <remarks>
        ///     ファイル出力は1件を1行として書き出す。<c>ArgumentNullException</c> のように
        ///     Message自体が複数行になる例外があるため、改行を空白へ畳んでから返す。
        ///     スタックトレースは含めない。Consoleへは例外として別途出力している。
        /// </remarks>
        /// <param name="exception"> 要約する例外。 </param>
        /// <returns> 型名と理由を含む1行のテキスト。 </returns>
        internal static string DescribeException(Exception exception)
        {
            string reason = exception.Message
                .Replace("\r\n", " ")
                .Replace('\r', ' ')
                .Replace('\n', ' ');

            return $"{exception.GetType().FullName}: {reason}";
        }

        /// <summary>
        ///     参照が使用できない状態かを判定する。
        /// </summary>
        /// <remarks>
        ///     型引数に制約が無いため、<c>==</c> は <see cref="UnityEngine.Object" /> の
        ///     比較演算子ではなく参照比較になる。破棄済みのUnityオブジェクトを見逃さないよう、
        ///     Unityオブジェクトのときだけ比較演算子へ委ねる。
        /// </remarks>
        /// <typeparam name="T"> 判定する参照の型。 </typeparam>
        /// <param name="object"> 判定する対象。 </param>
        /// <returns> 真のnull、または破棄済みのUnityオブジェクトの場合はtrue。 </returns>
        private static bool IsNullReference<T>(T @object)
        {
            if (@object is UnityEngine.Object unityObject) { return unityObject == null; }

            return @object is null;
        }

        /// <summary>
        ///     nullと判定された参照が、未代入と破棄済みのどちらであるかを表す語を返す。
        /// </summary>
        /// <remarks>
        ///     破棄済みでも真のnullでも <c>name</c> の取得は失敗する。参照せずに種別だけを伝え、
        ///     警告を出すためのAPIが例外で落ちないようにする。
        /// </remarks>
        /// <param name="object"> nullと判定済みの対象。 </param>
        /// <returns> 未代入なら <c>unassigned</c>、破棄済みなら <c>destroyed</c>。 </returns>
        private static string DescribeNullKind(object @object)
            => @object is null ? "unassigned" : "destroyed";

        /// <summary>
        ///     エラーログの先頭へ付けるFrameworkのバージョン表記。
        /// </summary>
        /// <remarks>
        ///     不具合の報告を受ける側が最初に必要とするのがバージョンである。
        ///     利用者がpackage.jsonを開かなくても、ログ1行から分かる状態にする。
        /// </remarks>
        internal static string VersionTag { get; } =
            $"[{SymphonyConstant.SYMPHONY_FRAMEWORK} v{SymphonyConstant.VERSION}]";

        /// <summary> LogDirectで出力されたログを後続処理へ通知するイベント。 </summary>
        internal static event Action<string, LogKindEnum> OnLogDirect;

        /// <summary> 蓄積中のログ文字列。 </summary>
        private static StringBuilder _logTextBuilder = null;

        #endregion
    }
}
