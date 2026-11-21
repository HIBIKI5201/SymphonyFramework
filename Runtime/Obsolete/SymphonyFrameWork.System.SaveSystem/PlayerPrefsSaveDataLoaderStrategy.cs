namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     6.x の PlayerPrefs を使う読み書き方式の基底型。
    ///     <see cref="SymphonyFramework.SaveData.PlayerPrefsSaveDataLoaderStrategy" /> を継承する。
    /// </summary>
    /// <remarks>
    ///     7.0.0 の移行期間だけ残す。8.0.0 で削除する。
    ///     旧 <see cref="SaveDataLoaderStrategy" /> と同じく、旧 <see cref="SaveDataContent" /> を受け取る抽象メソッドへ橋渡しする。
    ///     6.x と違い、旧 <see cref="SaveDataLoaderStrategy" /> の派生型ではない。
    /// </remarks>
    [global::System.Serializable]
    [global::System.Obsolete("PlayerPrefsSaveDataLoaderStrategy は名前空間 SymphonyFramework.SaveData へ移りました。using を書き換えてください。8.0.0 で削除します。")]
    public abstract class PlayerPrefsSaveDataLoaderStrategy : SymphonyFramework.SaveData.PlayerPrefsSaveDataLoaderStrategy
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
