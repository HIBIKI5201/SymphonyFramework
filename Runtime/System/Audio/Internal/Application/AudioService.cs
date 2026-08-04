using System;
using System.Collections.Generic;
using System.Linq;

using SymphonyFrameWork.Debugger.Logger;

using UnityEngine;
using UnityEngine.Audio;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     Configの解釈、AudioSourceの遅延構築、音量のAudioMixerへの反映を担当する。
    ///     グループの保持は<see cref="AudioGroupRegistry"/>、AudioSourceを載せる
    ///     オブジェクトの寿命は<see cref="IAudioSourceHost"/>へ委譲する。
    /// </summary>
    internal sealed class AudioService
    {
        /// <summary>
        ///     Config、グループの保持先、AudioSourceの生成先を指定して生成する。
        /// </summary>
        /// <param name="config"> オーディオミキサーとグループ設定。 </param>
        /// <param name="registry"> グループを所有するレジストリ。 </param>
        /// <param name="audioSourceHost"> AudioSourceの生成と解放を担う所有者。 </param>
        internal AudioService(
            AudioConfig config,
            AudioGroupRegistry registry,
            IAudioSourceHost audioSourceHost)
        {
            _config = config;
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _audioSourceHost = audioSourceHost ?? throw new ArgumentNullException(nameof(audioSourceHost));
        }

        /// <summary>
        ///     指定グループのAudioSourceを取得する。
        /// </summary>
        /// <param name="groupName"> Configに登録されたオーディオグループ名。 </param>
        /// <returns> 対応するAudioSource。未登録の場合はnull。 </returns>
        internal AudioSource GetAudioSource(string groupName)
        {
            EnsureGroupsBuilt();

            return _registry.TryGet(groupName, out AudioGroupEntity entity)
                ? entity.Source
                : null;
        }

        /// <summary>
        ///     指定グループの音量を割合で変更する。
        /// </summary>
        /// <param name="groupName"> Configに登録されたオーディオグループ名。 </param>
        /// <param name="ratio"> 0から1までの音量割合。 </param>
        internal void SetVolumeRatio(string groupName, float ratio)
        {
            EnsureGroupsBuilt();

            if (!_registry.TryGet(groupName, out AudioGroupEntity entity))
            {
                return;
            }

            if (!entity.TryGetVolumeDecibel(ratio, out float decibel))
            {
                Debug.LogWarning($"{groupName}のボリュームがありません");
                return;
            }

            _config?.AudioMixer.SetFloat(entity.ExposedVolumeParameterName, decibel);
        }

        /// <summary> 生成済みAudioSourceと登録を解放する。 </summary>
        internal void Reset()
        {
            _audioSourceHost.Release();
            _registry.Clear();
        }

        /// <summary>
        ///     Configに定義されたミキサーグループごとのAudioSourceを一度だけ構築する。
        ///     AudioMixerが未割り当ての場合も構築済みとして記録し、警告を繰り返さない。
        /// </summary>
        private void EnsureGroupsBuilt()
        {
            if (_registry.IsBuilt)
            {
                return;
            }

            _registry.MarkBuilt();

            AudioMixer mixer = _config?.AudioMixer;

            if (!mixer)
            {
                Debug.LogWarning("オーディオミキサーがアサインされていません");
                return;
            }

            SymphonyDebugLogger.AddText("Audio Managerを初期化しました。");

            foreach (AudioConfig.AudioGroupConfig settings in GetGroupSettings())
            {
                BuildGroup(mixer, settings);
            }

            SymphonyDebugLogger.LogText();
        }

        /// <summary> Configから有効なグループ設定を列挙する。 </summary>
        /// <returns> グループ名が設定されている設定の一覧。 </returns>
        private IEnumerable<AudioConfig.AudioGroupConfig> GetGroupSettings()
        {
            return _config.AudioGroupSettingList
                .Where(settings => settings != null
                    && !string.IsNullOrEmpty(settings.AudioGroupName));
        }

        /// <summary>
        ///     1グループ分のAudioSourceを生成してレジストリへ登録する。
        /// </summary>
        /// <param name="mixer"> 対象のAudioMixer。 </param>
        /// <param name="settings"> グループの再生設定。 </param>
        private void BuildGroup(AudioMixer mixer, AudioConfig.AudioGroupConfig settings)
        {
            string groupName = settings.AudioGroupName;

            AudioMixerGroup group = mixer.FindMatchingGroups(groupName).FirstOrDefault();
            if (!group)
            {
                SymphonyDebugLogger.AddText($"{groupName} is not a valid AudioMixerGroup.");
                return;
            }

            AudioSource source = _audioSourceHost.CreateAudioSource();
            source.outputAudioMixerGroup = group;
            source.playOnAwake = false;
            if (settings.IsLoop) source.loop = true;

            // 初期音量が取得できない場合はnullのまま保持する。
            // 旧実装は `volume ?? 0` としており、そのせいで「ボリュームがありません」の
            // 分岐が到達不能になっていた。
            float? volume = ResolveOriginalVolume(mixer, settings, groupName);

            _registry.TryAdd(new AudioGroupEntity(
                groupName,
                group,
                source,
                settings.ExposedVolumeParameterName,
                volume));
        }

        /// <summary>
        ///     公開パラメーターから初期音量を取得する。
        /// </summary>
        /// <param name="mixer"> 対象のAudioMixer。 </param>
        /// <param name="settings"> グループの再生設定。 </param>
        /// <param name="groupName"> ログへ出すグループ名。 </param>
        /// <returns> 取得できた初期音量。取得できない場合はnull。 </returns>
        private static float? ResolveOriginalVolume(
            AudioMixer mixer,
            AudioConfig.AudioGroupConfig settings,
            string groupName)
        {
            if (!string.IsNullOrEmpty(settings.ExposedVolumeParameterName)
                && mixer.GetFloat(settings.ExposedVolumeParameterName, out float value))
            {
                SymphonyDebugLogger.AddText($"{groupName}は正常に追加されました。volume : {value}");
                return value;
            }

            SymphonyDebugLogger.AddText($"{groupName}のVolumeParameterが見つかりませんでした");
            return null;
        }

        private readonly AudioConfig _config;
        private readonly AudioGroupRegistry _registry;
        private readonly IAudioSourceHost _audioSourceHost;
    }
}
