using SymphonyFrameWork.System.ServiceLocate;
using UnityEngine;

namespace SymphonyFrameWork.Samples.ServiceLocatorSample
{
    /// <summary> 有効化時に自身をSingletonとして登録するService Locatorサンプル。 </summary>
    public sealed class ServiceLocatorSample_1 : MonoBehaviour
    {
        /// <summary> 重複したSingleton候補の自動破棄を明示して自身を登録する。 </summary>
        private void OnEnable()
        {
            ServiceLocator.RegisterInstanceWithAutoDispose(
                this,
                LocateType.Singleton);
        }
    }
}
