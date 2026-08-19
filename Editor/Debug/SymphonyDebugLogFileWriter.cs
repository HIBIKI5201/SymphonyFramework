using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;

using UnityEngine;

namespace SymphonyFrameWork.Editor.Debugger.Logger
{
    /// <summary>
    ///     SymphonyDebugLogger.OnLogDirectを購読し、ログをファイルへキャッシュ出力する。
    /// </summary>
    internal static class SymphonyDebugLogFileWriter
    {
        #region 外部向けAPI

        /// <summary> ログをファイルへ出力するかどうか。 </summary>
        public static bool IsFileLoggingEnabled = true;

        /// <summary>
        ///     ログ購読と定期書き込みTimerを開始する。
        /// </summary>
        internal static void Initialize()
        {
            // 導入形態にかかわらず、プロジェクトのLibrary配下へログを出力する。
            s_LogFilePath = EditorSymphonyConstant.ResolveDebugLogFileAbsolutePath(Application.dataPath);

            // 再初期化でも購読が重複しないよう、解除してから登録する。
            SymphonyDebugLogger.OnLogDirect -= EnqueueLog;
            SymphonyDebugLogger.OnLogDirect += EnqueueLog;

            // 既存Timerを破棄し、常に一つの周期処理だけがバッファをフラッシュする状態にする。
            s_FlushTimer?.Dispose();
            s_FlushTimer = new Timer(
                _ => Flush(),
                null,
                TimeSpan.FromSeconds(FLUSH_INTERVAL_SECONDS),
                TimeSpan.FromSeconds(FLUSH_INTERVAL_SECONDS));
        }

        /// <summary>
        ///     ログ購読を解除して定期書き込みTimerを破棄する。
        /// </summary>
        internal static void Shutdown()
        {
            // static eventとTimerは呼び出し元より長生きするため、初期化時の購読と対にして解除する。
            SymphonyDebugLogger.OnLogDirect -= EnqueueLog;
            s_FlushTimer?.Dispose();
            s_FlushTimer = null;
        }

        /// <summary>
        ///     蓄積されたログを直ちにファイルへ書き込む。
        /// </summary>
        internal static void Flush()
        {
            // Timerとログ通知による同時書き込みを一つの排他区間へまとめる。
            lock (s_FileLock)
            {
                FlushLocked();
            }
        }

        #endregion

        #region 内部処理

        /// <summary> 一度に貯めるログが何件を超えたら即座に書き込むか。 </summary>
        private const int MAX_BUFFERED_LINES = 50;

        /// <summary> 貯めたログを書き込む間隔（秒）。 </summary>
        private const double FLUSH_INTERVAL_SECONDS = 5.0;

        private static readonly object s_FileLock = new();
        private static readonly List<string> s_PendingLines = new();
        private static Timer s_FlushTimer;
        private static string s_LogFilePath;

        /// <summary>
        ///     ログをファイル出力用のバッファへ蓄積する。
        /// </summary>
        private static void EnqueueLog(string text, LogKindEnum kind)
        {
            // ファイル出力を無効化している間は、メモリ上の待機バッファも増やさない。
            if (!IsFileLoggingEnabled) { return; }

            // Timerとログ通知が同時にバッファへ触れないよう、追加と件数判定を同じlock内で行う。
            lock (s_FileLock)
            {
                s_PendingLines.Add($"[{DateTime.Now:HH:mm:ss}][{kind}] {text}");

                // 上限へ達した場合は周期Timerを待たず、バッファの増加を抑える。
                if (s_PendingLines.Count >= MAX_BUFFERED_LINES) { FlushLocked(); }
            }
        }

        /// <summary>
        ///     蓄積されたログを排他制御済みの状態で書き込む。
        /// </summary>
        /// <remarks> s_FileLockを取得した状態で呼び出す。 </remarks>
        private static void FlushLocked()
        {
            // 未書き込みのログが無い場合は、ディレクトリ確認やファイルI/Oを行わない。
            if (s_PendingLines.Count == 0) { return; }

            try
            {
                // 初回出力時だけCacheディレクトリを作り、蓄積順のまま末尾へ追記する。
                string directory = Path.GetDirectoryName(s_LogFilePath);
                // 初回出力でCacheディレクトリが無い場合だけ作成する。
                if (!Directory.Exists(directory)) { Directory.CreateDirectory(directory); }

                File.AppendAllLines(s_LogFilePath, s_PendingLines);
            }
            catch (Exception e)
            {
                // Editor機能のファイル出力失敗でRuntimeログ処理を中断させず、Consoleへ診断を残す。
                Debug.LogWarning($"[{nameof(SymphonyDebugLogFileWriter)}] ログファイルの書き込みに失敗しました。\n{e}");
            }

            // 書き込み成否にかかわらず既存挙動どおりバッファを解放し、同じ行の再試行を避ける。
            s_PendingLines.Clear();
        }

        #endregion
    }
}
