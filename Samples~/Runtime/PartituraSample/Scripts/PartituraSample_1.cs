using SymphonyFramework.Services;
using UnityEngine;

namespace SymphonyFramework.Samples.PartituraSample
{
    /// <summary> 有効化時に自身をPersistentの寿命で登録するPartituraサンプル。 </summary>
    public sealed class PartituraSample_1 : MonoBehaviour
    {
        /// <summary> 重複した候補の自動破棄を明示して自身を登録する。 </summary>
        private void OnEnable()
        {
            Partitura.RegisterWithAutoDispose(
                this,
                ServiceLifetimeEnum.Persistent);
        }
    }
}
