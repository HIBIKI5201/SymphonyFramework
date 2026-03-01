namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     登録するインスタンスの種類を定義します。
    /// </summary>
    public enum LocateType : byte
    {
        /// <summary>
        ///     通常のシングルトンとして登録します。
        ///     Componentの場合、ServiceLocatorのGameObjectの子オブジェクトになります。
        /// </summary>
        Singleton,
        /// <summary>
        ///     インスタンスをServiceLocatorに登録しますが、親子関係は設定しません。
        /// </summary>
        Locator
    }
}
