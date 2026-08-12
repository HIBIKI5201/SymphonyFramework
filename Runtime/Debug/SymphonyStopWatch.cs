using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace SymphonyFrameWork.Debugger
{
    /// <summary>
    ///     UnityEditorでID単位の処理時間を計測する。
    /// </summary>
    public static class SymphonyStopWatch
    {
        #region 外部向けAPI

        /// <summary>
        ///     指定したIDの計測を開始する。
        /// </summary>
        /// <param name="id"> 計測を識別する一意なID。 </param>
        /// <param name="text"> 計測結果の前に表示する文字列。 </param>
        [Conditional("UNITY_EDITOR")]
        public static void Start(string id, string text = "time is")
        {
#if UNITY_EDITOR
            // 同じIDの計測結果が上書きされないよう、重複時はEditorで直接警告する。
            if (!dict.TryAdd(id, (Stopwatch.StartNew(), text)))
            {
                Debug.LogWarning($"ストップウォッチのIDが被っています\n{id} ではない別のIDを指定してください");
            }
#endif
        }

        /// <summary>
        ///     指定したIDの計測を停止して結果を出力する。
        /// </summary>
        /// <param name="id"> 停止する計測のID。 </param>
        [Conditional("UNITY_EDITOR")]
        public static void Stop(string id)
        {
#if UNITY_EDITOR
            // 開始済みのIDだけ計測結果を出力して登録を解放し、未登録ならEditorで誤用を警告する。
            if (dict.TryGetValue(id, out (Stopwatch watch, string text) value))
            {
                value.watch.Stop();
                Debug.Log($"{value.text} <color=green><b>{value.watch.ElapsedMilliseconds}</b></color> ms");
                dict.Remove(id);
            }
            else { Debug.LogWarning($"{id}のストップウォッチは開始されていません"); }
#endif
        }

        #endregion

        #region 内部処理

#if UNITY_EDITOR
        private static readonly Dictionary<string, (Stopwatch watch, string text)> dict = new();
#endif

        #endregion
    }
}
