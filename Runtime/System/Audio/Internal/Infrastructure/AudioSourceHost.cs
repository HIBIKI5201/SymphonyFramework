using System;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Exceptions;

using UnityEngine;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     AudioSourceを載せるシステムオブジェクトを所有する。
    ///     **生成は最初にAudioSourceを求められた時点まで遅らせる。**
    ///     AudioMixerが未割り当てなど、1つも作らない場合はGameObjectも作らない。
    /// </summary>
    internal sealed class AudioSourceHost : IAudioSourceHost
    {
        /// <summary>
        ///     システムオブジェクトの生成契約を指定して生成する。
        /// </summary>
        /// <param name="systemObjectFactory"> シーンをまたぐGameObjectの生成契約。 </param>
        internal AudioSourceHost(ISystemObjectFactory systemObjectFactory)
        {
            _systemObjectFactory = systemObjectFactory
                ?? throw new ArgumentNullException(nameof(systemObjectFactory));
        }

        /// <summary> AudioSourceを1つ生成する。所有オブジェクトは初回だけ生成する。 </summary>
        /// <returns> 生成したAudioSource。 </returns>
        public AudioSource CreateAudioSource()
        {
            EnsureInstance();
            return _instance.AddComponent<AudioSource>();
        }

        /// <summary> 所有オブジェクトを破棄する。多重呼び出しでも安全。 </summary>
        public void Release()
        {
            if (_instance)
            {
                UnityEngine.Object.Destroy(_instance);
            }

            _instance = null;
        }

        /// <summary> 所有オブジェクトが無ければ生成する。 </summary>
        private void EnsureInstance()
        {
            if (_instance)
            {
                return;
            }

            _instance = _systemObjectFactory.CreateObject(nameof(AudioManager));

            if (!_instance)
            {
                throw new SymphonyNotInitializedException(typeof(AudioManager));
            }
        }

        private readonly ISystemObjectFactory _systemObjectFactory;

        private GameObject _instance;
    }
}
