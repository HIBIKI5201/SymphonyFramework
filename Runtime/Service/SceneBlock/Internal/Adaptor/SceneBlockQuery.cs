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
            List<string> blockNames = new(_registry.Entities.Keys);

            // 辞書の列挙順に依存しない表示にするため、ブロック名で並びを固定する。
            blockNames.Sort(StringComparer.Ordinal);

            List<SceneBlockInfo> blockInfos = new(blockNames.Count);
            foreach (string blockName in blockNames)
            {
                blockInfos.Add(CreateInfo(_registry.Entities[blockName]));
            }

            return blockInfos;
        }

        #endregion

        #region 内部処理

        private readonly SceneBlockRegistry _registry;

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
