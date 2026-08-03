using System;

using UnityEngine;
using UnityEngine.Audio;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     AudioMixerグループ単位の再生設定。
    ///     グループ名を同一性とし、再生元と音量変換の規則を保持する。
    ///     GameObjectの生成や破棄には関与しない。
    /// </summary>
    internal sealed class AudioGroupEntity
    {
        /// <summary>
        ///     AudioMixerが扱える最小音量。
        ///     割合0を指定したときのデシベル値であり、Unityの仕様に由来する。
        /// </summary>
        internal const float MINIMUM_VOLUME_DECIBEL = -80f;

        /// <summary>
        ///     グループ名と再生設定を指定して生成する。
        /// </summary>
        /// <param name="groupName"> AudioMixer内のグループ名。 </param>
        /// <param name="group"> 対応するAudioMixerGroup。 </param>
        /// <param name="source"> グループの再生を担当するAudioSource。 </param>
        /// <param name="exposedVolumeParameterName"> 音量制御に使用する公開パラメーター名。 </param>
        /// <param name="originalVolumeDecibel">
        ///     AudioMixerから取得した初期音量。公開パラメーターが無い場合はnull。
        /// </param>
        internal AudioGroupEntity(
            string groupName,
            AudioMixerGroup group,
            AudioSource source,
            string exposedVolumeParameterName,
            float? originalVolumeDecibel)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException("オーディオグループ名を指定してください。", nameof(groupName));
            }

            GroupName = groupName;
            Group = group;
            Source = source;
            ExposedVolumeParameterName = exposedVolumeParameterName;
            OriginalVolumeDecibel = originalVolumeDecibel;
        }

        /// <summary> このエントリの同一性を表すグループ名。 </summary>
        internal string GroupName { get; }

        /// <summary> 対応するAudioMixerGroup。 </summary>
        internal AudioMixerGroup Group { get; }

        /// <summary> グループの再生を担当するAudioSource。 </summary>
        internal AudioSource Source { get; }

        /// <summary> 音量制御に使用する公開パラメーター名。 </summary>
        internal string ExposedVolumeParameterName { get; }

        /// <summary> AudioMixerから取得した初期音量。公開パラメーターが無い場合はnull。 </summary>
        internal float? OriginalVolumeDecibel { get; }

        /// <summary>
        ///     音量の割合を、AudioMixerへ設定するデシベル値へ変換する。
        ///     割合0で<see cref="MINIMUM_VOLUME_DECIBEL"/>、割合1で初期音量になるよう線形に補間する。
        /// </summary>
        /// <param name="ratio"> 0から1までの音量割合。 </param>
        /// <param name="decibel"> 変換したデシベル値。 </param>
        /// <returns> 変換できた場合はtrue。公開パラメーターが無い場合はfalse。 </returns>
        internal bool TryGetVolumeDecibel(float ratio, out float decibel)
        {
            if (OriginalVolumeDecibel == null)
            {
                decibel = default;
                return false;
            }

            decibel = (ratio * (OriginalVolumeDecibel.Value - MINIMUM_VOLUME_DECIBEL))
                + MINIMUM_VOLUME_DECIBEL;
            return true;
        }
    }
}
