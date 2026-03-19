using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace SymphonyFrameWork.Debugger.HUD
{
    public class SymphonyHUDDrawer : MonoBehaviour
    {
        public void Add(Func<string> func) => _extraTexts.Add(func);
        public void Remove(Func<string> func) => _extraTexts.Remove(func);

        private readonly List<Func<string>> _extraTexts = new();
        private readonly StringBuilder _textToDisplay = new();

        private float _deltaTime = 0.0f;
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
            _textToDisplay.Clear();
            GetProfilingText(_textToDisplay);

            // 追加テキストを追加。
            _textToDisplay.AppendLine(GetExtraText());
        }

        private void OnGUI()
        {
            GUI.Label(_rect, _textToDisplay.ToString(), _style);
        }


        private void GetProfilingText(in StringBuilder text)
        {
            float msec;
            float fps;

            // デルタタイムが0以下の場合は、無効な値としてNaNを設定。
            if (_deltaTime <= 0f)
            {
                msec = float.NaN;
                fps = float.NaN;
            }
            else
            {
                msec = _deltaTime * 1000.0f;
                fps = 1.0f / _deltaTime;
            }

            long monoMemory = Profiler.GetMonoUsedSizeLong(); // Monoの使用メモリ量を取得。
            long totalAllocated = Profiler.GetTotalAllocatedMemoryLong(); // 総アロケートメモリ量を取得。
            long totalReserved = Profiler.GetTotalReservedMemoryLong(); // 総リザーブメモリ量を取得。

            text.AppendLine($"FPS: {fps.ToString("0.")} ({msec.ToString("0,0")} ms)");
            text.AppendLine($"Mono Memory: {GetMemoryUsageString(monoMemory)}");
            text.AppendLine($"Total Allocated: {GetMemoryUsageString(totalAllocated)}");
            text.AppendLine($"Total Reserved: {GetMemoryUsageString(totalReserved)}");
        }

        private string GetMemoryUsageString(long bytes)
        {
            if (bytes < 1024) { return $"{bytes} B"; }
            if (bytes < 1024 * 1024) { return $"{(bytes / 1024d):0.00} KB"; }
            if (bytes < 1024 * 1024 * 1024) { return $"{(bytes / (1024d * 1024d)):0.00} MB"; }
            return $"{(bytes / (1024d * 1024d * 1024d)):0.00} GB";
        }

        private string GetExtraText()
        {
            StringBuilder extraTextBuilder = new();
            foreach (var textFunc in _extraTexts)
            {
                extraTextBuilder.AppendLine(textFunc());
            }
            return extraTextBuilder.ToString();
        }
    }
}
