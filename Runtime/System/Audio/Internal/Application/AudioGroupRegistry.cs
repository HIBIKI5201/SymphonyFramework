using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     グループ名をキーに<see cref="AudioGroupEntity"/>を所有し、検索と消去を担当する。
    ///     構築処理を実行済みかどうかもここで保持する。
    /// </summary>
    internal sealed class AudioGroupRegistry
    {
        /// <summary>
        ///     構築処理を実行済みかどうか。
        ///     **結果が空でも実行済みとして扱う。** AudioMixerが未割り当ての場合に
        ///     警告が呼び出しのたびに出るのを防ぐため。
        /// </summary>
        internal bool IsBuilt { get; private set; }

        /// <summary> 登録されているグループの件数。 </summary>
        internal int Count => _groups.Count;

        /// <summary> 構築処理を実行済みとして記録する。 </summary>
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
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (_groups.ContainsKey(entity.GroupName))
            {
                return false;
            }

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
            if (string.IsNullOrEmpty(groupName))
            {
                entity = null;
                return false;
            }

            return _groups.TryGetValue(groupName, out entity);
        }

        /// <summary> 全ての登録と構築済み記録を消去する。 </summary>
        internal void Clear()
        {
            _groups.Clear();
            IsBuilt = false;
        }

        private readonly Dictionary<string, AudioGroupEntity> _groups = new();
    }
}
