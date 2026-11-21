namespace SymphonyFramework.Samples.FermataSample
{
    /// <summary>
    ///     UI演出カテゴリーに属する往復移動オブジェクト。
    /// </summary>
    /// <remarks>
    ///     ゲームプレイを止めてもこちらは動き続けることを見せるために置く。
    ///     <c>Fermata.SetPause&lt;IFermataSample_UiPausable&gt;(true)</c> で止まる。
    /// </remarks>
    public sealed class FermataSample_UiMover
        : FermataSample_MoverBase, IFermataSample_UiPausable
    {
    }
}
