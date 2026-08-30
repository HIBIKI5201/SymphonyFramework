using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockの追跡状態を、公開API向けの不変なスナップショットへ変換する。
    /// </summary>
    internal sealed class SceneBlockQuery
    {
        #region 外部向けAPI

        /// <summary>
        ///     読み取り対象の追跡状態を指定して生成する。
        /// </summary>
        /// <param name="registry"> ブロックとシーン保持の追跡状態。 </param>
        /// <exception cref="ArgumentNullException"> registryがnullの場合。 </exception>
        internal SceneBlockQuery(SceneBlockRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        ///     指定したブロックのスナップショットを取得する。
        /// </summary>
        /// <param name="blockName"> 取得するブロック名。 </param>
        /// <param name="blockInfo"> 取得できたスナップショット。 </param>
        /// <returns> 追跡中の場合はtrue。 </returns>
        internal bool TryGetInfo(string blockName, out SceneBlockInfo blockInfo)
        {
            if (!_registry.TryGet(blockName, out SceneBlockLoadEntity entity))
            {
                blockInfo = default;
                return false;
            }

            blockInfo = CreateInfo(entity);
            return true;
        }

        /// <summary>
        ///     追跡中の全ブロックのスナップショットを取得する。
        /// </summary>
        /// <returns> ブロック名のOrdinal順に並んだスナップショット。 </returns>
        internal IReadOnlyList<SceneBlockInfo> GetInfos()
        {
            List<string> blockNames = SortedBlockNames();

            List<SceneBlockInfo> blockInfos = new(blockNames.Count);
            foreach (string blockName in blockNames)
            {
                blockInfos.Add(CreateInfo(_registry.Entities[blockName]));
            }

            return blockInfos;
        }

        /// <summary>
        ///     追跡中の全ブロックの表示用更新データを取得する。
        /// </summary>
        /// <returns> ブロック名のOrdinal順に並んだ更新データ。 </returns>
        internal IReadOnlyList<SceneBlockDto> GetDtos()
        {
            List<string> blockNames = SortedBlockNames();

            List<SceneBlockDto> blockDtos = new(blockNames.Count);
            foreach (string blockName in blockNames)
            {
                SceneBlockLoadEntity entity = _registry.Entities[blockName];

                // 公開API向けInfoと同じ値を、View向けの型として別に組み立てる。
                blockDtos.Add(new SceneBlockDto(
                    entity.BlockName,
                    entity.State,
                    entity.Progress,
                    _registry.GetHeldSceneNames(entity.BlockName),
                    entity.Layers.Count));
            }

            return blockDtos;
        }

        #endregion

        #region 内部処理

        private readonly SceneBlockRegistry _registry;

        /// <summary>
        ///     追跡中のブロック名をOrdinal順で取得する。
        /// </summary>
        /// <returns> 並びを固定したブロック名。 </returns>
        private List<string> SortedBlockNames()
        {
            // 辞書の列挙順に依存しない表示にするため、ブロック名で並びを固定する。
            List<string> blockNames = new(_registry.Entities.Keys);
            blockNames.Sort(StringComparer.Ordinal);

            return blockNames;
        }

        /// <summary>
        ///     Entityと保持情報から公開用スナップショットを作る。
        /// </summary>
        /// <param name="entity"> 変換元のブロック。 </param>
        /// <returns> 公開用スナップショット。 </returns>
        private SceneBlockInfo CreateInfo(SceneBlockLoadEntity entity)
        {
            // 保持シーンはRegistryが正本であり、Entityへ二重に持たせない。
            return new SceneBlockInfo(
                entity.BlockName,
                entity.State,
                entity.Progress,
                _registry.GetHeldSceneNames(entity.BlockName),
                entity.Layers.Count);
        }

        #endregion
    }
}
