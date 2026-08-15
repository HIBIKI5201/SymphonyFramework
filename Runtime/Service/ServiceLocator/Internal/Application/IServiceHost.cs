namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     ApplicationからUnity上のサービス所有処理を分離する境界。
    /// </summary>
    internal interface IServiceHost
    {
        #region 外部向けAPI

        /// <summary>
        ///     Singleton登録されたComponentを所有階層へ接続する。
        /// </summary>
        /// <param name="instance"> 登録されたpayload。 </param>
        void Attach(object instance);

        /// <summary>
        ///     登録解除されたComponentを所有階層から切り離す。
        /// </summary>
        /// <param name="instance"> 登録解除されたpayload。 </param>
        void Detach(object instance);

        /// <summary>
        ///     IDisposableまたはComponentとしてpayloadを解放する。
        /// </summary>
        /// <param name="instance"> 解放するpayload。 </param>
        /// <returns> 解放処理を1つ以上実行した場合はtrue。 </returns>
        bool DisposeInstance(object instance);

        #endregion
    }
}
