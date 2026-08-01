using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary> Scene RegistryとEntityを公開InfoまたはView用Dtoへ変換する。 </summary>
    internal sealed class SceneLoadQuery
    {
        /// <summary> 読み取り対象のRegistryを指定してQueryを生成する。 </summary>
        /// <param name="registry"> Scene Entityを所有するRegistry。 </param>
        internal SceneLoadQuery(SceneLoadRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        private readonly SceneLoadRegistry _registry;

        /// <summary> 指定したSceneの公開スナップショットを取得する。 </summary>
        /// <param name="sceneName"> 取得するシーン名。 </param>
        /// <param name="sceneInfo"> 取得できた公開スナップショット。 </param>
        /// <returns> 追跡中のSceneを取得できた場合はtrue。 </returns>
        internal bool TryGetInfo(string sceneName, out SceneLoadInfo sceneInfo)
        {
            if (!_registry.TryGet(sceneName, out SceneLoadEntity entity))
            {
                sceneInfo = default;
                return false;
            }

            string activeSceneName = _registry.ActiveSceneName;
            sceneInfo = CreateInfo(entity, activeSceneName);
            return true;
        }

        /// <summary> 追跡中Sceneの公開スナップショット一覧を返す。 </summary>
        /// <returns> Scene名のordinal昇順で並んだ変更不能な一覧。 </returns>
        internal IReadOnlyList<SceneLoadInfo> GetInfos()
        {
            List<SceneLoadEntity> entities = GetSortedEntities();
            string activeSceneName = _registry.ActiveSceneName;
            var sceneInfos = new SceneLoadInfo[entities.Count];

            for (int i = 0; i < entities.Count; i++)
            {
                sceneInfos[i] = CreateInfo(entities[i], activeSceneName);
            }

            return Array.AsReadOnly(sceneInfos);
        }

        /// <summary> 追跡中SceneのView用更新値一覧を返す。 </summary>
        /// <returns> Scene名のordinal昇順で並んだ変更不能な一覧。 </returns>
        internal IReadOnlyList<SceneLoadDto> GetDtos()
        {
            List<SceneLoadEntity> entities = GetSortedEntities();
            string activeSceneName = _registry.ActiveSceneName;
            var sceneDtos = new SceneLoadDto[entities.Count];

            for (int i = 0; i < entities.Count; i++)
            {
                SceneLoadEntity entity = entities[i];
                sceneDtos[i] = new SceneLoadDto(
                    entity.Name,
                    entity.State,
                    entity.Priority,
                    entity.Progress,
                    IsActive(entity.Name, activeSceneName));
            }

            return Array.AsReadOnly(sceneDtos);
        }

        /// <summary> Entityから公開スナップショットを生成する。 </summary>
        /// <param name="entity"> 変換元Entity。 </param>
        /// <param name="activeSceneName"> 取得時点のActive Scene名。 </param>
        /// <returns> 公開スナップショット。 </returns>
        private static SceneLoadInfo CreateInfo(
            SceneLoadEntity entity,
            string activeSceneName) =>
            new(
                entity.Name,
                entity.State,
                entity.Priority,
                entity.Progress,
                IsActive(entity.Name, activeSceneName));

        /// <summary> 2つのScene名が同じActive Sceneを示すか判定する。 </summary>
        /// <param name="sceneName"> 判定するScene名。 </param>
        /// <param name="activeSceneName"> 取得時点のActive Scene名。 </param>
        /// <returns> 一致する場合はtrue。 </returns>
        private static bool IsActive(string sceneName, string activeSceneName) =>
            string.Equals(sceneName, activeSceneName, StringComparison.Ordinal);

        /// <summary> RegistryのEntityをScene名のordinal昇順で複製する。 </summary>
        /// <returns> 並べ替え済みEntity一覧。 </returns>
        private List<SceneLoadEntity> GetSortedEntities()
        {
            var entities = new List<SceneLoadEntity>(_registry.Entities.Values);
            entities.Sort(CompareEntities);
            return entities;
        }

        /// <summary> 2つのEntityをScene名のordinal順で比較する。 </summary>
        /// <param name="left"> 左辺のEntity。 </param>
        /// <param name="right"> 右辺のEntity。 </param>
        /// <returns> 比較結果。 </returns>
        private static int CompareEntities(
            SceneLoadEntity left,
            SceneLoadEntity right) =>
            StringComparer.Ordinal.Compare(left.Name, right.Name);
    }
}
