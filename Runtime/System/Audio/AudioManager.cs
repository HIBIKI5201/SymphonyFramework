using System;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Exceptions;

using UnityEngine;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     オーディオ再生。
    ///     引数の検証と<see cref="AudioService"/>への転送だけを行い、状態は保持しません。
    /// </summary>
    public static class AudioManager
    {
        /// <summary>
        ///     指定したミキサーグループの音量を割合で変更する。
        /// </summary>
        /// <param name="name"> Configに登録されたオーディオグループ名。 </param>
        /// <param name="value"> 0から1までの音量割合。 </param>
        public static void VolumeSliderChanged(string name, float value)
        {
            AudioService service = EnsureInitialized();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("オーディオグループ名を指定してください。", nameof(name));
            }

            if (value < 0 || 1 < value)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "音量割合は0から1の範囲で指定してください。");
            }

            service.SetVolumeRatio(name, value);
        }

        /// <summary>
        ///     指定されたAudioSourceを取得する。
        /// </summary>
        /// <param name="name"> Configに登録されたオーディオグループ名。 </param>
        /// <returns> 対応するAudioSource。未登録の場合はnull。 </returns>
        public static AudioSource GetAudioSource(string name)
        {
            AudioService service = EnsureInitialized();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("オーディオグループ名を指定してください。", nameof(name));
            }

            return service.GetAudioSource(name);
        }

        /// <summary> Audio Managerが初期化済みかどうか。 </summary>
        internal static bool IsInitialized => _service != null;

        /// <summary> Configを受け取り、内部レイヤーを結合する。 </summary>
        /// <param name="config"> オーディオミキサーとグループ設定。 </param>
        /// <param name="systemObjectFactory"> AudioSource所有用GameObjectの生成契約。 </param>
        internal static void Initialize(
            AudioConfig config,
            ISystemObjectFactory systemObjectFactory)
        {
            ResetRuntimeState();

            _service = new AudioService(
                config,
                new AudioGroupRegistry(),
                new AudioSourceHost(systemObjectFactory));
        }

        /// <summary> 生成済みAudioSourceとランタイム状態を解放する。 </summary>
        internal static void ResetRuntimeState()
        {
            _service?.Reset();
            _service = null;
        }

        /// <summary> Compositionから内部レイヤーが結合済みであることを確認する。 </summary>
        /// <returns> 処理を委譲するService。 </returns>
        private static AudioService EnsureInitialized()
        {
            return _service ?? throw new SymphonyNotInitializedException(typeof(AudioManager));
        }

        private static AudioService _service;
    }
}
