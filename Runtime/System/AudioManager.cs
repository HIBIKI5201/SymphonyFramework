using SymphonyFrameWork.Config;
using SymphonyFrameWork.Debugger;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace SymphonyFrameWork.System
{
    /// <summary>
    /// オーディオ再生
    /// </summary>
    public static class AudioManager
    {
        private static AudioManagerConfig _config;
        private static GameObject _instance;

        private static
            Dictionary<string, AudioSettingData> _audioDict = new();

        private struct AudioSettingData
        {
            public readonly AudioMixerGroup Group;
            public readonly AudioSource Source;
            public readonly string ExposedName;
            public readonly float? OriginalVolume;

            public AudioSettingData(AudioMixerGroup group, AudioSource source, string exposedName, float? originalVolume)
            {
                Group = group;
                Source = source;
                ExposedName = exposedName;
                OriginalVolume = originalVolume;
            }
        }

        internal static void Initialize()
        {
            _instance = null;
            _audioDict = null;
            _config = SymphonyConfigLocator.GetConfig<AudioManagerConfig>();
        }

        private static void CreateInstance()
        {
            if (_instance is not null) return;

            var instance = new GameObject(nameof(AudioManager));

            SymphonyCoreSystem.MoveObjectToSymphonySystem(instance);
            _instance = instance;
        }

        private static void AudioSourceInitialize()
        {
            if (_audioDict != null)
            {
                return;
            }

            _audioDict = new();

            CreateInstance();

            AudioMixer mixer = _config?.AudioMixer;

            if (!mixer)
            {
                Debug.LogWarning("オーディオミキサーがアサインされていません");
                return;
            }

            SymphonyDebugLogger.AddText("Audio Managerを初期化しました。");

            foreach (string name in _config.AudioGroupSettingList.Select(s => s.AudioGroupName))


            {
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                //Enum名からデータを取得
                var data = _config.AudioGroupSettingList.Find(s => s.AudioGroupName == name);

                if (data == null)
                {
                    Debug.LogWarning($"{name}のデータがありません。");
                    continue;
                }

                //ミキサーグループを取得する
                AudioMixerGroup group = mixer.FindMatchingGroups(name).FirstOrDefault();
                if (group)
                {
                    AudioSource source = _instance.AddComponent<AudioSource>();
                    source.outputAudioMixerGroup = group;
                    source.playOnAwake = false;
                    if (data.IsLoop) source.loop = true;

                    //初期のボリュームを取得
                    float? volume = null;
                    if (!string.IsNullOrEmpty(data.ExposedParameterName) &&
                        mixer.GetFloat(data.ExposedParameterName, out var value))
                    {
                        volume = value;
                        SymphonyDebugLogger.AddText($"{name}は正常に追加されました。volume : {volume}");
                    }
                    else
                    {
                        SymphonyDebugLogger.AddText($"{name}のVolumeParameterが見つかりませんでした");
                    }

                    //各情報を追加
                    _audioDict.Add(name, new AudioSettingData(group, source, data.ExposedParameterName, volume ?? 0));
                }
                else
                {
                    SymphonyDebugLogger.AddText($"{name} is not a valid AudioMixerGroup.");
                }
            }

            SymphonyDebugLogger.TextLog();
        }

        /// <summary>
        ///     指定したミキサーの音量を割合で変更する
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value">割合(0~1)</param>
        public static void VolumeSliderChanged(string name, float value)
        {
            AudioSourceInitialize();

            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (value < 0 || 1 < value)
            {
                Debug.LogWarning("入力は無効な値です");
                return;
            }

            if (!_audioDict.TryGetValue(name, out var data)) return;

            if (data.OriginalVolume == null)
            {
                Debug.LogWarning($"{name}のボリュームがありません");
                return;
            }

            //デシベルで音量を割合変更
            float db = value * (data.OriginalVolume.Value + 80) - 80;

            _config?.AudioMixer.SetFloat(data.ExposedName, db);
        }

        /// <summary>
        ///     指定されたAudioSourceを取得する
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static AudioSource GetAudioSource(string name)
        {
            AudioSourceInitialize();

            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return _audioDict.TryGetValue(name, out var data) ? data.Source : null;
        }
    }
}
