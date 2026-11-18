namespace SymphonyFramework.Services
{
    /// <summary>
    ///     インスタンスの登録方式（寿命の扱い）を定義する。
    /// </summary>
    /// <remarks> 値の順序は 6.x の <c>LocateTypeEnum</c>（Singleton、Locator）と同じに保つ。 </remarks>
    public enum ServiceLifetimeEnum : byte
    {
        #region 外部向けAPI

        /// <summary> 永続化して登録する。 </summary>
        /// <remarks> Componentの場合はPartituraのGameObjectの子へ移し、シーンを跨いで残す。 </remarks>
        Persistent,

        /// <summary> 親子関係を変更せずに登録する。 </summary>
        /// <remarks> Componentの場合は元のシーンに残り、そのシーンと一緒に破棄される。 </remarks>
        Scene

        #endregion
    }
}
