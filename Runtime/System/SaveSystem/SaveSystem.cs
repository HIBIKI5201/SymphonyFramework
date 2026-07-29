using System;
using System.Threading;
using Api = SymphonyFrameWork.System.API;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     CoreSystemが所有するライフタイムにSaveDataRegistryを連動させます。
    /// </summary>
    internal static class SaveSystem
    {
        /// <summary> セーブデータレジストリをシステムのライフタイムへ関連付ける。 </summary>
        internal static void Initialize(
            CancellationToken destroyCancellationToken,
            Func<SaveDataLoader> loaderResolver)
        {
            _destroyRegistration.Dispose();
            Api.SaveDataRegistry.ConfigureLoaderResolver(loaderResolver);
            _destroyRegistration = destroyCancellationToken.Register(Api.SaveDataRegistry.ResetRuntimeState);
        }

        private static CancellationTokenRegistration _destroyRegistration;
    }
}
