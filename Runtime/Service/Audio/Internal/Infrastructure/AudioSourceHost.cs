using System;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Exceptions;

using UnityEngine;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     AudioSourceを載せるシステムオブジェクトを所有する。
    /// </summary>
    /// <remarks> AudioSourceが必要になるまでGameObjectの生成を遅らせる。 </remarks>
    internal sealed class AudioSourceHost : IAudioSourceHost
    {
        #region 外部向けAPI

        /// <summary>
        ///     システムオブジェクトの生成契約を指定して生成する。
        /// </summary>
        /// <param name="systemObjectFactory"> シーンをまたぐGameObjectの生成契約。 </param>
        internal AudioSourceHost(ISystemObjectFactory systemObjectFactory)
        {
            // シーンをまたぐ所有オブジェクトはCompositionが提供する生成規則へ統一する。
            _systemObjectFactory = systemObjectFactory
                ?? throw new ArgumentNullException(nameof(systemObjectFactory));
        }

        /// <summary>
        ///     AudioSourceを1つ生成する。
        /// </summary>
        /// <remarks> 所有オブジェクトは最初の呼び出しで生成する。 </remarks>
        /// <returns> 生成したAudioSource。 </returns>
        public AudioSource CreateAudioSource()
        {
            // AudioMixerに有効なグループがある場合だけ、永続オブジェクトを遅延生成する。
            EnsureInstance();
            return _instance.AddComponent<AudioSource>();
        }

        /// <summary>
        ///     所有オブジェクトを破棄する。
        /// </summary>
        /// <remarks> 多重呼び出しを許容する。 </remarks>
        public void Release()
        {
            // Unityオブジェクトが生存している場合だけ破棄し、多重解放を許容する。
            if (_instance) { UnityEngine.Object.Destroy(_instance); }

            _instance = null;
        }

        #endregion

        #region 内部処理

        private readonly ISystemObjectFactory _systemObjectFactory;

        private GameObject _instance;

        /// <summary>
        ///     所有オブジェクトが無ければ生成する。
        /// </summary>
        private void EnsureInstance()
        {
            // 生成済みの永続オブジェクトへAudioSourceを集約する。
            if (_instance) { return; }

            // AudioManager用の名前とDontDestroyOnLoad設定は共通Factoryへ委譲する。
            _instance = _systemObjectFactory.CreateObject(nameof(AudioManager));

            // CompositionがGameObjectを生成できない状態は、後続のAddComponentより前に通知する。
            if (!_instance) { throw new SymphonyNotInitializedException(typeof(AudioManager)); }
        }

        #endregion
    }
}
