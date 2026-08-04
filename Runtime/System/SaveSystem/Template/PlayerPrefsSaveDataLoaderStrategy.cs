using System;
using System.Threading;
using SymphonyFrameWork.Utility;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     PlayerPrefsへのJSON入出力を提供するLoader基底クラスです。
    /// </summary>
    [Serializable]
    public abstract class PlayerPrefsSaveDataLoaderStrategy : SaveDataLoaderStrategy
    {
        /// <summary> PlayerPrefsに型固有のキーが存在するか確認する。 </summary>
        protected override bool ExistsCore(Type dataType)
        {
            return PlayerPrefs.HasKey(GetKey(dataType));
        }

        /// <summary> PlayerPrefsから型固有のJSONを読み込む。 </summary>
        protected override Awaitable<string> LoadJsonAsync(Type dataType, CancellationToken token)
        {
            return SymphonyAwaitable.FromResult(PlayerPrefs.GetString(GetKey(dataType)));
        }

        /// <summary> PlayerPrefsへ型固有のJSONを書き込む。 </summary>
        protected override Awaitable SaveJsonAsync(Type dataType, string json, CancellationToken token)
        {
            PlayerPrefs.SetString(GetKey(dataType), json);
            PlayerPrefs.Save();
            return SymphonyAwaitable.Completed();
        }

        /// <summary> PlayerPrefsから型固有の保存値を削除する。 </summary>
        protected override Awaitable DeleteCoreAsync(Type dataType, CancellationToken token)
        {
            PlayerPrefs.DeleteKey(GetKey(dataType));
            PlayerPrefs.Save();
            return SymphonyAwaitable.Completed();
        }

        /// <summary> 型の完全修飾名からPlayerPrefsキーを生成する。 </summary>
        private static string GetKey(Type dataType) => dataType.FullName;
    }
}
