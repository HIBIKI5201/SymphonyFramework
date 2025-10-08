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
        [SerializeField, Tooltip("オーディオミキサー")]
        private AudioMixer _audioMixer;
        /// <summry> オーディオミキサー。 </summry>
        public AudioMixer AudioMixer { get => _audioMixer; }

        [SerializeField, Tooltip("オーディオグループの設定リスト")]
        private List<AudioGroupSettings> _audioGroupSettingList;
        /// <summry> オーディオグループの設定リスト。 </summry>
        public List<AudioGroupSettings> AudioGroupSettingList { get => _audioGroupSettingList; }

        /// <summry> オーディオグループの設定。 </summry>
        [Serializable]
        public class AudioGroupSettings
        {
            /// <summry> オーディオグループの名前。 </summry>
            public string AudioGroupName => _audioGroupName;
            /// <summry> ボリュームのパラメータ名。 </summry>
            public string ExposedVolumeParameterName => _exposedVolumeParameterName;
            /// <summry> ループの有効化。 </summry>
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
