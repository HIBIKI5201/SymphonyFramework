using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータのEntityを公開Infoまたは表示用Dtoへ変換する。
    ///     永続化データの存在確認はローダーへのI/Oになるため、ここでは行わない。
    /// </summary>
    internal sealed class SaveDataQuery
    {
        /// <summary> 読み取り対象のレジストリを指定してQueryを生成する。 </summary>
        /// <param name="registry"> Entityを所有するレジストリ。 </param>
        internal SaveDataQuery(SaveDataEntryRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary> キャッシュ済みエントリの公開スナップショット一覧を返す。 </summary>
        /// <returns> 型の完全名を基準にordinal昇順で並んだ変更不能な一覧。 </returns>
        internal IReadOnlyList<SaveDataEntryInfo> GetInfos()
        {
            List<SaveDataEntryEntity> entities = GetSortedEntities();
            var infos = new SaveDataEntryInfo[entities.Count];

            for (int i = 0; i < entities.Count; i++)
            {
                SaveDataEntryEntity entity = entities[i];
                infos[i] = new SaveDataEntryInfo(
                    entity.DataType,
                    entity.Content,
                    entity.IsLoaded);
            }

            return Array.AsReadOnly(infos);
        }

        /// <summary> キャッシュ済みエントリの表示用更新値一覧を返す。 </summary>
        /// <returns> 型の完全名を基準にordinal昇順で並んだ変更不能な一覧。 </returns>
        internal IReadOnlyList<SaveDataDto> GetDtos()
        {
            List<SaveDataEntryEntity> entities = GetSortedEntities();
            var dtos = new SaveDataDto[entities.Count];

            for (int i = 0; i < entities.Count; i++)
            {
                SaveDataEntryEntity entity = entities[i];
                dtos[i] = new SaveDataDto(
                    entity.DataType,
                    GetTypeName(entity.DataType),
                    entity.Content?.SaveDate,
                    entity.IsLoaded);
            }

            return Array.AsReadOnly(dtos);
        }

        /// <summary> レジストリのEntityを型の完全名を基準にordinal昇順で複製する。 </summary>
        /// <returns> 並べ替え済みEntity一覧。 </returns>
        private List<SaveDataEntryEntity> GetSortedEntities()
        {
            var entities = new List<SaveDataEntryEntity>(_registry.GetEntities());
            entities.Sort(CompareEntities);
            return entities;
        }

        /// <summary> 2つのEntityをセーブデータ型の完全名で比較する。 </summary>
        /// <param name="left"> 左辺のEntity。 </param>
        /// <param name="right"> 右辺のEntity。 </param>
        /// <returns> 比較結果。 </returns>
        private static int CompareEntities(
            SaveDataEntryEntity left,
            SaveDataEntryEntity right) =>
            StringComparer.Ordinal.Compare(
                GetTypeName(left.DataType),
                GetTypeName(right.DataType));

        /// <summary> 型の完全名を取得し、完全名が無い型では短い型名を返す。 </summary>
        /// <param name="dataType"> 表示する型。 </param>
        /// <returns> 型の完全名または短い型名。 </returns>
        private static string GetTypeName(Type dataType) =>
            dataType.FullName ?? dataType.Name;

        private readonly SaveDataEntryRegistry _registry;
    }
}
