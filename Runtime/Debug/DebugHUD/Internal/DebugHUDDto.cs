using System;

namespace SymphonyFrameWork.Debugger.HUD
{
    /// <summary> Debug HUDの表示用更新値を表す。 </summary>
    internal readonly struct DebugHUDDto : IEquatable<DebugHUDDto>
    {
        #region 外部向けAPI

        /// <summary> 表示に必要な値を指定して生成する。 </summary>
        internal DebugHUDDto(
            bool isInitialized,
            bool isAvailable,
            bool isVisible,
            int registeredTextCount)
        {
            IsInitialized = isInitialized;
            IsAvailable = isAvailable;
            IsVisible = isVisible;
            RegisteredTextCount = registeredTextCount;
        }

        /// <summary> Debug HUDが初期化済みの場合はtrue。 </summary>
        internal bool IsInitialized { get; }

        /// <summary> EditorまたはDevelopment Buildで利用可能な場合はtrue。 </summary>
        internal bool IsAvailable { get; }

        /// <summary> HUDが表示中の場合はtrue。 </summary>
        internal bool IsVisible { get; }

        /// <summary> 現在登録されている追加テキストの件数。 </summary>
        internal int RegisteredTextCount { get; }

        /// <inheritdoc />
        public bool Equals(DebugHUDDto other) =>
            IsInitialized == other.IsInitialized
            && IsAvailable == other.IsAvailable
            && IsVisible == other.IsVisible
            && RegisteredTextCount == other.RegisteredTextCount;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is DebugHUDDto other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = IsInitialized.GetHashCode();
                hashCode = (hashCode * 397) ^ IsAvailable.GetHashCode();
                hashCode = (hashCode * 397) ^ IsVisible.GetHashCode();
                hashCode = (hashCode * 397) ^ RegisteredTextCount;
                return hashCode;
            }
        }

        /// <summary> 2つの表示値が等しい場合はtrue。 </summary>
        public static bool operator ==(DebugHUDDto left, DebugHUDDto right) => left.Equals(right);

        /// <summary> 2つの表示値が異なる場合はtrue。 </summary>
        public static bool operator !=(DebugHUDDto left, DebugHUDDto right) => !left.Equals(right);

        #endregion
    }
}
