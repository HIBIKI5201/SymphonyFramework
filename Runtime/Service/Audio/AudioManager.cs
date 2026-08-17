using System;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Exceptions;

using UnityEngine;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     オーディオ再生APIを提供する。
    /// </summary>
    /// <remarks> 引数を検証して<see cref="AudioService"/>へ処理を委譲する。 </remarks>
    public static class AudioManager
    {
        #region 外部向けAPI

        /// <summary>
        ///     指定したミキサーグループの音量を割合で変更する。
        /// </summary>
        /// <param name="name"> Configに登録されたオーディオグループ名。 </param>
        /// <param name="value"> 0から1までの音量割合。 </param>
        public static void VolumeSliderChanged(string name, float value)
        {
            AudioService service = EnsureInitialized();

            // Configの検索に使えない名前は、未登録グループと区別するため呼び出し誤りとして扱う。
            if (string.IsNullOrEmpty(name)) { throw new ArgumentException("オーディオグループ名を指定してください。", nameof(name)); }

            // AudioMixerへ渡すdB値へ変換できる0から1の割合だけを受け付ける。
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

            // Configの検索に使えない名前は、未登録グループと区別するため呼び出し誤りとして扱う。
            if (string.IsNullOrEmpty(name)) { throw new ArgumentException("オーディオグループ名を指定してください。", nameof(name)); }

            return service.GetAudioSource(name);
        }

        #endregion

        #region 内部処理

        private static AudioService _service;

        /// <summary>
        ///     Compositionから内部レイヤーが結合済みであることを確認する。
        /// </summary>
        /// <returns> 処理を委譲するService。 </returns>
        private static AudioService EnsureInitialized()
        {
            return _service ?? throw new SymphonyNotInitializedException(typeof(AudioManager));
        }

        /// <summary> Audio Managerが初期化済みかどうか。 </summary>
        internal static bool IsInitialized => _service != null;

        /// <summary>
        ///     Configを受け取り、内部レイヤーを結合する。
        /// </summary>
        /// <param name="config"> オーディオミキサーとグループ設定。 </param>
        /// <param name="systemObjectFactory"> AudioSource所有用GameObjectの生成契約。 </param>
        internal static void Initialize(
            AudioConfig config,
            ISystemObjectFactory systemObjectFactory)
        {
            // Domain Reloadなしで再初期化されても、前回のAudioSourceと参照を残さない。
            ResetRuntimeState();

            // AudioMixer設定、グループ状態、Unityオブジェクトの寿命を各内部層へ分離して結合する。
            _service = new AudioService(
                config,
                new AudioGroupRegistry(),
                new AudioSourceHost(systemObjectFactory));
        }

        /// <summary>
        ///     生成済みAudioSourceとランタイム状態を解放する。
        /// </summary>
        internal static void ResetRuntimeState()
        {
            // 生成済みのUnityオブジェクトを先に解放してから、Compositionの参照を破棄する。
            _service?.Reset();
            _service = null;
        }

        #endregion
    }
}
