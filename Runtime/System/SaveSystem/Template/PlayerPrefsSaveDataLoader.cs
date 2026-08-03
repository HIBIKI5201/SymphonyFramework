using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     <see cref="PlayerPrefsSaveDataLoaderStrategy"/>へ置き換えられた非推奨の基底クラスです。
    ///     この型を継承した既存のローダーは、新しい基底の派生としてそのまま動作します。
    ///     メンバーを持たないため、実装は<see cref="PlayerPrefsSaveDataLoaderStrategy"/>が担います。
    /// </summary>
    [Serializable]
    [Obsolete("PlayerPrefsSaveDataLoaderStrategyを継承してください。3.0.0で削除します。", error: false)]
    public abstract class PlayerPrefsSaveDataLoader : PlayerPrefsSaveDataLoaderStrategy
    {
    }
}
