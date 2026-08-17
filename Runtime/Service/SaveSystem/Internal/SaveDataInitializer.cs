using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     Compositionから<see cref="SaveStore" />へ依存を設定する。
    /// </summary>
    internal static class SaveDataInitializer
    {
        #region 外部向けAPI

        /// <summary>
        ///     Save Storeへローダーの解決処理を設定する。
        /// </summary>
        /// <param name="loaderResolver"> 現在のConfigに対応するローダーを返す処理。 </param>
        internal static void Initialize(Func<SaveDataLoaderStrategy> loaderResolver)
        {
            // Compositionが選んだConfigを遅延解決できる形でFacadeへ渡す。
            SaveStore.ConfigureLoaderResolver(loaderResolver);
        }

        #endregion
    }
}
