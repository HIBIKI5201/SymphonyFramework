using SymphonyFrameWork.Core;
using SymphonyFrameWork.System;
using System;
using System.Text;
using UnityEditor;
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
        ///     HUDを表示する。
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(SymphonyDebugHUD) + "/" + nameof(Show))]
        public static void Show()
        {
            _ = _debugHUD.Value; // アクセスしてインスタンスを作成。
        }

        /// <summary>
        ///     HUDを非表示にする。
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(SymphonyDebugHUD) + "/" + nameof(Hide))]
        public static void Hide()
        {
            if (_debugHUD.IsValueCreated) // 既にインスタンスが作成されている場合のみ。
            {
                // インスタンスを破棄して非表示にする。
                Destroy(_debugHUD.Value.gameObject);
                _debugHUD = new Lazy<SymphonyDebugHUD>(CreateDebugHUD);
            }
        }

        /// <summary>
        ///     SymphonyDebugHUDに追加のテキストを追加する。
        /// </summary>
        /// <param name="text"></param>
        public static void AddText(string text, Color color = default)
        {
            if (!_debugHUD.IsValueCreated) return; // インスタンスが作成されていない場合は何もしない。

            if (color != default) //カラーを指定する
            {
                string colorHex = ColorUtility.ToHtmlStringRGB(color);
                text = $"<color=#{colorHex}>{text}</color>";
            }

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
        private StringBuilder _extraText = new();
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
            _textToDisplay = new();
            GetProfilingText(ref _textToDisplay);

            // 追加テキストを追加。
            _textToDisplay.AppendLine(_extraText.ToString());

            _extraText = new();
        }

        private void GetProfilingText(ref StringBuilder text)
        {
            float msec = _deltaTime * 1000.0f; // ミリ秒に変換。
            float fps = 1.0f / _deltaTime; // FPSの計算。

            long monoMemory = Profiler.GetMonoUsedSizeLong(); // Monoの使用メモリ量を取得。
            long totalAllocated = Profiler.GetTotalAllocatedMemoryLong(); // 総アロケートメモリ量を取得。
            long totalReserved = Profiler.GetTotalReservedMemoryLong(); // 総リザーブメモリ量を取得。

            text.AppendLine($"FPS: {fps:0.} ({msec:0.0} ms)");
            text.AppendLine($"Mono Memory: {monoMemory / (1024 * 1024)} MB");
            text.AppendLine($"Total Allocated: {totalAllocated / (1024 * 1024)} MB");
            text.AppendLine($"Total Reserved: {totalReserved / (1024 * 1024)} MB");
        }

        private void OnGUI()
        {
            GUI.Label(_rect, _textToDisplay.ToString(), _style);
        }
    }
}
