using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace SymphonyFrameWork
{
    public class AudioManagerConfig : ScriptableObject
    {
        [SerializeField]
        private AudioMixer _audioMixer;
        public AudioMixer AudioMixer { get => _audioMixer; }

        [SerializeField]
        private List<AudioGroupSettings> _audioGroupSettingList;
        public List<AudioGroupSettings> AudioGroupSettingList { get => _audioGroupSettingList; }

        [Serializable]
        public class AudioGroupSettings
        {
            [SerializeField]
            private string _audioGroupName = string.Empty;
            public string AudioGroupName { get => _audioGroupName; }

            [SerializeField]
            private string _exposedParameterName = string.Empty;
            public string ExposedParameterName { get => _exposedParameterName; }

            [SerializeField]
            private bool _isLoop = false;
            public bool IsLoop { get => _isLoop; }
        }
    }
}
