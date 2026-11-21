using System;
using System.Collections.Generic;

namespace SymphonyFramework.Scenes
{
    /// <summary>
    ///     Suiteの追跡状態を、公開API向けの不変なスナップショットへ変換する。
    /// </summary>
    internal sealed class SuiteQuery
    {
        #region 外部向けAPI

        /// <summary>
        ///     読み取り対象の追跡状態を指定して生成する。
        /// </summary>
        /// <param name="registry"> ブロックとシーン保持の追跡状態。 </param>
        /// <exception cref="ArgumentNullException"> registryがnullの場合。 </exception>
        internal SuiteQuery(SuiteRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        ///     指定したブロックのスナップショットを取得する。
        /// </summary>
        /// <param name="suiteName"> 取得するブロック名。 </param>
        /// <param name="suiteInfo"> 取得できたスナップショット。 </param>
        /// <returns> 追跡中の場合はtrue。 </returns>
        internal bool TryGetInfo(string suiteName, out SuiteInfo suiteInfo)
        {
            if (!_registry.TryGet(suiteName, out SuiteLoadEntity entity))
            {
                suiteInfo = default;
                return false;
            }

            suiteInfo = CreateInfo(entity);
            return true;
        }

        /// <summary>
        ///     追跡中の全ブロックのスナップショットを取得する。
        /// </summary>
        /// <returns> ブロック名のOrdinal順に並んだスナップショット。 </returns>
        internal IReadOnlyList<SuiteInfo> GetInfos()
        {
            List<string> suiteNames = SortedBlockNames();

            List<SuiteInfo> suiteInfos = new(suiteNames.Count);
            foreach (string suiteName in suiteNames)
            {
                suiteInfos.Add(CreateInfo(_registry.Entities[suiteName]));
            }

            return suiteInfos;
        }

        /// <summary>
        ///     追跡中の全ブロックの表示用更新データを取得する。
        /// </summary>
        /// <returns> ブロック名のOrdinal順に並んだ更新データ。 </returns>
        internal IReadOnlyList<SuiteDto> GetDtos()
        {
            List<string> suiteNames = SortedBlockNames();

            List<SuiteDto> blockDtos = new(suiteNames.Count);
            foreach (string suiteName in suiteNames)
            {
                SuiteLoadEntity entity = _registry.Entities[suiteName];

                // 公開API向けInfoと同じ値を、View向けの型として別に組み立てる。
                blockDtos.Add(new SuiteDto(
                    entity.SuiteName,
                    entity.State,
                    entity.Progress,
                    _registry.GetHeldSceneNames(entity.SuiteName),
                    entity.Layers.Count));
            }

            return blockDtos;
        }

        #endregion

        #region 内部処理

        private readonly SuiteRegistry _registry;

        /// <summary>
        ///     追跡中のブロック名をOrdinal順で取得する。
        /// </summary>
        /// <returns> 並びを固定したブロック名。 </returns>
        private List<string> SortedBlockNames()
        {
            // 辞書の列挙順に依存しない表示にするため、ブロック名で並びを固定する。
            List<string> suiteNames = new(_registry.Entities.Keys);
            suiteNames.Sort(StringComparer.Ordinal);

            return suiteNames;
        }

        /// <summary>
        ///     Entityと保持情報から公開用スナップショットを作る。
        /// </summary>
        /// <param name="entity"> 変換元のブロック。 </param>
        /// <returns> 公開用スナップショット。 </returns>
        private SuiteInfo CreateInfo(SuiteLoadEntity entity)
        {
            // 保持シーンはRegistryが正本であり、Entityへ二重に持たせない。
            return new SuiteInfo(
                entity.SuiteName,
                entity.State,
                entity.Progress,
                _registry.GetHeldSceneNames(entity.SuiteName),
                entity.Layers.Count);
        }

        #endregion
    }
}
