using SymphonyFrameWork.System.SaveSystem;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SymphonyFrameWork.Samples.SaveDataSystemSample
{
    public class SaveDataSystemSample_Controller : MonoBehaviour
    {
        private readonly Queue<string> _commentaryLogs = new();
        private Vector2 _scrollPosition;
        private bool _isBusy;

        private async void Start()
        {
            AddCommentary("サンプルを開始しました。永続化済みデータを Registry にロードします。");
            await LoadInternalAsync();
        }

        [ContextMenu("Save Sample Data")]
        public async void SaveSampleData()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            AddCommentary("Registry が保持している現在インスタンスを永続化します。");

            SaveDataSystemSample_PlayerData data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerData>();
            await SaveDataRegistry.SaveAsync<SaveDataSystemSample_PlayerData>();
            AddCommentary($"保存完了: {data.PlayerName} / Level {data.Level} / Gold {data.Gold}");
            Debug.Log($"Saved: {data.PlayerName} Lv.{data.Level} Gold:{data.Gold}");
            _isBusy = false;
        }

        [ContextMenu("Load Sample Data")]
        public async void LoadSampleData()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            AddCommentary("永続化済みデータを読み込み、Registry 上の現在インスタンスを差し替えます。");
            await LoadInternalAsync();
            _isBusy = false;
        }

        [ContextMenu("Delete Sample Data")]
        public async void DeleteSampleData()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            AddCommentary("永続化データと Registry 上の現在インスタンスを削除します。次回アクセス時は初期データが自動生成されます。");
            await SaveDataRegistry.DeleteAsync<SaveDataSystemSample_PlayerData>();
            AddCommentary("削除完了。次のアクセスで Registry が新しい初期インスタンスを生成します。");
            Debug.Log("Deleted SaveDataSystemSample_PlayerData");
            _isBusy = false;
        }

        private async Awaitable LoadInternalAsync()
        {
            SaveDataSystemSample_PlayerData data = await SaveDataRegistry.LoadAsync<SaveDataSystemSample_PlayerData>();

            AddCommentary($"ロード完了: {data.PlayerName} / Level {data.Level} / Gold {data.Gold}");
            Debug.Log($"Loaded: {data.PlayerName} Lv.{data.Level} Gold:{data.Gold}");
        }

        private void OnGUI()
        {
            SaveDataSystemSample_PlayerData data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerData>();
            float margin = Mathf.Max(12f, Screen.width * 0.025f);
            float width = Screen.width - (margin * 2f);
            float height = Screen.height - (margin * 2f);
            Rect outerRect = new(margin, margin, width, height);
            Rect innerRect = new(
                outerRect.x + 12f,
                outerRect.y + 28f,
                outerRect.width - 24f,
                outerRect.height - 40f);

            GUI.Box(outerRect, "Save Data System Sample");

            GUILayout.BeginArea(innerRect);
            GUILayout.Label("実況解説");
            GUILayout.Label("1. この画面は常に Registry 上の単一インスタンスだけを参照します。");
            GUILayout.Label("2. Save は現在インスタンスを永続化し、Load は永続化データで現在インスタンスを差し替えます。");
            GUILayout.Label("3. Delete 後に再アクセスすると、Registry が初期インスタンスを自動生成します。");
            GUILayout.Space(8f);

            GUILayout.Label($"Editing Name : {data.PlayerName}");
            GUILayout.Label($"Editing Level: {data.Level}");
            GUILayout.Label($"Editing Gold : {data.Gold}");
            GUILayout.Label($"Save Date    : {data.SaveDate ?? "(unsaved)"}");
            GUILayout.Label($"Saved Exists : {SaveDataRegistry.Exists<SaveDataSystemSample_PlayerData>()}");
            GUILayout.Label($"Cache Loaded : {IsCacheLoaded()}");
            GUILayout.Label($"Busy         : {_isBusy}");
            GUILayout.Space(8f);

            GUILayout.Label("Edit Values");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rename Hero")) { RenameHero(); }
            if (GUILayout.Button("Level +1")) { IncreaseLevel(); }
            if (GUILayout.Button("Gold +100")) { IncreaseGold(); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Draft")) { ResetDraft(); }
            if (GUILayout.Button("Save")) { SaveSampleData(); }
            if (GUILayout.Button("Load")) { LoadSampleData(); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Delete Save")) { DeleteSampleData(); }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("Commentary Log");
            float logHeight = Mathf.Max(180f, innerRect.height * 0.42f);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(logHeight));
            GUILayout.TextArea(BuildCommentaryText(), GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void RenameHero()
        {
            SaveDataSystemSample_PlayerData data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerData>();
            string playerName = data.PlayerName;
            playerName = playerName == "Symphony"
                ? "KillChord"
                : playerName == "KillChord"
                    ? "Sinfonia"
                    : "Symphony";
            data.PlayerName = playerName;

            AddCommentary($"名前を {playerName} に変更しました。まだ保存はされていません。");
        }

        private void ResetDraft()
        {
            SaveDataSystemSample_PlayerData data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerData>();
            data.PlayerName = "Symphony";
            data.Level = 1;
            data.Gold = 100;
            AddCommentary("編集中の値を初期状態へ戻しました。");
        }

        private void IncreaseLevel()
        {
            SaveDataSystemSample_PlayerData data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerData>();
            data.Level++;
            AddCommentary("Level を 1 増やしました。まだ保存はされていません。");
        }

        private void IncreaseGold()
        {
            SaveDataSystemSample_PlayerData data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerData>();
            data.Gold += 100;
            AddCommentary("Gold を 100 増やしました。まだ保存はされていません。");
        }

        private bool IsCacheLoaded()
        {
            foreach (SaveDataRegistryEntryInfo entry in SaveDataRegistry.GetEntries())
            {
                if (entry.DataType == typeof(SaveDataSystemSample_PlayerData))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddCommentary(string message)
        {
            if (_commentaryLogs.Count >= 12)
            {
                _commentaryLogs.Dequeue();
            }

            _commentaryLogs.Enqueue(message);
        }

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
