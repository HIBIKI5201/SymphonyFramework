using SymphonyFrameWork.System;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SymphonyFrameWork.Samples.PauseManagerSample
{
    /// <summary> カテゴリーごとのポーズ切り替え、通知、Pausable待機処理を実演する。 </summary>
    public sealed class PauseManagerSample_Controller : MonoBehaviour
    {
        private readonly Queue<string> _commentaryLogs = new();
        private Vector2 _scrollPosition;
        private int _unpausedFrameCount;
        private float _countdownRemaining;
        private bool _isCountdownRunning;

        /// <summary> カテゴリーごとのポーズ変更通知を購読する。 </summary>
        /// <remarks>
        ///     **eventではなくメソッドで購読する。** C#のeventは型パラメータを持てないため、
        ///     カテゴリー単位の通知はAddPauseChangedHandlerで受け取る。
        /// </remarks>
        private void OnEnable()
        {
            PauseManager.AddPauseChangedHandler<IPauseManagerSample_GameplayPausable>(
                OnGameplayPauseChanged);
            PauseManager.AddPauseChangedHandler<IPauseManagerSample_UiPausable>(
                OnUiPauseChanged);
        }

        /// <summary> 購読を解除する。 </summary>
        private void OnDisable()
        {
            PauseManager.RemovePauseChangedHandler<IPauseManagerSample_GameplayPausable>(
                OnGameplayPauseChanged);
            PauseManager.RemovePauseChangedHandler<IPauseManagerSample_UiPausable>(
                OnUiPauseChanged);
        }

        /// <summary> サンプルを初期状態に戻し、UI側のCubeを生成してループを開始する。 </summary>
        private void Start()
        {
            PauseManager.SetPauseAll(false);
            SpawnUiMover();
            AddCommentary("サンプルを開始しました。2つのCubeは別々のカテゴリーに属します。");
            CountFrames();
        }

        /// <summary> ゲームプレイカテゴリーのポーズをトグルする。 </summary>
        [ContextMenu("Toggle Gameplay Pause")]
        public void ToggleGameplayPause()
        {
            PauseManager.SetPause<IPauseManagerSample_GameplayPausable>(
                !PauseManager.IsPaused<IPauseManagerSample_GameplayPausable>());
        }

        /// <summary> UI演出カテゴリーのポーズをトグルする。 </summary>
        [ContextMenu("Toggle UI Pause")]
        public void ToggleUiPause()
        {
            PauseManager.SetPause<IPauseManagerSample_UiPausable>(
                !PauseManager.IsPaused<IPauseManagerSample_UiPausable>());
        }

        /// <summary> 全カテゴリーのポーズをトグルする。 </summary>
        [ContextMenu("Toggle All Pause")]
        public void ToggleAllPause()
        {
            PauseManager.SetPauseAll(!PauseManager.IsPausedAny());
        }

        /// <summary> ゲームプレイのポーズを除外した5秒カウントダウンを開始する。 </summary>
        [ContextMenu("Start 5s Countdown")]
        public async void StartCountdown()
        {
            if (_isCountdownRunning)
            {
                return;
            }

            _isCountdownRunning = true;
            _countdownRemaining = 5f;
            AddCommentary("5秒カウントダウンを開始します。ゲームプレイのポーズ中だけ進みません。");

            try
            {
                while (_countdownRemaining > 0f)
                {
                    float step = Mathf.Min(0.1f, _countdownRemaining);

                    // 待機もカテゴリーを指定する。UIだけ止めてもカウントダウンは進む。
                    await PauseManager.PausableWaitForSecondAsync<IPauseManagerSample_GameplayPausable>(
                        step, destroyCancellationToken);
                    _countdownRemaining -= step;
                }

                AddCommentary("カウントダウンが完了しました。");
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _isCountdownRunning = false;
            }
        }

        /// <summary>
        ///     UI演出カテゴリーのCubeを実行時に生成する。
        /// </summary>
        /// <remarks>
        ///     **非アクティブで生成してから色を設定する。** アクティブなGameObjectへ
        ///     AddComponentすると、その場でAwakeが走って見た目が先に作られてしまう。
        /// </remarks>
        private static void SpawnUiMover()
        {
            GameObject uiMoverObject = new(nameof(PauseManagerSample_UiMover));
            uiMoverObject.SetActive(false);
            uiMoverObject.transform.position = new Vector3(0f, 2f, 0f);

            PauseManagerSample_UiMover uiMover =
                uiMoverObject.AddComponent<PauseManagerSample_UiMover>();
            uiMover.SetColor(Color.cyan);

            uiMoverObject.SetActive(true);
        }

        /// <summary> ゲームプレイのポーズ変更を実況ログへ記録する。 </summary>
        private void OnGameplayPauseChanged(bool paused)
        {
            AddCommentary(paused
                ? "Gameplayをポーズしました。白いCubeとカウントダウンが止まります。"
                : "Gameplayを再開しました。");
        }

        /// <summary> UI演出のポーズ変更を実況ログへ記録する。 </summary>
        private void OnUiPauseChanged(bool paused)
        {
            AddCommentary(paused
                ? "UIをポーズしました。水色のCubeだけが止まります。"
                : "UIを再開しました。");
        }

        /// <summary> カテゴリーごとの状態、操作ボタン、実況ログをIMGUIで描画する。 </summary>
        private void OnGUI()
        {
            float margin = Mathf.Max(12f, Screen.width * 0.025f);
            float width = Screen.width - (margin * 2f);
            float height = Screen.height - (margin * 2f);
            Rect outerRect = new(margin, margin, width, height);
            Rect innerRect = new(
                outerRect.x + 12f,
                outerRect.y + 28f,
                outerRect.width - 24f,
                outerRect.height - 40f);

            GUI.Box(outerRect, "Pause Manager Sample");

            GUILayout.BeginArea(innerRect);
            GUILayout.Label("実況解説");
            GUILayout.Label("1. 白いCubeはGameplay、水色のCubeはUIのカテゴリーに属します。");
            GUILayout.Label("2. 片方のカテゴリーを止めても、もう片方は動き続けます。");
            GUILayout.Label("3. Unpaused FramesとカウントダウンはGameplayのポーズにだけ追従します。");
            GUILayout.Space(8f);

            bool isGameplayPaused = PauseManager.IsPaused<IPauseManagerSample_GameplayPausable>();
            bool isUiPaused = PauseManager.IsPaused<IPauseManagerSample_UiPausable>();

            GUILayout.Label($"Gameplay         : {(isGameplayPaused ? "Paused" : "Running")}");
            GUILayout.Label($"UI               : {(isUiPaused ? "Paused" : "Running")}");
            GUILayout.Label($"Any Paused       : {PauseManager.IsPausedAny()}");
            GUILayout.Label($"Unpaused Frames  : {_unpausedFrameCount}");
            GUILayout.Label($"Countdown Remain : {_countdownRemaining:F1}s ({(_isCountdownRunning ? "running" : "idle")})");
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(isGameplayPaused ? "Resume Gameplay" : "Pause Gameplay"))
            {
                ToggleGameplayPause();
            }

            if (GUILayout.Button(isUiPaused ? "Resume UI" : "Pause UI")) { ToggleUiPause(); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(PauseManager.IsPausedAny() ? "Resume All" : "Pause All"))
            {
                ToggleAllPause();
            }

            if (GUILayout.Button("Start 5s Countdown")) { StartCountdown(); }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("Commentary Log");
            float logHeight = Mathf.Max(160f, innerRect.height * 0.36f);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(logHeight));
            GUILayout.TextArea(BuildCommentaryText(), GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary> ゲームプレイのポーズ中は増加しないフレームカウンタを回す。 </summary>
        private async void CountFrames()
        {
            try
            {
                while (true)
                {
                    await PauseManager.PausableNextFrameAsync<IPauseManagerSample_GameplayPausable>(
                        destroyCancellationToken);
                    _unpausedFrameCount++;
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary> 表示上限を維持しながら実況ログを末尾へ追加する。 </summary>
        private void AddCommentary(string message)
        {
            if (_commentaryLogs.Count >= 12)
            {
                _commentaryLogs.Dequeue();
            }

            _commentaryLogs.Enqueue(message);
        }

        /// <summary> 現在の実況ログを改行区切りの表示文字列へまとめる。 </summary>
        private string BuildCommentaryText()
        {
            StringBuilder builder = new();
            foreach (string log in _commentaryLogs)
            {
                builder.AppendLine(log);
            }

            return builder.ToString();
        }
    }
}
