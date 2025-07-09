using SymphonyFrameWork.System;
using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace SymphonyFrameWork.Debugger
{
    /// <summary>
    ///     画面上にデバッグ用のHUDを表示するクラス
    /// </summary>
    public class SymphonyDebugHUD : MonoBehaviour
    {
        /// <summary>
        ///     SymphonyDebugHUDに追加のテキストを追加する
        /// </summary>
        /// <param name="text"></param>
        public static void AddText(string text)
        {
            _debugHUD.Value.AddText(text);
        }

        private class SymphonyDebugHUDRenderer : MonoBehaviour
        {
            public void AddText(string text) => _extraText += text + "\n";

            private float deltaTime = 0.0f;
            private string _extraText = string.Empty;
            private string _textToDisplay = string.Empty;

            private void Update()
            {
                deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f; // デルタタイムの計算

                _textToDisplay = GetProfilingText();

                _textToDisplay += _extraText;
                _extraText = string.Empty;
            }

            private void OnGUI()
            {
                int w = Screen.width, h = Screen.height;

                Rect rect = new Rect(10, 10, w, h);

                GUIStyle style = new GUIStyle();
                style.alignment = TextAnchor.UpperLeft;
                style.fontSize = h * 1 / 50;
                style.normal.textColor = Color.white;

                GUI.Label(rect, _textToDisplay, style);
            }

            private string GetProfilingText()
            {
                // OnGUIで使用する文字列をここで確定しておく
                float msec = deltaTime * 1000.0f; // ミリ秒に変換
                float fps = 1.0f / deltaTime; // FPSの計算

                long monoMemory = Profiler.GetMonoUsedSizeLong(); // Monoの使用メモリ量を取得
                long totalAllocated = Profiler.GetTotalAllocatedMemoryLong(); // 総アロケートメモリ量を取得
                long totalReserved = Profiler.GetTotalReservedMemoryLong(); // 総リザーブメモリ量を取得

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
        }

        internal static void Initialize()
        {
            _debugHUD = new Lazy<SymphonyDebugHUDRenderer>(CreateDebugHUD);
        }

        private static Lazy<SymphonyDebugHUDRenderer> _debugHUD;

        private static SymphonyDebugHUDRenderer CreateDebugHUD()
        {
            GameObject instance = new GameObject(nameof(SymphonyDebugHUD));
            DontDestroyOnLoad(instance);
            SymphonyDebugHUDRenderer debugHUD = instance.AddComponent<SymphonyDebugHUDRenderer>();
            SymphonyCoreSystem.MoveObjectToSymphonySystem(instance);
            return debugHUD;
        }
    }
}
