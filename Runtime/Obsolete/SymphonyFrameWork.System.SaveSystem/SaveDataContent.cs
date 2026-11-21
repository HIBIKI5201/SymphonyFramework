namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     6.x のセーブデータの基底型。<see cref="SymphonyFramework.SaveData.SaveDataContent" /> を継承する。
    /// </summary>
    /// <remarks>
    ///     7.0.0 の移行期間だけ残す。8.0.0 で削除する。
    ///     保存キーは利用側のデータ型の名前なので、基底型を書き換えても既存のセーブデータはそのまま読める。
    /// </remarks>
    [global::System.Serializable]
    [global::System.Obsolete("SaveDataContent は名前空間 SymphonyFramework.SaveData へ移りました。using を書き換えてください。8.0.0 で削除します。")]
    public abstract class SaveDataContent : SymphonyFramework.SaveData.SaveDataContent
    {
    }
}
