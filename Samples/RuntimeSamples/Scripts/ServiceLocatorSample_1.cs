using SymphonyFrameWork.System.ServiceLocate;
using UnityEngine;

namespace SymphonyFrameWork.Samples.ServiceLocatorSample
{
    public class ServiceLocatorSample_1 : MonoBehaviour
    {
        private Camera _camera;

        private void Start()
        {
            ServiceLocator.RegisterInstance(this, LocateType.Singleton);

            _camera = ServiceLocator.GetInstance<Camera>();
        }
    }
}
