using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace SymphonyFrameWork.Debugger
{
    public class SymphonyDebugHUD : MonoBehaviour
    {
        public void AddText(string text)
        {
            _extraText += text + "\n";
        }

        private float deltaTime = 0.0f;
        private string _extraText = string.Empty;
        private string _textToDisplay = string.Empty;

        private void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            _textToDisplay = GetProfilingText();
            _textToDisplay += _extraText;

            // _extraTextはUpdateでクリアする（OnGUI中にリセットしない）
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
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;

            long monoMemory = Profiler.GetMonoUsedSizeLong();
            long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
            long totalReserved = Profiler.GetTotalReservedMemoryLong();

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
}
