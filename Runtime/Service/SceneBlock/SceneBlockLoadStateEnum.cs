namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockのロードおよびアンロード状態を表す。
    /// </summary>
    public enum SceneBlockLoadStateEnum : int
    {
        #region 外部向けAPI

        /// <summary> 追跡されていない状態。 </summary>
        None = -1,

        /// <summary> ブロックのシーンをロードしている状態。 </summary>
        Loading = 0,

        /// <summary> ブロックの全シーンのロードが完了した状態。 </summary>
        Complete = 1,

        /// <summary> ブロックのシーンをアンロードしている状態。 </summary>
        Unloading = 2

        #endregion
    }
}
