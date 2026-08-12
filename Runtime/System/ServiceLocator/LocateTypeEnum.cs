namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     インスタンスの登録方式を定義する。
    /// </summary>
    public enum LocateTypeEnum : byte
    {
        #region 外部向けAPI

        /// <summary> Singletonとして登録する。 </summary>
        /// <remarks> Componentの場合はServiceLocatorのGameObjectの子になる。 </remarks>
        Singleton,

        /// <summary> 親子関係を変更せずに登録する。 </summary>
        Locator

        #endregion
    }
}
