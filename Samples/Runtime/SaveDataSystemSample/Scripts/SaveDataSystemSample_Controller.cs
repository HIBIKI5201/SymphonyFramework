using SymphonyFrameWork.System.SaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SymphonyFrameWork.Samples.SaveDataSystemSample
{
    public class SaveDataSystemSample_Controller : MonoBehaviour
    {
        private readonly Queue<string> _commentaryLogs = new();
        private Vector2 _scrollPosition;
        private bool _isBusy;
        private SaveDataSystemSample_PlayerDataA _PlayerDataA;
        private SaveDataSystemSample_PlayerDataB _PlayerDataB;

        private void Start()
        {
            AddCommentary("サンプルを開始しました。永続化済みデータを Registry にロードします。");
            _PlayerDataA = SaveDataRegistry.Get<SaveDataSystemSample_PlayerDataA>();
            _PlayerDataB = SaveDataRegistry.Get<SaveDataSystemSample_PlayerDataB>();
            ReportLoadedDataA();
            ReportLoadedDataB();
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

            SaveDataSystemSample_PlayerDataA data = _PlayerDataA;
            await SaveDataRegistry.SaveAsync<SaveDataSystemSample_PlayerDataA>();
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
            await SaveDataRegistry.DeleteAsync<SaveDataSystemSample_PlayerDataA>();
            AddCommentary("削除完了。次のアクセスで Registry が新しい初期インスタンスを生成します。");
            Debug.Log("Deleted SaveDataSystemSample_PlayerData");
            _isBusy = false;
        }

        private async Awaitable LoadInternalAsync()
        {
            await SaveDataRegistry.LoadAsync<SaveDataSystemSample_PlayerDataA>();
            _PlayerDataA = SaveDataRegistry.Get<SaveDataSystemSample_PlayerDataA>();
            ReportLoadedDataA();
        }

        private void ReportLoadedDataA()
        {
            SaveDataSystemSample_PlayerDataA data = _PlayerDataA;
            AddCommentary($"ロード完了: {data.PlayerName} / Level {data.Level} / Gold {data.Gold}");
            Debug.Log($"Loaded: {data.PlayerName} Lv.{data.Level} Gold:{data.Gold}");
        }

        [ContextMenu("Save Sample Data B")]
        public async void SaveSampleDataB()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            AddCommentary("Registry が保持している DataB の現在インスタンスを永続化します。");

            SaveDataSystemSample_PlayerDataB data = _PlayerDataB;
            await SaveDataRegistry.SaveAsync<SaveDataSystemSample_PlayerDataB>();
            AddCommentary($"保存完了(B): ItemIDs [{FormatItemIDs(data)}]");
            Debug.Log($"Saved(B): [{FormatItemIDs(data)}]");
            _isBusy = false;
        }

        [ContextMenu("Load Sample Data B")]
        public async void LoadSampleDataB()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            AddCommentary("永続化済みの DataB を読み込み、Registry 上の現在インスタンスを差し替えます。");
            await LoadInternalAsyncB();
            _isBusy = false;
        }

        [ContextMenu("Delete Sample Data B")]
        public async void DeleteSampleDataB()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            AddCommentary("DataB の永続化データと Registry 上の現在インスタンスを削除します。");
            await SaveDataRegistry.DeleteAsync<SaveDataSystemSample_PlayerDataB>();
            AddCommentary("削除完了(B)。次のアクセスで Registry が新しい初期インスタンスを生成します。");
            Debug.Log("Deleted SaveDataSystemSample_PlayerDataB");
            _isBusy = false;
        }

        private async Awaitable LoadInternalAsyncB()
        {
            await SaveDataRegistry.LoadAsync<SaveDataSystemSample_PlayerDataB>();
            _PlayerDataB = SaveDataRegistry.Get<SaveDataSystemSample_PlayerDataB>();
            ReportLoadedDataB();
        }

        private void ReportLoadedDataB()
        {
            SaveDataSystemSample_PlayerDataB data = _PlayerDataB;
            AddCommentary($"ロード完了(B): ItemIDs [{FormatItemIDs(data)}]");
            Debug.Log($"Loaded(B): [{FormatItemIDs(data)}]");
        }

        private void OnGUI()
        {
            SaveDataSystemSample_PlayerDataA dataA = _PlayerDataA;
            SaveDataSystemSample_PlayerDataB dataB = _PlayerDataB;
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

            GUILayout.Label($"Editing Name : {dataA.PlayerName}");
            GUILayout.Label($"Editing Level: {dataA.Level}");
            GUILayout.Label($"Editing Gold : {dataA.Gold}");
            GUILayout.Label($"Save Date    : {dataA.SaveDate ?? "(unsaved)"}");
            GUILayout.Label($"Saved Exists : {SaveDataRegistry.Exists<SaveDataSystemSample_PlayerDataA>()}");
            GUILayout.Label($"Cache Loaded : {IsCacheLoaded<SaveDataSystemSample_PlayerDataA>()}");
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

            GUILayout.Space(12f);
            GUILayout.Label("---- DataB (Items) ----");
            GUILayout.Label($"Item IDs     : [{FormatItemIDs(dataB)}]");
            GUILayout.Label($"Save Date(B) : {dataB.SaveDate ?? "(unsaved)"}");
            GUILayout.Label($"Saved Exists : {SaveDataRegistry.Exists<SaveDataSystemSample_PlayerDataB>()}");
            GUILayout.Label($"Cache Loaded : {IsCacheLoaded<SaveDataSystemSample_PlayerDataB>()}");
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Item")) { AddItem(); }
            if (GUILayout.Button("Clear Items")) { ClearItems(); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save B")) { SaveSampleDataB(); }
            if (GUILayout.Button("Load B")) { LoadSampleDataB(); }
            if (GUILayout.Button("Delete B")) { DeleteSampleDataB(); }
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
            SaveDataSystemSample_PlayerDataA data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerDataA>();
            string playerName = data.PlayerName;
            playerName = playerName == NAME_OPTION_1
                ? NAME_OPTION_2
                : playerName == NAME_OPTION_2
                    ? NAME_OPTION_3
                    : NAME_OPTION_1;
            data.PlayerName = playerName;

            AddCommentary($"名前を {playerName} に変更しました。まだ保存はされていません。");
        }

        private const string NAME_OPTION_1 = "Symphony";
        private const string NAME_OPTION_2 = "Framework";
        private const string NAME_OPTION_3 = "Sinfonia";

        private void ResetDraft()
        {
            SaveDataSystemSample_PlayerDataA data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerDataA>();
            data.PlayerName = NAME_OPTION_1;
            data.Level = 1;
            data.Gold = 100;
            AddCommentary("編集中の値を初期状態へ戻しました。");
        }

        private void IncreaseLevel()
        {
            SaveDataSystemSample_PlayerDataA data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerDataA>();
            data.Level++;
            AddCommentary("Level を 1 増やしました。まだ保存はされていません。");
        }

        private void IncreaseGold()
        {
            SaveDataSystemSample_PlayerDataA data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerDataA>();
            data.Gold += 100;
            AddCommentary("Gold を 100 増やしました。まだ保存はされていません。");
        }

        private void AddItem()
        {
            SaveDataSystemSample_PlayerDataB data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerDataB>();
            int nextId = data.ItemIDs.Length == 0 ? 1 : data.ItemIDs.Max() + 1;
            data.ItemIDs = data.ItemIDs.Append(nextId).ToArray();
            AddCommentary($"Item {nextId} を追加しました。まだ保存はされていません。");
        }

        private void ClearItems()
        {
            SaveDataSystemSample_PlayerDataB data = SaveDataRegistry.Get<SaveDataSystemSample_PlayerDataB>();
            data.ItemIDs = Array.Empty<int>();
            AddCommentary("Item をすべて削除しました。まだ保存はされていません。");
        }

        private static string FormatItemIDs(SaveDataSystemSample_PlayerDataB data)
        {
            return string.Join(", ", data.ItemIDs);
        }

        private static bool IsCacheLoaded<T>() where T : SaveDataContent, new()
        {
            foreach (SaveDataRegistryEntryInfo entry in SaveDataRegistry.GetEntries())
            {
                if (entry.DataType == typeof(T))
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
