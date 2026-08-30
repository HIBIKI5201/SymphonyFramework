using SymphonyFrameWork.System.SceneBlock;
using SymphonyFrameWork.System.SceneLoad;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SymphonyFrameWork.Samples.SceneBlockSample
{
    /// <summary> ブロック単位のロードと単体シーンのロードが共存することを実演する。 </summary>
    public sealed class SceneBlockSample_Controller : MonoBehaviour
    {
        private const string SHARED_SCENE = "SceneBlockSample_Shared";

        [SerializeField, Tooltip("Base / Town / Shared を持つScene Block。")]
        private SceneBlockAsset _townBlock;

        [SerializeField, Tooltip("Base / Dungeon / Shared を持つScene Block。")]
        private SceneBlockAsset _dungeonBlock;

        private readonly Queue<string> _commentaryLogs = new();
        private Vector2 _scrollPosition;
        private bool _isBusy;
        private float _blockProgress;

        /// <summary> サンプルの前提条件を実況ログへ表示する。 </summary>
        private void Start()
        {
            AddCommentary("サンプルを開始しました。ブロックのシーンはまだロードされていません。");
            AddCommentary("事前にSceneBlockSample_Base / _Town / _Dungeon / _Shared をBuild SettingsのScene Listへ追加してください。");
            AddCommentary("TownBlockとDungeonBlockは Base と Shared を共有し、Base だけが Is Persistent です。");
        }

        /// <summary> Town Blockをロードする。 </summary>
        [ContextMenu("Load Town Block")]
        public async void LoadTownBlock()
        {
            await LoadBlockAsync(_townBlock);
        }

        /// <summary> Dungeon Blockをロードする。 </summary>
        [ContextMenu("Load Dungeon Block")]
        public async void LoadDungeonBlock()
        {
            await LoadBlockAsync(_dungeonBlock);
        }

        /// <summary> Town Blockをアンロードする。 </summary>
        [ContextMenu("Unload Town Block")]
        public async void UnloadTownBlock()
        {
            await UnloadBlockAsync(_townBlock);
        }

        /// <summary> Dungeon Blockをアンロードする。 </summary>
        [ContextMenu("Unload Dungeon Block")]
        public async void UnloadDungeonBlock()
        {
            await UnloadBlockAsync(_dungeonBlock);
        }

        /// <summary> 次のブロックを先にロードしてから、前のブロックをアンロードする。 </summary>
        [ContextMenu("Switch To Dungeon")]
        public async void SwitchToDungeon()
        {
            if (_isBusy) { return; }

            _isBusy = true;
            AddCommentary("Dungeonを先にロードしてからTownをアンロードします。共有しているSharedは保持され続けます。");

            try
            {
                await SceneBlockLoader.LoadAsync(_dungeonBlock, token: destroyCancellationToken);
                await SceneBlockLoader.UnloadAsync(_townBlock, token: destroyCancellationToken);
                AddCommentary("Dungeonへの切り替えが完了しました。Sharedがロードされたままであることを確認してください。");
            }
            catch (OperationCanceledException)
            {
                AddCommentary("切り替えを中断しました。");
            }
            catch (SceneBlockPlanException exception)
            {
                ReportPlanException(exception);
            }

            _isBusy = false;
        }

        /// <summary> Shared SceneをSceneLoaderで直接ロードする。 </summary>
        [ContextMenu("Load Shared Scene Directly")]
        public async void LoadSharedSceneDirectly()
        {
            if (_isBusy) { return; }

            _isBusy = true;
            AddCommentary($"{SHARED_SCENE} をSceneLoaderで直接ロードします。以後このシーンはブロックのアンロードで落ちません。");

            bool succeeded = await SceneLoader.LoadSceneAsync(
                SHARED_SCENE,
                token: destroyCancellationToken);

            AddCommentary(succeeded
                ? $"{SHARED_SCENE} の直接ロードに成功しました。"
                : $"{SHARED_SCENE} の直接ロードに失敗しました。Build Settingsの登録を確認してください。");
            _isBusy = false;
        }

        /// <summary> Shared SceneをSceneLoaderで直接アンロードする。 </summary>
        [ContextMenu("Unload Shared Scene Directly")]
        public async void UnloadSharedSceneDirectly()
        {
            if (_isBusy) { return; }

            _isBusy = true;
            AddCommentary($"{SHARED_SCENE} をSceneLoaderで直接アンロードします。ブロック外でロードしたシーンは利用側が落とします。");

            await SceneLoader.UnloadSceneAsync(SHARED_SCENE, token: destroyCancellationToken);

            AddCommentary($"{SHARED_SCENE} の直接アンロードが完了しました。");
            _isBusy = false;
        }

        /// <summary> ブロックの状態、操作ボタン、実況ログをIMGUIで描画する。 </summary>
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

            GUI.Box(outerRect, "Scene Block Sample");

            GUILayout.BeginArea(innerRect);
            GUILayout.Label("実況解説");
            GUILayout.Label("1. ブロックは依存順にロードされます。Baseが先、TownとSharedが後です。");
            GUILayout.Label("2. Switch To Dungeonは次をロードしてから前をアンロードするため、共有のSharedが保持され続けます。");
            GUILayout.Label("3. Baseは Is Persistent のため、両方のブロックをアンロードしても残ります。");
            GUILayout.Label("4. Load Shared Scene Directlyの後は、ブロックをアンロードしてもSharedが残ります。");
            GUILayout.Space(8f);

            GUILayout.Label($"Busy           : {_isBusy}");
            GUILayout.Label($"Block Progress : {_blockProgress:P0}");
            GUILayout.Space(8f);

            DrawBlockStatus();
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Town")) { LoadTownBlock(); }
            if (GUILayout.Button("Unload Town")) { UnloadTownBlock(); }
            if (GUILayout.Button("Load Dungeon")) { LoadDungeonBlock(); }
            if (GUILayout.Button("Unload Dungeon")) { UnloadDungeonBlock(); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Switch To Dungeon")) { SwitchToDungeon(); }
            if (GUILayout.Button("Load Shared Directly")) { LoadSharedSceneDirectly(); }
            if (GUILayout.Button("Unload Shared Directly")) { UnloadSharedSceneDirectly(); }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("Commentary Log");
            float logHeight = Mathf.Max(160f, innerRect.height * 0.28f);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(logHeight));
            GUILayout.TextArea(BuildCommentaryText(), GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary> 追跡中の全ブロックの公開状態スナップショットをラベル表示する。 </summary>
        private void DrawBlockStatus()
        {
            IReadOnlyList<SceneBlockInfo> blockInfos = SceneBlockLoader.GetBlockInfos();
            if (blockInfos.Count == 0)
            {
                GUILayout.Label("追跡中のScene Blockはありません。");
                return;
            }

            foreach (SceneBlockInfo blockInfo in blockInfos)
            {
                GUILayout.Label(
                    $"{blockInfo.BlockName} | State: {blockInfo.State} | "
                    + $"Progress: {blockInfo.Progress:P0} | Layers: {blockInfo.LayerCount} | "
                    + $"Held: {string.Join(", ", blockInfo.HeldSceneNames)}");
            }
        }

        /// <summary> ブロックをロードし、結果を実況ログへ残す。 </summary>
        /// <param name="block"> ロードするScene Block。 </param>
        /// <returns> ロード処理を表すAwaitable。 </returns>
        private async Awaitable LoadBlockAsync(SceneBlockAsset block)
        {
            if (_isBusy || block == null) { return; }

            _isBusy = true;
            _blockProgress = 0f;
            AddCommentary($"{block.BlockName} を依存順にロードします。");

            try
            {
                IProgress<float> progress = new Progress<float>(value => _blockProgress = value);
                bool succeeded = await SceneBlockLoader.LoadAsync(
                    block,
                    progress,
                    destroyCancellationToken);

                AddCommentary(succeeded
                    ? $"{block.BlockName} のロードに成功しました。"
                    : $"{block.BlockName} のロードに失敗しました。Build Settingsの登録を確認してください。");
            }
            catch (OperationCanceledException)
            {
                AddCommentary($"{block.BlockName} のロードを中断しました。ロード済みのシーンは残ります。");
            }
            catch (SceneBlockPlanException exception)
            {
                ReportPlanException(exception);
            }

            _isBusy = false;
        }

        /// <summary> ブロックをアンロードし、結果を実況ログへ残す。 </summary>
        /// <param name="block"> アンロードするScene Block。 </param>
        /// <returns> アンロード処理を表すAwaitable。 </returns>
        private async Awaitable UnloadBlockAsync(SceneBlockAsset block)
        {
            if (_isBusy || block == null) { return; }

            _isBusy = true;
            AddCommentary($"{block.BlockName} の保持を解きます。他が保持しているシーンと永続シーンは残ります。");

            try
            {
                await SceneBlockLoader.UnloadAsync(block, token: destroyCancellationToken);
                AddCommentary($"{block.BlockName} のアンロードが完了しました。");
            }
            catch (OperationCanceledException)
            {
                AddCommentary($"{block.BlockName} のアンロードを中断しました。");
            }

            _isBusy = false;
        }

        /// <summary> 依存グラフの異常を1件ずつ実況ログへ残す。 </summary>
        /// <param name="exception"> 発生した依存グラフの異常。 </param>
        private void ReportPlanException(SceneBlockPlanException exception)
        {
            AddCommentary($"{exception.BlockName} の依存関係を解決できませんでした。");
            foreach (string description in exception.Descriptions)
            {
                AddCommentary($"- {description}");
            }
        }

        /// <summary> 表示上限を維持しながら実況ログを末尾へ追加する。 </summary>
        /// <param name="message"> 追加する実況ログ。 </param>
        private void AddCommentary(string message)
        {
            if (_commentaryLogs.Count >= 12)
            {
                _commentaryLogs.Dequeue();
            }

            _commentaryLogs.Enqueue(message);
        }

        /// <summary> 現在の実況ログを改行区切りの表示文字列へまとめる。 </summary>
        /// <returns> 実況ログの表示文字列。 </returns>
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
