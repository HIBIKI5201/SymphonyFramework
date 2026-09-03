using SymphonyFrameWork.System;

namespace SymphonyFrameWork.Samples.PauseManagerSample
{
    /// <summary>
    ///     ゲームプレイのポーズカテゴリー。
    /// </summary>
    /// <remarks>
    ///     カテゴリーは<see cref="PauseManager.IPausable"/>を継承した空のinterfaceである。
    ///     このサンプルでは手で書いているが、
    ///     <c>Project Settings > SymphonyFrameWork > Pause Category</c> から生成もできる。
    /// </remarks>
    public interface IPauseManagerSample_GameplayPausable : PauseManager.IPausable { }

    /// <summary>
    ///     UI演出のポーズカテゴリー。
    /// </summary>
    /// <remarks>
    ///     ゲームプレイを止めたままUIだけ動かし続ける、といった分け方を実演するために置く。
    /// </remarks>
    public interface IPauseManagerSample_UiPausable : PauseManager.IPausable { }
}
