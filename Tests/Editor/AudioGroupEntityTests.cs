using System;

using NUnit.Framework;

using SymphonyFrameWork.System;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     AudioGroupEntityの音量変換規則を検証する。
    ///     AudioMixerGroupとAudioSourceには触れないため、いずれもnullで生成してよい。
    /// </summary>
    public sealed class AudioGroupEntityTests
    {
        /// <summary> 初期音量を指定した検証用エントリを作る。 </summary>
        /// <param name="originalVolumeDecibel"> AudioMixerから取得した初期音量。 </param>
        /// <returns> 検証用のエントリ。 </returns>
        private static AudioGroupEntity CreateEntity(float? originalVolumeDecibel) =>
            new("BGM", null, null, "BGMVolume", originalVolumeDecibel);

        /// <summary> 生成時の値をそのまま公開する。 </summary>
        [Test]
        public void Constructor_ExposesValues()
        {
            AudioGroupEntity entity = CreateEntity(-6f);

            Assert.That(entity.GroupName, Is.EqualTo("BGM"));
            Assert.That(entity.ExposedVolumeParameterName, Is.EqualTo("BGMVolume"));
            Assert.That(entity.OriginalVolumeDecibel, Is.EqualTo(-6f));
        }

        /// <summary> グループ名を省略した生成は拒否される。 </summary>
        [Test]
        public void Constructor_EmptyGroupName_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new AudioGroupEntity(null, null, null, "BGMVolume", 0f));
            Assert.Throws<ArgumentException>(
                () => new AudioGroupEntity(string.Empty, null, null, "BGMVolume", 0f));
        }

        /// <summary> 割合1は初期音量そのものになる。 </summary>
        [Test]
        public void TryGetVolumeDecibel_FullRatio_ReturnsOriginalVolume()
        {
            AudioGroupEntity entity = CreateEntity(-6f);

            Assert.That(entity.TryGetVolumeDecibel(1f, out float decibel), Is.True);
            Assert.That(decibel, Is.EqualTo(-6f).Within(0.0001f));
        }

        /// <summary> 割合0は最小音量になる。 </summary>
        [Test]
        public void TryGetVolumeDecibel_ZeroRatio_ReturnsMinimum()
        {
            AudioGroupEntity entity = CreateEntity(-6f);

            Assert.That(entity.TryGetVolumeDecibel(0f, out float decibel), Is.True);
            Assert.That(
                decibel,
                Is.EqualTo(AudioGroupEntity.MINIMUM_VOLUME_DECIBEL).Within(0.0001f));
        }

        /// <summary> 中間の割合は最小音量と初期音量の線形補間になる。 </summary>
        [Test]
        public void TryGetVolumeDecibel_HalfRatio_IsLinearlyInterpolated()
        {
            AudioGroupEntity entity = CreateEntity(0f);

            Assert.That(entity.TryGetVolumeDecibel(0.5f, out float decibel), Is.True);
            Assert.That(decibel, Is.EqualTo(-40f).Within(0.0001f), "0dBの半分は-40dB。");
        }

        /// <summary>
        ///     **公開パラメーターが見つからなかったグループは変換できない。**
        ///     旧実装は初期音量へ0を代入していたため、この分岐が到達不能だった。
        /// </summary>
        [Test]
        public void TryGetVolumeDecibel_NoOriginalVolume_ReturnsFalse()
        {
            AudioGroupEntity entity = CreateEntity(null);

            Assert.That(entity.TryGetVolumeDecibel(0.5f, out float decibel), Is.False);
            Assert.That(decibel, Is.EqualTo(default(float)));
        }
    }
}
