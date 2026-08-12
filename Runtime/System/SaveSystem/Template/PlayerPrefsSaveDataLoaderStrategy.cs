using System;
using System.Threading;
using SymphonyFrameWork.Utility;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     利用側がJSON変換を実装するPlayerPrefs向け抽象Strategyを提供する。
    /// </summary>
    [Serializable]
    public abstract class PlayerPrefsSaveDataLoaderStrategy : SaveDataLoaderStrategy
    {
        #region 内部処理

        /// <summary>
        ///     PlayerPrefsに型固有のキーが存在するか確認する。
        /// </summary>
        protected override bool ExistsCore(Type dataType)
        {
            // 型の完全修飾名をキーとして、異なる名前空間の同名型を区別する。
            return PlayerPrefs.HasKey(GetKey(dataType));
        }

        /// <summary>
        ///     PlayerPrefsから型固有のJSONを読み込む。
        /// </summary>
        protected override Awaitable<string> LoadJsonAsync(Type dataType, CancellationToken token)
        {
            // 基底型が開始前のキャンセルを検査済みのため同期的に読み、空文字列を既定値生成へ渡す。
            return SymphonyAwaitable.FromResult(PlayerPrefs.GetString(GetKey(dataType)));
        }

        /// <summary>
        ///     PlayerPrefsへ型固有のJSONを書き込む。
        /// </summary>
        protected override Awaitable SaveJsonAsync(Type dataType, string json, CancellationToken token)
        {
            // 基底型が開始前のキャンセルを検査済みのため同期的に保存し、完了済みAwaitableを返す。
            PlayerPrefs.SetString(GetKey(dataType), json);
            PlayerPrefs.Save();
            return SymphonyAwaitable.Completed();
        }

        /// <summary>
        ///     PlayerPrefsから型固有の保存値を削除する。
        /// </summary>
        protected override Awaitable DeleteCoreAsync(Type dataType, CancellationToken token)
        {
            // 基底型が開始前のキャンセルを検査済みのため同期的に削除し、次回ロードを既定値生成へ進める。
            PlayerPrefs.DeleteKey(GetKey(dataType));
            PlayerPrefs.Save();
            return SymphonyAwaitable.Completed();
        }

        /// <summary>
        ///     型の完全修飾名からPlayerPrefsキーを生成する。
        /// </summary>
        private static string GetKey(Type dataType) => dataType.FullName;

        #endregion
    }
}
