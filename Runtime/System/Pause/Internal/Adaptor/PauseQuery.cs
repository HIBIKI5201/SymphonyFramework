using System;

namespace SymphonyFrameWork.System
{
    /// <summary> ポーズ機構の内部状態を公開Infoまたは表示用Dtoへ変換する。 </summary>
    internal sealed class PauseQuery
    {
        /// <summary> 読み取り対象の状態と購読の保持先を指定してQueryを生成する。 </summary>
        /// <param name="state"> ポーズ状態を保持するEntity。 </param>
        /// <param name="registry"> ポーズ通知の購読を所有するレジストリ。 </param>
        internal PauseQuery(PauseStateEntity state, PausableRegistry registry)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary> 現在ポーズ中かどうか。 </summary>
        internal bool IsPaused => _state.IsPaused;

        /// <summary> 管理状態の公開スナップショットを返す。 </summary>
        /// <returns> 取得時点の管理状態。 </returns>
        internal PauseInfo GetInfo() => new(_state.IsPaused, _registry.Count);

        /// <summary> 管理状態の表示用更新値を返す。 </summary>
        /// <returns> 取得時点の表示値。 </returns>
        internal PauseDto GetDto() => new(_state.IsPaused, _registry.Count);

        private readonly PauseStateEntity _state;
        private readonly PausableRegistry _registry;
    }
}
