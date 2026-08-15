using System;

using SymphonyFrameWork.Core;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ機構の状態変更を表示用ReactivePropertyへ変換する。
    /// </summary>
    internal sealed class PauseViewModel : IDisposable
    {
        #region 外部向けAPI

        /// <summary>
        ///     Queryと状態変更元Serviceを指定して生成する。
        /// </summary>
        /// <param name="query"> 表示用Dtoを生成するQuery。 </param>
        /// <param name="service"> 状態変更eventを発行するService。 </param>
        internal PauseViewModel(PauseQuery query, PauseService service)
        {
            // 表示値の取得元と更新通知元が欠けたViewModelを生成させない。
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _service = service ?? throw new ArgumentNullException(nameof(service));

            // 購読前のスナップショットを初期値とし、以降はServiceの変更通知へ追従する。
            _state = new ReactiveProperty<PauseDto>(_query.GetDto());
            _service.OnStateChanged += StateChangedHandler;
        }

        /// <summary> ポーズ機構の表示用更新値。 </summary>
        internal IReadOnlyReactiveProperty<PauseDto> State => _state;

        /// <summary>
        ///     Service eventの購読とReactivePropertyを破棄する。
        /// </summary>
        public void Dispose()
        {
            // 多重破棄で購読解除やReactivePropertyの破棄を繰り返さない。
            if (_isDisposed) { return; }

            // 先に再入を防ぎ、通知元から切り離してから表示状態を破棄する。
            _isDisposed = true;
            _service.OnStateChanged -= StateChangedHandler;
            _state.Dispose();
        }

        #endregion

        #region 内部処理

        private readonly PauseQuery _query;
        private readonly PauseService _service;
        private readonly ReactiveProperty<PauseDto> _state;

        private bool _isDisposed;

        /// <summary>
        ///     Serviceの状態変更を最新の表示値へ反映する。
        /// </summary>
        private void StateChangedHandler()
        {
            _state.SetValue(_query.GetDto());
        }

        #endregion
    }
}
