using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     Compositionから<see cref="SaveStore" />へ依存を設定する。
    /// </summary>
    internal static class SaveDataInitializer
    {
        /// <summary> Save Storeへローダーの解決処理を設定する。 </summary>
        /// <param name="loaderResolver"> 現在のConfigに対応するローダーを返す処理。 </param>
        internal static void Initialize(Func<SaveDataLoaderStrategy> loaderResolver)
        {
            SaveStore.ConfigureLoaderResolver(loaderResolver);
        }
    }
}
