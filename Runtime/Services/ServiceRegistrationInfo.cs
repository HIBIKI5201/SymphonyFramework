using System;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Services
{
    /// <summary>
    ///     Partituraへ登録された1件の状態を表す不変なスナップショット。
    /// </summary>
    // 6.x の名前（SymphonyFrameWork.System.ServiceLocate）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.System.ServiceLocate",
        sourceAssembly: "SymphonyFrameWork")]
    public readonly struct ServiceRegistrationInfo : IEquatable<ServiceRegistrationInfo>
    {
        #region 外部向けAPI

        /// <summary>
        ///     Queryが抽出した登録状態からスナップショットを生成する。
        /// </summary>
        /// <param name="serviceType"> 登録キーとして使用された型。 </param>
        /// <param name="instance"> 登録されたpayload。 </param>
        /// <param name="lifetime"> 登録時に指定された登録方式。 </param>
        internal ServiceRegistrationInfo(
            Type serviceType,
            object instance,
            ServiceLifetimeEnum lifetime)
        {
            // 取得時点の登録状態を、後から変更されない値として固定する。
            ServiceType = serviceType;
            Instance = instance;
            Lifetime = lifetime;
        }

        /// <summary> 登録キーとして使用された型。 </summary>
        public Type ServiceType { get; }

        /// <summary> 登録されたpayload。 </summary>
        public object Instance { get; }

        /// <summary> 登録時に指定された登録方式。 </summary>
        public ServiceLifetimeEnum Lifetime { get; }

        /// <summary>
        ///     2つの登録スナップショットが同値か判定する。
        /// </summary>
        /// <param name="left"> 左辺のスナップショット。 </param>
        /// <param name="right"> 右辺のスナップショット。 </param>
        /// <returns> 同値の場合はtrue。 </returns>
        public static bool operator ==(
            ServiceRegistrationInfo left,
            ServiceRegistrationInfo right) =>
            left.Equals(right);

        /// <summary>
        ///     2つの登録スナップショットが異なるか判定する。
        /// </summary>
        /// <param name="left"> 左辺のスナップショット。 </param>
        /// <param name="right"> 右辺のスナップショット。 </param>
        /// <returns> 異なる場合はtrue。 </returns>
        public static bool operator !=(
            ServiceRegistrationInfo left,
            ServiceRegistrationInfo right) =>
            !left.Equals(right);

        /// <summary>
        ///     指定した登録スナップショットと同値か判定する。
        /// </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> 型と登録方式が等しく、payloadが同一参照の場合はtrue。 </returns>
        public bool Equals(ServiceRegistrationInfo other) =>
            ServiceType == other.ServiceType
            && ReferenceEquals(Instance, other.Instance)
            && Lifetime == other.Lifetime;

        /// <summary>
        ///     指定したオブジェクトと同値か判定する。
        /// </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同値のServiceRegistrationInfoの場合はtrue。 </returns>
        public override bool Equals(object obj) =>
            obj is ServiceRegistrationInfo other && Equals(other);

        /// <summary>
        ///     型、payload参照、登録方式に基づくハッシュコードを返す。
        /// </summary>
        /// <returns> ハッシュコード。 </returns>
        public override int GetHashCode()
        {
            // Equalsと同じくpayloadの内容ではなく参照をハッシュへ反映する。
            unchecked
            {
                int hashCode = ServiceType?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397)
                    ^ (Instance != null
                        ? RuntimeHelpers.GetHashCode(Instance)
                        : 0);
                hashCode = (hashCode * 397) ^ (int)Lifetime;
                return hashCode;
            }
        }

        #endregion
    }
}
