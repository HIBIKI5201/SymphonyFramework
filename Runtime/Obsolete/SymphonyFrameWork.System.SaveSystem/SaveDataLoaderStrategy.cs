using System.Threading;

using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     6.x のセーブデータの読み書き方式の基底型。<see cref="SymphonyFramework.SaveData.SaveDataLoaderStrategy" /> を継承する。
    /// </summary>
    /// <remarks>
    ///     7.0.0 の移行期間だけ残す。8.0.0 で削除する。
    ///     6.x の派生型は、引数が旧 <see cref="SaveDataContent" /> の <c>SerializeToJson</c> / <c>OverwriteFromJson</c> を
    ///     override している。その形のまま動くよう、新しい基底の抽象メソッドを旧型の抽象メソッドへ橋渡しする。
    ///     <b>旧 <see cref="SaveDataContent" /> を継承していないデータ型は扱えない</b>（<see cref="global::System.InvalidOperationException" />）。
    /// </remarks>
    [global::System.Serializable]
    [global::System.Obsolete("SaveDataLoaderStrategy は名前空間 SymphonyFramework.SaveData へ移りました。using を書き換えてください。8.0.0 で削除します。")]
    public abstract class SaveDataLoaderStrategy : SymphonyFramework.SaveData.SaveDataLoaderStrategy
    {
        #region 内部処理

        /// <summary>
        ///     6.x の形のセーブデータをJSONへ変換する。
        /// </summary>
        /// <param name="dataType"> セーブデータの型。 </param>
        /// <param name="data"> 変換するセーブデータ。 </param>
        /// <returns> 保存するJSON。 </returns>
        protected abstract string SerializeToJson(global::System.Type dataType, SaveDataContent data);

        /// <summary>
        ///     6.x の形のセーブデータへJSONを上書きする。
        /// </summary>
        /// <param name="dataType"> セーブデータの型。 </param>
        /// <param name="json"> 読み込んだJSON。 </param>
        /// <param name="data"> 上書きするセーブデータ。 </param>
        protected abstract void OverwriteFromJson(global::System.Type dataType, string json, SaveDataContent data);

        /// <inheritdoc />
        protected sealed override string SerializeToJson(
            global::System.Type dataType,
            SymphonyFramework.SaveData.SaveDataContent data) =>
            SerializeToJson(dataType, LegacySaveDataBridge.AsLegacy(dataType, data));

        /// <inheritdoc />
        protected sealed override void OverwriteFromJson(
            global::System.Type dataType,
            string json,
            SymphonyFramework.SaveData.SaveDataContent data) =>
            OverwriteFromJson(dataType, json, LegacySaveDataBridge.AsLegacy(dataType, data));

        #endregion
    }
}
