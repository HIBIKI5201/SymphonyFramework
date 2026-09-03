using System;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ機構の内部状態を公開Infoまたは表示用Dtoへ変換する。
    /// </summary>
    internal sealed class PauseQuery
    {
        #region 外部向けAPI

        /// <summary>
        ///     読み取り対象の状態と購読の保持先を指定して生成する。
        /// </summary>
        /// <param name="state"> ポーズ状態を保持するEntity。 </param>
        /// <param name="registry"> ポーズ通知の購読を所有するレジストリ。 </param>
        internal PauseQuery(PauseStateEntity state, PausableRegistry registry)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary> どれか1つでもポーズ中かどうか。 </summary>
        internal bool IsPausedAny => _state.IsPausedAny;

        /// <summary>
        ///     カテゴリーのポーズ状態を返す。
        /// </summary>
        /// <param name="category"> 対象のカテゴリー。 </param>
        /// <returns> ポーズ中の場合はtrue。 </returns>
        internal bool IsPaused(Type category) => _state.IsPaused(category);

        /// <summary>
        ///     管理状態の公開スナップショットを返す。
        /// </summary>
        /// <returns> 取得時点の管理状態。 </returns>
        internal PauseInfo GetInfo() => new(_state.IsPausedAny, _registry.Count);

        /// <summary>
        ///     カテゴリー単位の公開スナップショットを返す。
        /// </summary>
        /// <param name="category"> 対象のカテゴリー。 </param>
        /// <returns> 取得時点のカテゴリーの状態。 </returns>
        internal PauseInfo GetInfo(Type category) =>
            new(_state.IsPaused(category), _registry.CountIn(category), category);

        /// <summary>
        ///     管理状態の表示用更新値を返す。
        /// </summary>
        /// <returns> 取得時点の表示値。 </returns>
        internal PauseDto GetDto() => new(_state.IsPausedAny, _registry.Count);

        #endregion

        #region 内部処理

        private readonly PauseStateEntity _state;
        private readonly PausableRegistry _registry;

        #endregion
    }
}
