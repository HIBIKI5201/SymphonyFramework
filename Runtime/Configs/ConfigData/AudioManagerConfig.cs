using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SymphonyFrameWork
{
    public class AudioManagerConfig : ScriptableObject
    {
        public AudioMixer AudioMixer => _audioMixer;
        public List<AudioGroupSettings> AudioGroupSettingList => _audioGroupSettingList;

        [SerializeField]
        private AudioMixer _audioMixer;

        [SerializeField]
        private List<AudioGroupSettings> _audioGroupSettingList;

        [Serializable]
        public class AudioGroupSettings
        {
            public string AudioGroupName => _audioGroupName;
            public string ExposedVolumeParameterName => _exposedParameterName;
            public bool IsLoop => _isLoop;

            [SerializeField]
            private string _audioGroupName = string.Empty;

            [SerializeField]
            private string _exposedParameterName = string.Empty;

            [SerializeField]
            private bool _isLoop = false;
        }
    }
}
