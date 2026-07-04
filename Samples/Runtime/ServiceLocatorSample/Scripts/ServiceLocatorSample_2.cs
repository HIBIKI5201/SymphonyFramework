using SymphonyFrameWork.System.ServiceLocate;
using System;
using UnityEngine;

namespace SymphonyFrameWork.Samples.ServiceLocatorSample
{
    public class ServiceLocatorSample_2 : MonoBehaviour
    {
        private async void OnEnable()
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(5f, destroyCancellationToken);
            }
            catch (Exception)
            {
                Debug.Log($"{gameObject.name}'s register is canceled");
                return;
            }

            ServiceLocator.RegisterInstance(this);
        }
    }
}
