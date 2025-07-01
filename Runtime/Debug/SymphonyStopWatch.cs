using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace SymphonyFrameWork.Debugger
{
    /// <summary>
    ///     UnityEditorで動作するストップウォッチを提供する
    /// </summary>
    public static class SymphonyStopWatch
    {
#if UNITY_EDITOR
        private static readonly Dictionary<string, (Stopwatch watch, string text)> dict = new();
#endif

        /// <summary>
        ///     指定された文字列のストップウォッチを計測開始する
        /// </summary>
        /// <param name="id"></param>
        /// <param name="text"></param>
        [Conditional("UNITY_EDITOR")]
        public static void Start(string id, string text = "time is")
        {
#if UNITY_EDITOR
            if (!dict.TryAdd(id, (Stopwatch.StartNew(), text)))
                Debug.LogWarning($"ストップウォッチのIDが被っています\n{id} ではない別のIDを指定してください");
#endif
        }

        /// <summary>
        ///     IDのタイマーを停止しログに出力
        /// </summary>
        /// <param name="id"></param>
        [Conditional("UNITY_EDITOR")]
        public static void Stop(string id)
        {
#if UNITY_EDITOR
            if (dict.TryGetValue(id, out var value))
            {
                value.watch.Stop();
                Debug.Log($"{value.text} <color=green><b>{value.watch.ElapsedMilliseconds}</b></color> ms");
                dict.Remove(id);
            }
            else
            {
                Debug.LogWarning($"{id}のストップウォッチは開始されていません");
            }
#endif
        }
    }
}