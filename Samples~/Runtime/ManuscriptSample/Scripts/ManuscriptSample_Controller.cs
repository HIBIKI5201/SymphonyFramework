using SymphonyFramework.Exceptions;
using SymphonyFramework.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SymphonyFramework.Samples.ManuscriptSample
{
    /// <summary> 2種類のセーブデータを編集、保存、読込、削除する操作サンプル。 </summary>
    public sealed class ManuscriptSample_Controller : MonoBehaviour
    {
        private readonly Queue<string> _commentaryLogs = new();
        private Vector2 _scrollPosition;
        private bool _isBusy;
        private ManuscriptSample_PlayerDataA _PlayerDataA;
        private ManuscriptSample_PlayerDataB _PlayerDataB;

        /// <summary> 両方のサンプルデータをロードし、初期状態を表示する。 </summary>
        /// <remarks>
        ///     3.0.0から<c>Get&lt;T&gt;()</c>は読み込み済みの値だけを返す。
        ///     初回取得は<c>LoadAsync&lt;T&gt;()</c>をawaitし、戻り値で受け取る。
        /// </remarks>
        private async void Start()
        {
            AddCommentary("サンプルを開始しました。永続化済みデータを Store にロードします。");
            _PlayerDataA = await Manuscript.LoadAsync<ManuscriptSample_PlayerDataA>(
                destroyCancellationToken);
            _PlayerDataB = await Manuscript.LoadAsync<ManuscriptSample_PlayerDataB>(
                destroyCancellationToken);
            ReportLoadedDataA();
            ReportLoadedDataB();
        }

        /// <summary> DataAの現在のRegistryインスタンスを保存する。 </summary>
        [ContextMenu("Save Sample Data")]
        public async void SaveSampleData()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            AddCommentary("Registry が保持している現在インスタンスを永続化します。");

            ManuscriptSample_PlayerDataA data = _PlayerDataA;
            await Manuscript.SaveAsync<ManuscriptSample_PlayerDataA>();
            AddCommentary($"保存完了: {data.PlayerName} / Level {data.Level} / Gold {data.Gold}");
            Debug.Log($"Saved: {data.PlayerName} Lv.{data.Level} Gold:{data.Gold}");
            _isBusy = false;
        }

        /// <summary> DataAを保存先から再読込する。 </summary>
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

        /// <summary> DataAの保存値を削除してRegistryインスタンスを初期化する。 </summary>
        [ContextMenu("Delete Sample Data")]
        public async void DeleteSampleData()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            AddCommentary("永続化データと Registry 上の現在インスタンスを削除します。次回アクセス時は初期データが自動生成されます。");
            await Manuscript.DeleteAsync<ManuscriptSample_PlayerDataA>();
            AddCommentary("削除完了。次のアクセスで Registry が新しい初期インスタンスを生成します。");
            Debug.Log("Deleted ManuscriptSample_PlayerData");
            _isBusy = false;
        }

        /// <summary> DataAを再読込し、画面が参照するインスタンスとログを更新する。 </summary>
        private async Awaitable LoadInternalAsync()
        {
            await Manuscript.LoadAsync<ManuscriptSample_PlayerDataA>();
            _PlayerDataA = Manuscript.Get<ManuscriptSample_PlayerDataA>();
            ReportLoadedDataA();
        }

        /// <summary> 現在のDataAを実況ログとUnity Consoleへ出力する。 </summary>
        private void ReportLoadedDataA()
        {
            ManuscriptSample_PlayerDataA data = _PlayerDataA;
            AddCommentary($"ロード完了: {data.PlayerName} / Level {data.Level} / Gold {data.Gold}");
            Debug.Log($"Loaded: {data.PlayerName} Lv.{data.Level} Gold:{data.Gold}");
        }

        /// <summary> DataBの現在のRegistryインスタンスを保存する。 </summary>
        [ContextMenu("Save Sample Data B")]
        public async void SaveSampleDataB()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            AddCommentary("Registry が保持している DataB の現在インスタンスを永続化します。");

            ManuscriptSample_PlayerDataB data = _PlayerDataB;
            await Manuscript.SaveAsync<ManuscriptSample_PlayerDataB>();
            AddCommentary($"保存完了(B): ItemIDs [{FormatItemIDs(data)}]");
            Debug.Log($"Saved(B): [{FormatItemIDs(data)}]");
            _isBusy = false;
        }

        /// <summary> DataBを保存先から再読込する。 </summary>
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

        /// <summary> DataBの保存値を削除してRegistryインスタンスを初期化する。 </summary>
        [ContextMenu("Delete Sample Data B")]
        public async void DeleteSampleDataB()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            AddCommentary("DataB の永続化データと Registry 上の現在インスタンスを削除します。");
            await Manuscript.DeleteAsync<ManuscriptSample_PlayerDataB>();
            AddCommentary("削除完了(B)。次のアクセスで Registry が新しい初期インスタンスを生成します。");
            Debug.Log("Deleted ManuscriptSample_PlayerDataB");
            _isBusy = false;
        }

        /// <summary> DataBを再読込し、画面が参照するインスタンスとログを更新する。 </summary>
        private async Awaitable LoadInternalAsyncB()
        {
            await Manuscript.LoadAsync<ManuscriptSample_PlayerDataB>();
            _PlayerDataB = Manuscript.Get<ManuscriptSample_PlayerDataB>();
            ReportLoadedDataB();
        }

        /// <summary> 現在のDataBを実況ログとUnity Consoleへ出力する。 </summary>
        private void ReportLoadedDataB()
        {
            ManuscriptSample_PlayerDataB data = _PlayerDataB;
            AddCommentary($"ロード完了(B): ItemIDs [{FormatItemIDs(data)}]");
            Debug.Log($"Loaded(B): [{FormatItemIDs(data)}]");
        }

        /// <summary> セーブ状態、編集操作、実況ログをIMGUIで描画する。 </summary>
        private void OnGUI()
        {
            ManuscriptSample_PlayerDataA dataA = _PlayerDataA;
            ManuscriptSample_PlayerDataB dataB = _PlayerDataB;
            float margin = Mathf.Max(12f, Screen.width * 0.025f);
            float width = Screen.width - (margin * 2f);
            float height = Screen.height - (margin * 2f);
            Rect outerRect = new(margin, margin, width, height);
            Rect innerRect = new(
                outerRect.x + 12f,
                outerRect.y + 28f,
                outerRect.width - 24f,
                outerRect.height - 40f);

            GUI.Box(outerRect, "Manuscript Sample");

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
            GUILayout.Label($"Saved Exists : {Manuscript.Exists<ManuscriptSample_PlayerDataA>()}");
            GUILayout.Label($"Cache Loaded : {IsCacheLoaded<ManuscriptSample_PlayerDataA>()}");
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
            GUILayout.Label($"Saved Exists : {Manuscript.Exists<ManuscriptSample_PlayerDataB>()}");
            GUILayout.Label($"Cache Loaded : {IsCacheLoaded<ManuscriptSample_PlayerDataB>()}");
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

        /// <summary> DataAのプレイヤー名を定義済み候補の次の値へ変更する。 </summary>
        private void RenameHero()
        {
            ManuscriptSample_PlayerDataA data = Manuscript.Get<ManuscriptSample_PlayerDataA>();
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

        /// <summary> DataAの編集中の値をサンプル既定値へ戻す。 </summary>
        private void ResetDraft()
        {
            ManuscriptSample_PlayerDataA data = Manuscript.Get<ManuscriptSample_PlayerDataA>();
            data.PlayerName = NAME_OPTION_1;
            data.Level = 1;
            data.Gold = 100;
            AddCommentary("編集中の値を初期状態へ戻しました。");
        }

        /// <summary> DataAのレベルを1増加させる。 </summary>
        private void IncreaseLevel()
        {
            ManuscriptSample_PlayerDataA data = Manuscript.Get<ManuscriptSample_PlayerDataA>();
            data.Level++;
            AddCommentary("Level を 1 増やしました。まだ保存はされていません。");
        }

        /// <summary> DataAの所持金を100増加させる。 </summary>
        private void IncreaseGold()
        {
            ManuscriptSample_PlayerDataA data = Manuscript.Get<ManuscriptSample_PlayerDataA>();
            data.Gold += 100;
            AddCommentary("Gold を 100 増やしました。まだ保存はされていません。");
        }

        /// <summary> DataBへ既存最大値の次のアイテムIDを追加する。 </summary>
        private void AddItem()
        {
            ManuscriptSample_PlayerDataB data = Manuscript.Get<ManuscriptSample_PlayerDataB>();
            int nextId = data.ItemIDs.Length == 0 ? 1 : data.ItemIDs.Max() + 1;
            data.ItemIDs = data.ItemIDs.Append(nextId).ToArray();
            AddCommentary($"Item {nextId} を追加しました。まだ保存はされていません。");
        }

        /// <summary> DataBのアイテムID一覧を空にする。 </summary>
        private void ClearItems()
        {
            ManuscriptSample_PlayerDataB data = Manuscript.Get<ManuscriptSample_PlayerDataB>();
            data.ItemIDs = Array.Empty<int>();
            AddCommentary("Item をすべて削除しました。まだ保存はされていません。");
        }

        /// <summary> DataBのアイテムID一覧を表示用文字列へ変換する。 </summary>
        private static string FormatItemIDs(ManuscriptSample_PlayerDataB data)
        {
            return string.Join(", ", data.ItemIDs);
        }

        /// <summary> 指定型のデータがRegistryへキャッシュ済みか確認する。 </summary>
        private static bool IsCacheLoaded<T>() where T : SaveDataContent, new()
        {
            IReadOnlyList<SaveDataEntryInfo> entries;
            try
            {
                entries = Manuscript.GetEntries();
            }
            catch (SymphonyNotInitializedException)
            {
                // Play開始直後などManuscriptの初期化前は、まだ何もキャッシュされていないものとして表示する。
                return false;
            }

            foreach (SaveDataEntryInfo entry in entries)
            {
                if (entry.DataType == typeof(T))
                {
                    return true;
                }
            }

            return false;
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
