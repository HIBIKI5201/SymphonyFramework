using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     CompositionからSaveDataRegistryへ依存を設定する。
    /// </summary>
    internal static class SaveSystem
    {
        /// <summary> セーブデータレジストリへローダーの解決処理を設定する。 </summary>
        /// <param name="loaderResolver"> 現在のConfigに対応するローダーを返す処理。 </param>
        internal static void Initialize(Func<SaveDataLoader> loaderResolver)
        {
            SaveStore.ConfigureLoaderResolver(loaderResolver);
        }
    }
}
