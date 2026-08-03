using UnityEngine;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     AudioSourceの生成と、それを所有するUnityオブジェクトの寿命を扱う契約。
    ///     Applicationがシーンをまたぐオブジェクトの生成・破棄を直接扱わないために置く。
    /// </summary>
    internal interface IAudioSourceHost
    {
        /// <summary>
        ///     AudioSourceを1つ生成する。
        ///     所有するオブジェクトは、最初に呼ばれたときだけ生成する。
        /// </summary>
        /// <returns> 生成したAudioSource。 </returns>
        AudioSource CreateAudioSource();

        /// <summary> 生成済みのAudioSourceと所有オブジェクトを解放する。 </summary>
        void Release();
    }
}
