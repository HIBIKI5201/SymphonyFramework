using SymphonyFrameWork.System.ServiceLocate;
using UnityEngine;

namespace SymphonyFrameWork.Samples.ServiceLocatorSample
{
    public class ServiceLocatorSample_1 : MonoBehaviour
    {
        private void OnEnable()
        {
            if (ServiceLocator.IsExistInstance<ServiceLocatorSample_1>())
            {
                Debug.Log("ServiceLocatorSample_1 instance already exists.");
                return;
            }

            ServiceLocator.RegisterInstance(this, LocateType.Singleton);
        }
    }
}
