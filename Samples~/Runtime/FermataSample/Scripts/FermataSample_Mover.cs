namespace SymphonyFramework.Samples.FermataSample
{
    /// <summary>
    ///     ゲームプレイカテゴリーに属する往復移動オブジェクト。
    /// </summary>
    /// <remarks>
    ///     **属するカテゴリーはinterfaceの実装で決まる。** 登録の呼び出しは基底クラスと同じで、
    ///     <c>Fermata.SetPause&lt;IFermataSample_GameplayPausable&gt;(true)</c> で止まる。
    /// </remarks>
    public sealed class FermataSample_Mover
        : FermataSample_MoverBase, IFermataSample_GameplayPausable
    {
    }
}
