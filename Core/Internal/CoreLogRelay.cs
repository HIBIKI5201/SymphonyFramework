using System;

using UnityEngine;

namespace SymphonyFrameWork.Core
{
    /// <summary>
    ///     Core層のログを、上位層のロガーへ中継する。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         フレームワーク内のログは <c>SymphonyDebugLogger</c> へ集約し、Editorの購読者が
    ///         <c>Cache/Log.txt</c> へ書き出す。しかし <c>SymphonyDebugLogger</c> はRuntime
    ///         アセンブリにあり、Coreはその下位にあるため直接参照できない。
    ///     </para>
    ///     <para>
    ///         参照方向を崩さずに集約先を揃えるため、Coreは中継口だけを持ち、上位層が注入する。
    ///         Editor設定をRuntimeへ注入する既存の作りと同じ形である。
    ///         **注入前でも動くよう、未設定時はUnity標準の出力へ落とす。**
    ///     </para>
    /// </remarks>
    internal static class CoreLogRelay
    {
        #region 外部向けAPI

        /// <summary>
        ///     例外の出力先。上位層が初期化時に設定する。
        /// </summary>
        /// <remarks>
        ///     nullの間はUnity標準の <c>Debug.LogException</c> へ落ちる。
        /// </remarks>
        internal static Action<Exception> ExceptionHandler;

        /// <summary>
        ///     例外をログへ出力する。
        /// </summary>
        /// <param name="exception"> 出力する例外。 </param>
        internal static void LogException(Exception exception)
        {
            // 上位層の初期化前でもログを失わないよう、未注入ならUnity標準の出力を使う。
            if (ExceptionHandler == null)
            {
                Debug.LogException(exception);
                return;
            }

            ExceptionHandler(exception);
        }

        /// <summary>
        ///     注入した出力先を解除する。
        /// </summary>
        /// <remarks>
        ///     assembly reload後に旧アセンブリのデリゲートを残さないため、上位層の終了処理から呼ぶ。
        /// </remarks>
        internal static void Reset()
        {
            ExceptionHandler = null;
        }

        #endregion
    }
}
