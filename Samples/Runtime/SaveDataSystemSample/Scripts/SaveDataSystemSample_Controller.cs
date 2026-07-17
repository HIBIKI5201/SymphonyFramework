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
            AddCommentary("サンプルを開始しました。現在のセーブデータを確認します。");

            if (!SaveDataRegistry.Exists<SaveDataSystemSample_PlayerData>())
            {
                AddCommentary("保存済みデータはまだありません。Edit Values で内容を調整して Save を押してください。");
                return;
            }

            AddCommentary("保存済みデータが見つかったのでロードします。");
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
            AddCommentary("Registry にロード済みのインスタンスをそのまま保存します。");

            SaveDataSystemSample_PlayerData data = await GetOrLoadDataAsync();
            await SaveDataRegistry.SaveAsync(data);
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
            AddCommentary("SaveDataRegistry から保存データをロードします。");
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
            AddCommentary("保存データ本体を削除します。次回 Load 時は新規データが生成されます。");
            await SaveDataRegistry.DeleteAsync<SaveDataSystemSample_PlayerData>();
            AddCommentary("削除完了。現在の編集値は画面上に残りますが、保存ファイルは消えています。");
            Debug.Log("Deleted SaveDataSystemSample_PlayerData");
            _isBusy = false;
        }

        [ContextMenu("Unload Sample Cache")]
        public void UnloadSampleCache()
        {
            SaveDataRegistry.Unload<SaveDataSystemSample_PlayerData>();
            AddCommentary("Registry のキャッシュだけを破棄しました。保存ファイル自体は残っています。");
            Debug.Log("Unloaded SaveDataSystemSample_PlayerData cache");
        }

        private async Awaitable LoadInternalAsync()
        {
            SaveDataSystemSample_PlayerData data = await SaveDataRegistry.LoadAsync<SaveDataSystemSample_PlayerData>();

            AddCommentary($"ロード完了: {data.PlayerName} / Level {data.Level} / Gold {data.Gold}");
            Debug.Log($"Loaded: {data.PlayerName} Lv.{data.Level} Gold:{data.Gold}");
        }

        private void OnGUI()
        {
            SaveDataSystemSample_PlayerData data = GetLoadedDataOrFallback();
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
            GUILayout.Label("1. Edit Values で編集中のプレイヤーデータを変えます。");
            GUILayout.Label("2. Save で SaveDataRegistry 経由の保存を試します。");
            GUILayout.Label("3. Load / Unload Cache / Delete で挙動の違いを確認できます。");
            GUILayout.Space(8f);

            GUILayout.Label($"Editing Name : {data.PlayerName}");
            GUILayout.Label($"Editing Level: {data.Level}");
            GUILayout.Label($"Editing Gold : {data.Gold}");
            GUILayout.Label($"Saved Exists : {SaveDataRegistry.Exists<SaveDataSystemSample_PlayerData>()}");
            GUILayout.Label($"Cache Loaded : {IsCacheLoaded()}");
            GUILayout.Label($"Busy         : {_isBusy}");
            GUILayout.Space(8f);

            GUILayout.Label("Edit Values");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rename Hero")) { RenameHero(); }
            if (GUILayout.Button("Level +1")) { data.Level++; AddCommentary("Level を 1 増やしました。まだ保存はされていません。"); }
            if (GUILayout.Button("Gold +100")) { data.Gold += 100; AddCommentary("Gold を 100 増やしました。まだ保存はされていません。"); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Draft")) { ResetDraft(); }
            if (GUILayout.Button("Save")) { SaveSampleData(); }
            if (GUILayout.Button("Load")) { LoadSampleData(); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unload Cache")) { UnloadSampleCache(); }
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
            SaveDataSystemSample_PlayerData data = GetLoadedDataOrFallback();
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
            SaveDataSystemSample_PlayerData data = GetLoadedDataOrFallback();
            data.PlayerName = "Symphony";
            data.Level = 1;
            data.Gold = 100;
            AddCommentary("編集中の値を初期状態へ戻しました。");
        }

        private SaveDataSystemSample_PlayerData GetLoadedDataOrFallback()
        {
            if (SaveDataRegistry.TryGetLoaded(out SaveDataSystemSample_PlayerData data))
            {
                return data;
            }

            return new SaveDataSystemSample_PlayerData();
        }

        private async Awaitable<SaveDataSystemSample_PlayerData> GetOrLoadDataAsync()
        {
            if (SaveDataRegistry.TryGetLoaded(out SaveDataSystemSample_PlayerData data))
            {
                return data;
            }

            return await SaveDataRegistry.LoadAsync<SaveDataSystemSample_PlayerData>();
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
