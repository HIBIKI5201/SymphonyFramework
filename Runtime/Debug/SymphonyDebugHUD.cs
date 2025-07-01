using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace SymphonyFrameWork.Debugger
{
    /// <summary>
    ///     ゲーム画面上にデバッグ情報を表示するクラス。
    /// </summary>
    public class SymphonyDebugHUD : MonoBehaviour
    {
        /// <summary>
        ///     デバッグHUDに追加のテキストを表示します。
        ///     LateUpdateで呼び出すと、前のフレームのテキストがクリアされるので注意してください。
        /// </summary>
        /// <param name="text"></param>
        public void AddText(string text)
        {
            _extraText += text + "\n";
        }

        private float deltaTime = 0.0f;

        private string _extraText = string.Empty;

        private void Update()
        {
            // フレーム時間の加算
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        private void LateUpdate()
        {
            _extraText = string.Empty;
        }

        private void OnGUI()
        {
            int w = Screen.width, h = Screen.height;

            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(10, 10, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 1 / 50;
            style.normal.textColor = Color.white;

            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;

            long totalMemory = GC.GetTotalMemory(false); // 管理メモリ（GCの影響受ける）
            long monoMemory = Profiler.GetMonoUsedSizeLong(); // Mono管理メモリ
            long totalAllocated = Profiler.GetTotalAllocatedMemoryLong(); // 全体の割り当て
            long totalReserved = Profiler.GetTotalReservedMemoryLong(); // 予約済み

            string text = string.Format(
                "FPS: {0:0.} ({1:0.0} ms)\n" +
                "Mono Memory: {2} MB\n" +
                "Total Allocated: {3} MB\n" +
                "Total Reserved: {4} MB\n",
                fps, msec,
                (monoMemory / (1024 * 1024)),
                (totalAllocated / (1024 * 1024)),
                (totalReserved / (1024 * 1024))
                );

            GUI.Label(rect, text, style);
        }
    }
}
