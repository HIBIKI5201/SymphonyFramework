using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace SymphonyFrameWork.Debugger.HUD
{
    /// <summary>
    ///     フレーム統計と任意のデバッグ文字列を画面上へ描画する。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    internal sealed class SymphonyHUDDrawer : MonoBehaviour
    {
        #region 外部向けAPI

        /// <summary>
        ///     HUDへ毎フレーム評価する文字列生成処理を追加する。
        /// </summary>
        /// <param name="func"> 表示文字列を返す処理。 </param>
        public void Add(Func<string> func) => _extraTexts.Add(func);

        /// <summary>
        ///     HUDから文字列生成処理を削除する。
        /// </summary>
        /// <param name="func"> 削除する文字列生成処理。 </param>
        public void Remove(Func<string> func) => _extraTexts.Remove(func);

        /// <summary> 現在登録されている追加テキストの数。 </summary>
        internal int RegisteredTextCount => _extraTexts.Count;

        #endregion

        #region 内部処理

        private readonly List<Func<string>> _extraTexts = new();
        private readonly StringBuilder _textToDisplay = new();

        private float _deltaTime = 0.0f;
        private Rect _rect;
        private GUIStyle _style;

        /// <summary>
        ///     現在の画面サイズに合わせてHUDの表示領域とスタイルを生成する。
        /// </summary>
        private void Awake()
        {
            // 初期化時点の画面サイズを基準に、HUDが左上から画面全体を使える領域を確保する。
            int w = Screen.width, h = Screen.height;
            Rect rect = new(10, 10, w, h);

            // 解像度に比例する文字サイズで、端末ごとの可読性を揃える。
            GUIStyle style = new();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 1 / 50;
            style.normal.textColor = Color.white;

            _rect = rect;
            _style = style;
        }

        /// <summary>
        ///     フレーム統計と追加テキストの表示内容を更新する。
        /// </summary>
        private void Update()
        {
            // Pause中も計測を止めず、急なフレーム時間の揺れを平滑化する。
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

            // 前フレームの内容を破棄し、現在の統計情報から表示文字列を組み立て直す。
            _textToDisplay.Clear();
            GetProfilingText(_textToDisplay);

            // 登録処理は毎フレーム評価し、変化するデバッグ状態を最新値で表示する。
            _textToDisplay.AppendLine(GetExtraText());
        }

        /// <summary>
        ///     構築済みのHUD文字列を画面へ描画する。
        /// </summary>
        private void OnGUI()
        {
            GUI.Label(_rect, _textToDisplay.ToString(), _style);
        }


        /// <summary>
        ///     FPSとメモリ使用量を表示用文字列へ追加する。
        /// </summary>
        /// <param name="text"> 統計情報の追加先。 </param>
        private void GetProfilingText(in StringBuilder text)
        {
            float msec;
            float fps;

            // 初回更新前など有効な経過時間が無い場合は、0除算を避けて未計測値を示す。
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

            // managed領域とUnity全体の割当・予約量を並べ、メモリ増加の由来を比較できるようにする。
            long monoMemory = Profiler.GetMonoUsedSizeLong();
            long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
            long totalReserved = Profiler.GetTotalReservedMemoryLong();

            text.AppendLine($"FPS: {fps.ToString("0.")} ({msec.ToString("0,0")} ms)");
            text.AppendLine($"Mono Memory: {GetMemoryUsageString(monoMemory)}");
            text.AppendLine($"Total Allocated: {GetMemoryUsageString(totalAllocated)}");
            text.AppendLine($"Total Reserved: {GetMemoryUsageString(totalReserved)}");
        }

        /// <summary>
        ///     バイト数を読みやすい単位付き文字列へ変換する。
        /// </summary>
        /// <param name="bytes"> 変換するバイト数。 </param>
        /// <returns> 適切な単位へ変換したメモリ量。 </returns>
        private string GetMemoryUsageString(long bytes)
        {
            // 値の桁に応じた最大単位へ変換し、小さい値だけは整数のバイト数を保つ。
            if (bytes < 1024) { return $"{bytes} B"; }
            if (bytes < 1024 * 1024) { return $"{(bytes / 1024d):0.00} KB"; }
            if (bytes < 1024 * 1024 * 1024) { return $"{(bytes / (1024d * 1024d)):0.00} MB"; }
            return $"{(bytes / (1024d * 1024d * 1024d)):0.00} GB";
        }

        /// <summary>
        ///     登録されたすべての文字列生成処理を評価して結合する。
        /// </summary>
        /// <returns> 改行で結合した追加テキスト。 </returns>
        private string GetExtraText()
        {
            StringBuilder extraTextBuilder = new();

            // 登録順を維持して各文字列を独立した行へ追加する。
            foreach (Func<string> textFunc in _extraTexts) { extraTextBuilder.AppendLine(textFunc()); }

            return extraTextBuilder.ToString();
        }

        #endregion
    }
}
