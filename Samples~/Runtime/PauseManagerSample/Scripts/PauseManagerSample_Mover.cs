namespace SymphonyFrameWork.Samples.PauseManagerSample
{
    /// <summary>
    ///     ゲームプレイカテゴリーに属する往復移動オブジェクト。
    /// </summary>
    /// <remarks>
    ///     **属するカテゴリーはinterfaceの実装で決まる。** 登録の呼び出しは基底クラスと同じで、
    ///     <c>PauseManager.SetPause&lt;IPauseManagerSample_GameplayPausable&gt;(true)</c> で止まる。
    /// </remarks>
    public sealed class PauseManagerSample_Mover
        : PauseManagerSample_MoverBase, IPauseManagerSample_GameplayPausable
    {
    }
}
