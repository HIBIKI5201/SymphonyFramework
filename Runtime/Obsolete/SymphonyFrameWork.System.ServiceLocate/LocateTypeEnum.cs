namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     6.x のインスタンスの登録方式。
    /// </summary>
    /// <remarks>
    ///     7.0.0 の移行期間だけ残す。8.0.0 で削除する。
    ///     値の順序は <see cref="SymphonyFramework.Services.ServiceLifetimeEnum" /> と同じ。
    /// </remarks>
    [global::System.Obsolete("LocateTypeEnum は ServiceLifetimeEnum（名前空間 SymphonyFramework.Services）へ改名しました。Singleton は Persistent、Locator は Scene です。8.0.0 で削除します。")]
    public enum LocateTypeEnum : byte
    {
        #region 外部向けAPI

        /// <summary> <see cref="SymphonyFramework.Services.ServiceLifetimeEnum.Persistent" /> と同じ。 </summary>
        Singleton,

        /// <summary> <see cref="SymphonyFramework.Services.ServiceLifetimeEnum.Scene" /> と同じ。 </summary>
        Locator

        #endregion
    }
}
