using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータのEntityを読み取り用スナップショットへ変換する。
    /// </summary>
    /// <remarks> 永続化データの存在確認はI/Oになるため行わない。 </remarks>
    internal sealed class SaveDataQuery
    {
        #region 外部向けAPI

        /// <summary>
        ///     読み取り対象のレジストリを指定して生成する。
        /// </summary>
        /// <param name="registry"> Entityを所有するレジストリ。 </param>
        internal SaveDataQuery(SaveDataEntryRegistry registry)
        {
            // 読み取り元が無いQueryは成立しないため、生成時に拒否する。
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        ///     キャッシュ済みエントリの公開スナップショット一覧を返す。
        /// </summary>
        /// <returns> 型の完全名を基準にordinal昇順で並んだ変更不能な一覧。 </returns>
        internal IReadOnlyList<SaveDataEntryInfo> GetInfos()
        {
            // 呼び出すたびに現在のEntityを並べ替え、外部へ可変なEntityを公開しないInfoへ変換する。
            List<SaveDataEntryEntity> entities = GetSortedEntities();
            SaveDataEntryInfo[] infos = new SaveDataEntryInfo[entities.Count];

            // 並べ替え済みの順序を保ったまま、各Entityの現在状態を複製する。
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

        /// <summary>
        ///     キャッシュ済みエントリの表示用更新値一覧を返す。
        /// </summary>
        /// <returns> 型の完全名を基準にordinal昇順で並んだ変更不能な一覧。 </returns>
        internal IReadOnlyList<SaveDataDto> GetDtos()
        {
            // 表示更新の比較に使えるよう、現在のEntityを値だけのDtoへ変換する。
            List<SaveDataEntryEntity> entities = GetSortedEntities();
            SaveDataDto[] dtos = new SaveDataDto[entities.Count];

            // Infoと同じ並び順を維持し、Editorが必要とする表示値だけを抽出する。
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

        #endregion

        #region 内部処理

        private readonly SaveDataEntryRegistry _registry;

        /// <summary>
        ///     Entityを型名のordinal昇順で複製する。
        /// </summary>
        /// <returns> 並べ替え済みEntity一覧。 </returns>
        private List<SaveDataEntryEntity> GetSortedEntities()
        {
            // Registryの複製をさらに並べ替え、保持順を変更せず安定した表示順を作る。
            List<SaveDataEntryEntity> entities = new(_registry.GetEntities());
            entities.Sort(CompareEntities);
            return entities;
        }

        /// <summary>
        ///     2つのEntityをセーブデータ型名で比較する。
        /// </summary>
        /// <param name="left"> 左辺のEntity。 </param>
        /// <param name="right"> 右辺のEntity。 </param>
        /// <returns> 比較結果。 </returns>
        private static int CompareEntities(
            SaveDataEntryEntity left,
            SaveDataEntryEntity right) =>
            StringComparer.Ordinal.Compare(
                GetTypeName(left.DataType),
                GetTypeName(right.DataType));

        /// <summary>
        ///     表示と並べ替えに使う型名を返す。
        /// </summary>
        /// <param name="dataType"> 表示する型。 </param>
        /// <returns> 型の完全名または短い型名。 </returns>
        private static string GetTypeName(Type dataType) =>
            dataType.FullName ?? dataType.Name;

        #endregion
    }
}
