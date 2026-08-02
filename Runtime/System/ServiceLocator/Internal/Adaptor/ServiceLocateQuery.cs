using System;
using System.Collections.Generic;

using UnityEngine;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary> Service RegistryとEntityを公開InfoまたはView用Dtoへ変換する。 </summary>
    internal sealed class ServiceLocateQuery
    {
        /// <summary> 読み取り対象のRegistryを指定してQueryを生成する。 </summary>
        /// <param name="registry"> Service Entityを所有するRegistry。 </param>
        internal ServiceLocateQuery(ServiceLocateRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        private readonly ServiceLocateRegistry _registry;

        /// <summary> 指定型の登録payloadを取得する。 </summary>
        /// <param name="serviceType"> 検索する登録キー。 </param>
        /// <param name="instance"> 取得できたpayload。 </param>
        /// <returns> 登録payloadを取得できた場合はtrue。 </returns>
        internal bool TryGetInstance(Type serviceType, out object instance)
        {
            if (!_registry.TryGet(
                serviceType,
                out ServiceRegistrationEntity entity))
            {
                instance = null;
                return false;
            }

            instance = entity.Instance;
            return true;
        }

        /// <summary> 指定型が登録済みか判定する。 </summary>
        /// <param name="serviceType"> 検索する登録キー。 </param>
        /// <returns> 登録済みの場合はtrue。 </returns>
        internal bool Contains(Type serviceType) =>
            _registry.Contains(serviceType);

        /// <summary> 指定型の公開スナップショットを取得する。 </summary>
        /// <param name="serviceType"> 検索する登録キー。 </param>
        /// <param name="registrationInfo"> 取得できた公開スナップショット。 </param>
        /// <returns> 登録中のServiceを取得できた場合はtrue。 </returns>
        internal bool TryGetInfo(
            Type serviceType,
            out ServiceRegistrationInfo registrationInfo)
        {
            if (!_registry.TryGet(
                serviceType,
                out ServiceRegistrationEntity entity))
            {
                registrationInfo = default;
                return false;
            }

            registrationInfo = CreateInfo(entity);
            return true;
        }

        /// <summary> 登録中Serviceの公開スナップショット一覧を返す。 </summary>
        /// <returns> 型の完全名を基準にordinal昇順で並んだ変更不能な一覧。 </returns>
        internal IReadOnlyList<ServiceRegistrationInfo> GetInfos()
        {
            List<ServiceRegistrationEntity> entities = GetSortedEntities();
            var registrationInfos = new ServiceRegistrationInfo[entities.Count];

            for (int i = 0; i < entities.Count; i++)
            {
                registrationInfos[i] = CreateInfo(entities[i]);
            }

            return Array.AsReadOnly(registrationInfos);
        }

        /// <summary> 登録中ServiceのView用更新値一覧を返す。 </summary>
        /// <returns> 型の完全名を基準にordinal昇順で並んだ変更不能な一覧。 </returns>
        internal IReadOnlyList<ServiceLocateDto> GetDtos()
        {
            List<ServiceRegistrationEntity> entities = GetSortedEntities();
            var serviceDtos = new ServiceLocateDto[entities.Count];

            for (int i = 0; i < entities.Count; i++)
            {
                ServiceRegistrationEntity entity = entities[i];
                serviceDtos[i] = new ServiceLocateDto(
                    entity.ServiceType.Name,
                    GetInstanceName(entity.Instance),
                    entity.LocateType);
            }

            return Array.AsReadOnly(serviceDtos);
        }

        /// <summary> Entityから公開スナップショットを生成する。 </summary>
        /// <param name="entity"> 変換元Entity。 </param>
        /// <returns> 公開スナップショット。 </returns>
        private static ServiceRegistrationInfo CreateInfo(
            ServiceRegistrationEntity entity) =>
            new(
                entity.ServiceType,
                entity.Instance,
                entity.LocateType);

        /// <summary> 登録payloadの表示名を取得する。 </summary>
        /// <param name="instance"> 登録されたpayload。 </param>
        /// <returns> Unity Object名または実行時型名。 </returns>
        private static string GetInstanceName(object instance)
        {
            if (instance is Component component)
            {
                return component == null
                    ? "(Destroyed)"
                    : component.name;
            }

            if (instance is UnityEngine.Object unityObject)
            {
                return unityObject == null
                    ? "(Destroyed)"
                    : unityObject.name;
            }

            return instance?.GetType().Name;
        }

        /// <summary> RegistryのEntityを型の完全名を基準にordinal昇順で複製する。 </summary>
        /// <returns> 並べ替え済みEntity一覧。 </returns>
        private List<ServiceRegistrationEntity> GetSortedEntities()
        {
            var entities = new List<ServiceRegistrationEntity>(
                _registry.Entities.Values);
            entities.RemoveAll(entity => !entity.IsRegistered);
            entities.Sort(CompareEntities);
            return entities;
        }

        /// <summary> 2つのEntityを登録キーの完全名で比較する。 </summary>
        /// <param name="left"> 左辺のEntity。 </param>
        /// <param name="right"> 右辺のEntity。 </param>
        /// <returns> 比較結果。 </returns>
        private static int CompareEntities(
            ServiceRegistrationEntity left,
            ServiceRegistrationEntity right) =>
            StringComparer.Ordinal.Compare(
                GetTypeName(left.ServiceType),
                GetTypeName(right.ServiceType));

        /// <summary> 型の完全名を取得し、完全名が無い型では短い型名を返す。 </summary>
        /// <param name="serviceType"> 表示する型。 </param>
        /// <returns> 型の完全名または短い型名。 </returns>
        private static string GetTypeName(Type serviceType) =>
            serviceType.FullName ?? serviceType.Name;
    }
}
