using System;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary> Service Locatorの表示に必要な不変の更新値。 </summary>
    internal readonly struct ServiceLocateDto : IEquatable<ServiceLocateDto>
    {
        /// <summary> 表示に必要な値を指定して更新値を生成する。 </summary>
        /// <param name="serviceTypeName"> 登録キーの短い型名。 </param>
        /// <param name="instanceName"> 登録payloadの表示名。 </param>
        /// <param name="locateType"> 登録時に指定された登録方式。 </param>
        internal ServiceLocateDto(
            string serviceTypeName,
            string instanceName,
            LocateTypeEnum locateType)
        {
            ServiceTypeName = serviceTypeName;
            InstanceName = instanceName;
            LocateType = locateType;
        }

        /// <summary> 登録キーの短い型名。 </summary>
        internal string ServiceTypeName { get; }

        /// <summary> 登録payloadの表示名。 </summary>
        internal string InstanceName { get; }

        /// <summary> 登録時に指定された登録方式。 </summary>
        internal LocateTypeEnum LocateType { get; }

        /// <summary> 指定した更新値と同値か判定する。 </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> すべての値が一致する場合はtrue。 </returns>
        public bool Equals(ServiceLocateDto other) =>
            string.Equals(
                ServiceTypeName,
                other.ServiceTypeName,
                StringComparison.Ordinal)
            && string.Equals(
                InstanceName,
                other.InstanceName,
                StringComparison.Ordinal)
            && LocateType == other.LocateType;

        /// <summary> 指定したオブジェクトと同値か判定する。 </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同値のServiceLocateDtoの場合はtrue。 </returns>
        public override bool Equals(object obj) =>
            obj is ServiceLocateDto other && Equals(other);

        /// <summary> 更新値の全値に基づくハッシュコードを返す。 </summary>
        /// <returns> ハッシュコード。 </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ServiceTypeName != null
                    ? StringComparer.Ordinal.GetHashCode(ServiceTypeName)
                    : 0;
                hashCode = (hashCode * 397)
                    ^ (InstanceName != null
                        ? StringComparer.Ordinal.GetHashCode(InstanceName)
                        : 0);
                hashCode = (hashCode * 397) ^ (int)LocateType;
                return hashCode;
            }
        }
    }
}
