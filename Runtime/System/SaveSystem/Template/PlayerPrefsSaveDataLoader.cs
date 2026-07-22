using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     PlayerPrefsへのJSON入出力を提供するLoader基底クラスです。
    /// </summary>
    [Serializable]
    public abstract class PlayerPrefsSaveDataLoader : SaveDataLoader
    {
        protected override bool ExistsCore(Type dataType)
        {
            return PlayerPrefs.HasKey(GetKey(dataType));
        }

        protected override ValueTask<string> LoadJsonAsync(Type dataType, CancellationToken token)
        {
            return new ValueTask<string>(PlayerPrefs.GetString(GetKey(dataType)));
        }

        protected override ValueTask SaveJsonAsync(Type dataType, string json, CancellationToken token)
        {
            PlayerPrefs.SetString(GetKey(dataType), json);
            PlayerPrefs.Save();
            return default;
        }

        protected override ValueTask DeleteCoreAsync(Type dataType, CancellationToken token)
        {
            PlayerPrefs.DeleteKey(GetKey(dataType));
            PlayerPrefs.Save();
            return default;
        }

        private static string GetKey(Type dataType) => dataType.FullName;
    }
}
