using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     AudioMixerグループの登録と構築状態を保持する。
    /// </summary>
    internal sealed class AudioGroupRegistry
    {
        #region 外部向けAPI

        /// <summary> 構築処理を実行済みかどうか。 </summary>
        /// <remarks> 結果が空でも、AudioMixer未割り当ての警告を繰り返さないため実行済みとして扱う。 </remarks>
        internal bool IsBuilt { get; private set; }

        /// <summary> 登録されているグループの件数。 </summary>
        internal int Count => _groups.Count;

        /// <summary>
        ///     構築処理を実行済みとして記録する。
        /// </summary>
        internal void MarkBuilt()
        {
            IsBuilt = true;
        }

        /// <summary>
        ///     グループを登録する。同名が登録済みの場合は何もしない。
        /// </summary>
        /// <param name="entity"> 登録するグループ。 </param>
        /// <returns> 新たに登録した場合はtrue。 </returns>
        internal bool TryAdd(AudioGroupEntity entity)
        {
            // グループ名へアクセスできない不完全なEntityを登録させない。
            if (entity == null) { throw new ArgumentNullException(nameof(entity)); }

            // 同名グループは先に構築したAudioSourceとの対応を維持するため上書きしない。
            if (_groups.ContainsKey(entity.GroupName)) { return false; }

            // グループ名をAudioMixer設定と同じ検索キーとして保持する。
            _groups.Add(entity.GroupName, entity);
            return true;
        }

        /// <summary>
        ///     グループ名で登録を検索する。
        /// </summary>
        /// <param name="groupName"> 検索するグループ名。 </param>
        /// <param name="entity"> 見つかったグループ。 </param>
        /// <returns> 登録がある場合はtrue。 </returns>
        internal bool TryGet(string groupName, out AudioGroupEntity entity)
        {
            // AudioMixerグループを特定できない名前ではDictionaryを検索しない。
            if (string.IsNullOrEmpty(groupName))
            {
                entity = null;
                return false;
            }

            return _groups.TryGetValue(groupName, out entity);
        }

        /// <summary>
        ///     全ての登録と構築済み記録を消去する。
        /// </summary>
        internal void Clear()
        {
            // 次回アクセス時にAudioMixer設定から再構築できる初期状態へ戻す。
            _groups.Clear();
            IsBuilt = false;
        }

        #endregion

        #region 内部処理

        private readonly Dictionary<string, AudioGroupEntity> _groups = new();

        #endregion
    }
}
