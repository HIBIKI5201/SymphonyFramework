using System;

using UnityEngine;
using UnityEngine.Audio;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     AudioMixerグループ単位の再生設定と音量変換規則を保持する。
    /// </summary>
    /// <remarks> グループ名を同一性とし、GameObjectの生成や破棄には関与しない。 </remarks>
    internal sealed class AudioGroupEntity
    {
        #region 外部向けAPI

        /// <summary> AudioMixerが扱う最小音量のdB値。 </summary>
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
            // AudioMixer内でグループを一意に識別できないEntityは生成させない。
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException("オーディオグループ名を指定してください。", nameof(groupName));
            }

            // Mixerのグループ、Source、公開パラメーター名、初期dB値の対応を固定する。
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
        ///     音量割合をAudioMixerへ設定するdB値へ変換する。
        /// </summary>
        /// <remarks> 割合0を<see cref="MINIMUM_VOLUME_DECIBEL"/>、割合1を初期dB値として線形補間する。 </remarks>
        /// <param name="ratio"> 0から1までの音量割合。 </param>
        /// <param name="decibel"> 変換したデシベル値。 </param>
        /// <returns> 変換できた場合はtrue。公開パラメーターが無い場合はfalse。 </returns>
        internal bool TryGetVolumeDecibel(float ratio, out float decibel)
        {
            // 公開パラメーターの初期dB値を取得できなかったグループは変換できない。
            if (OriginalVolumeDecibel == null)
            {
                decibel = default;
                return false;
            }

            // 公開APIの0-1割合を、AudioMixer.SetFloatが要求するdB単位へ変換する。
            decibel = (ratio * (OriginalVolumeDecibel.Value - MINIMUM_VOLUME_DECIBEL))
                + MINIMUM_VOLUME_DECIBEL;
            return true;
        }

        #endregion
    }
}
