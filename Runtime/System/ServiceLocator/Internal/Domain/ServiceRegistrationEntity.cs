using System;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary> Service Locatorへ登録された1件のサービスと状態を保持する。 </summary>
    internal sealed class ServiceRegistrationEntity
    {
        /// <summary> 登録キー、payload、登録方式を持つ登録済みEntityを生成する。 </summary>
        /// <param name="serviceType"> 登録キーとして使用する型。 </param>
        /// <param name="instance"> 登録されたpayload。 </param>
        /// <param name="locateType"> 登録時に指定された登録方式。 </param>
        internal ServiceRegistrationEntity(
            Type serviceType,
            object instance,
            LocateTypeEnum locateType)
        {
            ServiceType = serviceType
                ?? throw new ArgumentNullException(nameof(serviceType));
            Instance = instance
                ?? throw new ArgumentNullException(nameof(instance));
            LocateType = locateType;
            IsRegistered = true;
        }

        /// <summary> 登録キーとして使用する型。 </summary>
        internal Type ServiceType { get; }

        /// <summary> 登録されたpayload。 </summary>
        internal object Instance { get; }

        /// <summary> 登録時に指定された登録方式。 </summary>
        internal LocateTypeEnum LocateType { get; }

        /// <summary> 現在Registryへ登録されている場合はtrue。 </summary>
        internal bool IsRegistered { get; private set; }

        /// <summary> 登録状態を解除済みへ遷移させる。 </summary>
        /// <returns> この呼び出しで状態が変化した場合はtrue。 </returns>
        internal bool Unregister()
        {
            if (!IsRegistered)
            {
                return false;
            }

            IsRegistered = false;
            return true;
        }
    }
}
