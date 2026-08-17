using System;
using Newtonsoft.Json;
using SymphonyFrameWork.Attribute;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     保存日時を含むセーブデータの基底型を提供する。
    /// </summary>
    [Serializable]
    public abstract class SaveDataContent : IDisposable
    {
        #region 外部向けAPI

        /// <summary> ISO 8601形式で記録された最終保存日時。 </summary>
        [ReadOnly]
        public string SaveDate;

        /// <summary>
        ///     セーブデータが保持する基底状態を破棄する。
        /// </summary>
        public virtual void Dispose()
        {
            // 破棄後のインスタンスを未保存状態へ戻すため、基底型が管理する日時を消去する。
            SaveDate = null;
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     最終保存日時を更新する。
        /// </summary>
        /// <remarks>
        ///     保存日時は<see cref="SaveDataLoaderStrategy"/>が管理し、利用側からは更新できない。
        /// </remarks>
        /// <param name="saveDate"> 記録する日時。既定値の場合は現在日時。 </param>
        internal void UpdateSaveDate(DateTime saveDate = default)
        {
            // 呼び出し側が日時を指定しない通常保存では、書き込み開始時点のローカル日時を記録する。
            if (saveDate == default) { saveDate = DateTime.Now; }

            // 保存形式間で同じ精度とタイムゾーン情報を維持するため、ISO 8601のラウンドトリップ形式を使う。
            SaveDate = saveDate.ToString("O");
        }

        /// <summary>
        ///     記録されている保存日時を消去する。
        /// </summary>
        internal void ClearSaveDate()
        {
            // 永続化データが無い既定状態を、保存済みの日時と区別できるようにする。
            SaveDate = null;
        }

        #endregion
    }
}
