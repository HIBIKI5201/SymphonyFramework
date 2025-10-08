using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SymphonyFrameWork.Config
{
    /// <summary>
    ///     オーディオマネージャーの設定データを保持するクラス。
    /// </summary>
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
            public string AudioGroupName => _audioGroupName;
            public string ExposedVolumeParameterName => _exposedVolumeParameterName;
            public bool IsLoop => _isLoop;

            [SerializeField, Tooltip("オーディオグループの名前")]
            private string _audioGroupName = string.Empty;

            [SerializeField, Tooltip("ボリュームのパラメータ名")]
            private string _exposedVolumeParameterName = string.Empty;

            [SerializeField, Tooltip("ループの有効化")]
            private bool _isLoop = false;
        }
    }
}
