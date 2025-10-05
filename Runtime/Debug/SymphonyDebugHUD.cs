using SymphonyFrameWork.System;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace SymphonyFrameWork.Debugger
{
    /// <summary>
    ///     画面上にデバッグ用のHUDを表示するクラス
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class SymphonyDebugHUD : MonoBehaviour
    {
        /// <summary>
        ///     SymphonyDebugHUDに追加のテキストを追加する
        /// </summary>
        /// <param name="text"></param>
        public static void AddText(string text)
        {
            _debugHUD.Value._extraText.AppendLine(text);
        }

        internal static void Initialize()
        {
            _debugHUD = new Lazy<SymphonyDebugHUD>(CreateDebugHUD);
        }

        private static Lazy<SymphonyDebugHUD> _debugHUD;

        private static SymphonyDebugHUD CreateDebugHUD()
        {
            return SymphonyCoreSystem.CreateSystemObject<SymphonyDebugHUD>();
        }

        private float _deltaTime = 0.0f;
        private StringBuilder _extraText = null;
        private StringBuilder _textToDisplay = null;

        private Rect _rect;
        private GUIStyle _style;

        private void Awake()
        {
            int w = Screen.width, h = Screen.height;

            Rect rect = new Rect(10, 10, w, h);

            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 1 / 50;
            style.normal.textColor = Color.white;

            _rect = rect;
            _style = style;
        }

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f; // デルタタイムの計算（タイムスケールに影響しない）

            //基本テキストを取得。
            _textToDisplay = new(GetProfilingText());

            // 追加テキストを追加。
            _textToDisplay.AppendLine(_extraText.ToString());

            _extraText = new();
        }

        private string GetProfilingText()
        {
            float msec = _deltaTime * 1000.0f; // ミリ秒に変換。
            float fps = 1.0f / _deltaTime; // FPSの計算。

            long monoMemory = Profiler.GetMonoUsedSizeLong(); // Monoの使用メモリ量を取得。
            long totalAllocated = Profiler.GetTotalAllocatedMemoryLong(); // 総アロケートメモリ量を取得。
            long totalReserved = Profiler.GetTotalReservedMemoryLong(); // 総リザーブメモリ量を取得。

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

            return text;
        }

        private void OnGUI()
        {
            GUI.Label(_rect, _textToDisplay.ToString(), _style);
        }
    }
}
