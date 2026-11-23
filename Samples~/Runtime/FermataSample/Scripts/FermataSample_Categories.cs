using SymphonyFramework.Pause;

namespace SymphonyFramework.Samples.FermataSample
{
    /// <summary>
    ///     ゲームプレイのポーズカテゴリー。
    /// </summary>
    /// <remarks>
    ///     カテゴリーは<see cref="IPausable"/>を継承した空のinterfaceである。
    ///     このサンプルでは手で書いているが、
    ///     <c>Project Settings > SymphonyFramework > Pause Category</c> から生成もできる。
    /// </remarks>
    public interface IFermataSample_GameplayPausable : IPausable { }

    /// <summary>
    ///     UI演出のポーズカテゴリー。
    /// </summary>
    /// <remarks>
    ///     ゲームプレイを止めたままUIだけ動かし続ける、といった分け方を実演するために置く。
    /// </remarks>
    public interface IFermataSample_UiPausable : IPausable { }
}
