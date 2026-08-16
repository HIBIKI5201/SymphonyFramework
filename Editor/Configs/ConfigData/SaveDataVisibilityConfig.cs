using System;
using System.Collections.Generic;

using SymphonyFrameWork.Core;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     セーブデータ型ごとの管理対象上書きを保持する。
    /// </summary>
    [FilePath(EditorSymphonyConstant.PROJCET_SETTING_FILE_PATH + nameof(SaveDataVisibilityConfig) + ".asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class SaveDataVisibilityConfig : ScriptableSingleton<SaveDataVisibilityConfig>
    {
        #region 外部向けAPI

        /// <summary> 管理対象の上書き値が変更されたときに通知する。 </summary>
        internal event Action OnChanged;

        /// <summary>
        ///     保存済みの上書き値を変更不能なスナップショットとして取得する。
        /// </summary>
        /// <returns> 型の完全名と管理対象値の対応。 </returns>
        internal IReadOnlyDictionary<string, bool> GetOverrides()
        {
            // シリアライズ一覧を描画中の変更から切り離した辞書へ変換する。
            Dictionary<string, bool> overrides = new(StringComparer.Ordinal);
            foreach (VisibilityEntry entry in _entries)
            {
                // 壊れたシリアライズ値は設定キーとして使えないため読み飛ばす。
                if (entry == null || string.IsNullOrEmpty(entry.TypeFullName)) { continue; }

                overrides[entry.TypeFullName] = entry.IsManaged;
            }

            return overrides;
        }

        /// <summary>
        ///     1つの型へ管理対象の上書き値を保存する。
        /// </summary>
        /// <param name="typeFullName"> 型の完全名。 </param>
        /// <param name="isManaged"> 管理対象にする場合はtrue。 </param>
        internal void SetManaged(string typeFullName, bool isManaged)
        {
            // 無効な型名は保存せず、IMGUI描画中の例外も発生させない。
            if (string.IsNullOrEmpty(typeFullName)) { return; }

            // 実際に値が変わった場合だけ永続化と通知を行う。
            if (!SetManagedWithoutSave(typeFullName, isManaged)) { return; }

            SaveAndNotify();
        }

        /// <summary>
        ///     複数の型へ同じ管理対象の上書き値を一括保存する。
        /// </summary>
        /// <param name="typeFullNames"> 対象型の完全名。 </param>
        /// <param name="isManaged"> 管理対象にする場合はtrue。 </param>
        internal void SetManaged(IEnumerable<string> typeFullNames, bool isManaged)
        {
            // 列挙元が無い場合は変更なしとして扱う。
            if (typeFullNames == null) { return; }

            bool isChanged = false;
            // 全要素を書き換えてから1回だけ保存と通知を行う。
            foreach (string typeFullName in typeFullNames)
            {
                if (string.IsNullOrEmpty(typeFullName)) { continue; }

                isChanged |= SetManagedWithoutSave(typeFullName, isManaged);
            }

            if (isChanged) { SaveAndNotify(); }
        }

        #endregion

        #region 内部処理

        [SerializeField, Tooltip("型の完全名ごとに明示された管理対象の上書き値。")]
        private List<VisibilityEntry> _entries = new();

        /// <summary>
        ///     上書き値をメモリ上へ反映する。
        /// </summary>
        /// <param name="typeFullName"> 型の完全名。 </param>
        /// <param name="isManaged"> 管理対象にする場合はtrue。 </param>
        /// <returns> 保存内容が変わった場合はtrue。 </returns>
        private bool SetManagedWithoutSave(string typeFullName, bool isManaged)
        {
            // 同じ型の既存エントリがあれば、その値だけを更新する。
            foreach (VisibilityEntry entry in _entries)
            {
                if (entry == null || entry.TypeFullName != typeFullName) { continue; }
                if (entry.IsManaged == isManaged) { return false; }

                entry.IsManaged = isManaged;
                return true;
            }

            // 初めて明示された型だけ、新しいシリアライズ要素として追加する。
            _entries.Add(new VisibilityEntry(typeFullName, isManaged));
            return true;
        }

        /// <summary>
        ///     現在の設定をProjectSettingsへ保存して変更を通知する。
        /// </summary>
        private void SaveAndNotify()
        {
            // すべての上書きを反映してから1回だけディスクへ保存する。
            Save(true);
            OnChanged?.Invoke();
        }

        /// <summary>
        ///     1つの型に対する管理対象の上書き値を保持する。
        /// </summary>
        [Serializable]
        private sealed class VisibilityEntry
        {
            #region 外部向けAPI

            /// <summary>
            ///     型名と管理対象値を指定して初期化する。
            /// </summary>
            /// <param name="typeFullName"> 型の完全名。 </param>
            /// <param name="isManaged"> 管理対象にする場合はtrue。 </param>
            internal VisibilityEntry(string typeFullName, bool isManaged)
            {
                _typeFullName = typeFullName;
                _isManaged = isManaged;
            }

            /// <summary> 型の完全名。 </summary>
            internal string TypeFullName => _typeFullName;

            /// <summary> 管理対象にするかを示す。 </summary>
            internal bool IsManaged
            {
                get => _isManaged;
                set => _isManaged = value;
            }

            #endregion

            #region 内部処理

            [SerializeField, Tooltip("管理対象を上書きする型の完全名。")]
            private string _typeFullName;

            [SerializeField, Tooltip("管理対象にするか。")]
            private bool _isManaged;

            #endregion
        }

        #endregion
    }
}
