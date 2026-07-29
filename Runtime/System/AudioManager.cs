using System;
using UnityEngine;
using Api = SymphonyFrameWork.System.API;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     <see cref="Api.AudioManager"/> へ移動しました。
    /// </summary>
    [Obsolete("SymphonyFrameWork.System.API.AudioManager に移動しました。今後はそちらを使用してください。", error: false)]
    public static class AudioManager
    {
        /// <summary>
        ///     指定したミキサーグループの音量を割合で変更する。
        /// </summary>
        /// <param name="name"> Configに登録されたオーディオグループ名。 </param>
        /// <param name="value"> 0から1までの音量割合。 </param>
        public static void VolumeSliderChanged(string name, float value)
            => Api.AudioManager.VolumeSliderChanged(name, value);

        /// <summary>
        ///     指定されたAudioSourceを取得する。
        /// </summary>
        /// <param name="name"> Configに登録されたオーディオグループ名。 </param>
        /// <returns> 対応するAudioSource。未登録の場合はnull。 </returns>
        public static AudioSource GetAudioSource(string name)
            => Api.AudioManager.GetAudioSource(name);
    }
}
