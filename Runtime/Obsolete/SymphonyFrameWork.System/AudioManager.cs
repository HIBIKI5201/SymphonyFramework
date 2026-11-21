using SymphonyFramework.Audio;

using UnityEngine;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     6.x の <c>AudioManager</c> を <see cref="Sonority" /> へ転送する。
    /// </summary>
    /// <remarks> 7.0.0 の移行期間だけ残す。8.0.0 で削除する。 </remarks>
    [global::System.Obsolete("AudioManager は Sonority（名前空間 SymphonyFramework.Audio）へ改名しました。VolumeSliderChanged は SetVolume、GetAudioSource は TryGetSource / GetSource です。8.0.0 で削除します。")]
    public static class AudioManager
    {
        #region 外部向けAPI

        /// <summary> 旧 <c>VolumeSliderChanged</c>。<see cref="Sonority.SetVolume" /> へ転送する。 </summary>
        public static void VolumeSliderChanged(string name, float value) => Sonority.SetVolume(name, value);

        /// <summary> 旧 <c>GetAudioSource</c>。 </summary>
        /// <remarks> 6.x と同じく、Config に無いグループ名では null を返し、空の名前では例外を投げる。 </remarks>
        public static AudioSource GetAudioSource(string name)
        {
            // 未初期化の例外を名前の検証より先に出す 6.x の順序に合わせ、先に転送先へ問い合わせる。
            bool found = Sonority.TryGetSource(name, out AudioSource source);

            // 6.x は空の名前を、未登録グループと区別して呼び出し誤りとして扱っていた。
            if (string.IsNullOrEmpty(name))
            {
                throw new global::System.ArgumentException("オーディオグループ名を指定してください。", nameof(name));
            }

            return found ? source : null;
        }

        #endregion
    }
}
