namespace SymphonyFrameWork.Samples.PauseManagerSample
{
    /// <summary>
    ///     UI演出カテゴリーに属する往復移動オブジェクト。
    /// </summary>
    /// <remarks>
    ///     ゲームプレイを止めてもこちらは動き続けることを見せるために置く。
    ///     <c>PauseManager.SetPause&lt;IPauseManagerSample_UiPausable&gt;(true)</c> で止まる。
    /// </remarks>
    public sealed class PauseManagerSample_UiMover
        : PauseManagerSample_MoverBase, IPauseManagerSample_UiPausable
    {
    }
}
