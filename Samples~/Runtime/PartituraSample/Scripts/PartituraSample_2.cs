using SymphonyFramework.Services;
using System;
using UnityEngine;

namespace SymphonyFramework.Samples.PartituraSample
{
    /// <summary> 遅延後に自身をSceneの寿命で登録する非同期サンプル。 </summary>
    public sealed class PartituraSample_2 : MonoBehaviour
    {
        /// <summary> GameObjectの破棄に追従する待機後、自身をPartituraへ登録する。 </summary>
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

            Partitura.Register(this);
        }
    }
}
