using UnityEngine;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     AudioSourceと所有オブジェクトの寿命を扱う。
    /// </summary>
    /// <remarks> ApplicationからシーンをまたぐUnityオブジェクトの生成と破棄を分離する。 </remarks>
    internal interface IAudioSourceHost
    {
        #region 外部向けAPI

        /// <summary>
        ///     AudioSourceを1つ生成する。
        /// </summary>
        /// <remarks> 所有オブジェクトは最初の呼び出しで生成する。 </remarks>
        /// <returns> 生成したAudioSource。 </returns>
        AudioSource CreateAudioSource();

        /// <summary>
        ///     生成済みのAudioSourceと所有オブジェクトを解放する。
        /// </summary>
        void Release();

        #endregion
    }
}
