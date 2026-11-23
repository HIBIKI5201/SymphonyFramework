using System;

using SymphonyFramework.Core;
using SymphonyFramework.Exceptions;

using UnityEngine;

namespace SymphonyFramework.Audio
{
    /// <summary>
    ///     オーディオ再生APIを提供する。
    /// </summary>
    /// <remarks> 引数を検証して<see cref="AudioService"/>へ処理を委譲する。 </remarks>
    public static class Sonority
    {
        #region 外部向けAPI

        /// <summary>
        ///     指定したミキサーグループの音量を割合で変更する。
        /// </summary>
        /// <param name="name"> Configに登録されたオーディオグループ名。 </param>
        /// <param name="value"> 0から1までの音量割合。 </param>
        /// <exception cref="ArgumentException"> nameがnullまたは空の場合。 </exception>
        /// <exception cref="ArgumentOutOfRangeException"> valueが0から1の範囲外の場合。 </exception>
        /// <exception cref="SymphonyNotInitializedException"> Sonorityが未初期化の場合。 </exception>
        /// <remarks> Configに無いグループ名は、反映先が無いため何も変更しない。 </remarks>
        public static void SetVolume(string name, float value)
        {
            AudioService service = EnsureInitialized();
            ValidateName(name);

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
        ///     指定したグループのAudioSourceを取得する。
        /// </summary>
        /// <param name="name"> Configに登録されたオーディオグループ名。 </param>
        /// <returns> 対応するAudioSource。 </returns>
        /// <exception cref="ArgumentException"> nameがnullまたは空、またはConfigに無いグループ名の場合。 </exception>
        /// <exception cref="SymphonyNotInitializedException"> Sonorityが未初期化の場合。 </exception>
        /// <remarks> グループが無いかもしれない場合は <see cref="TryGetSource" /> を使う。 </remarks>
        public static AudioSource GetSource(string name)
        {
            AudioService service = EnsureInitialized();
            ValidateName(name);

            // 必須取得のため、未登録をnullで返さず呼び出し側へ設定漏れとして知らせる。
            AudioSource source = service.GetAudioSource(name);
            if (source == null)
            {
                throw new ArgumentException(
                    $"オーディオグループ {name} はConfigに登録されていません。",
                    nameof(name));
            }

            return source;
        }

        /// <summary>
        ///     指定したグループのAudioSourceの取得を試みる。
        /// </summary>
        /// <param name="name"> Configに登録されたオーディオグループ名。 </param>
        /// <param name="source"> 取得できたAudioSource。取得できない場合はnull。 </param>
        /// <returns> 取得できた場合はtrue。nameがnullまたは空、Configに無いグループ名の場合はfalse。 </returns>
        /// <exception cref="SymphonyNotInitializedException"> Sonorityが未初期化の場合。 </exception>
        public static bool TryGetSource(string name, out AudioSource source)
        {
            AudioService service = EnsureInitialized();

            // 入力欄の途中の値などを渡しても落ちないよう、取得できない名前はfalseで返す。
            source = string.IsNullOrEmpty(name) ? null : service.GetAudioSource(name);
            return source != null;
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
            return _service ?? throw new SymphonyNotInitializedException(typeof(Sonority));
        }

        /// <summary>
        ///     Configの検索に使える名前か検証する。
        /// </summary>
        /// <param name="name"> 検証するオーディオグループ名。 </param>
        /// <exception cref="ArgumentException"> nameがnullまたは空の場合。 </exception>
        private static void ValidateName(string name)
        {
            // Configの検索に使えない名前は、未登録グループと区別するため呼び出し誤りとして扱う。
            if (string.IsNullOrEmpty(name)) { throw new ArgumentException("オーディオグループ名を指定してください。", nameof(name)); }
        }

        /// <summary> Sonorityが初期化済みかどうか。 </summary>
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
