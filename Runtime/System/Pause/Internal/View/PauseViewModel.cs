using System;

using SymphonyFrameWork.Core;

namespace SymphonyFrameWork.System
{
    /// <summary> ポーズ機構の状態変更を表示用ReactivePropertyへ変換する。 </summary>
    internal sealed class PauseViewModel : IDisposable
    {
        /// <summary> Queryと状態変更元Serviceを指定してViewModelを生成する。 </summary>
        /// <param name="query"> 表示用Dtoを生成するQuery。 </param>
        /// <param name="service"> 状態変更eventを発行するService。 </param>
        internal PauseViewModel(PauseQuery query, PauseService service)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _state = new ReactiveProperty<PauseDto>(_query.GetDto());
            _service.OnStateChanged += StateChangedHandler;
        }

        /// <summary> ポーズ機構の表示用更新値。 </summary>
        internal IReadOnlyReactiveProperty<PauseDto> State => _state;

        /// <summary> Service eventの購読とReactivePropertyを破棄する。 </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _service.OnStateChanged -= StateChangedHandler;
            _state.Dispose();
        }

        /// <summary> Serviceの状態変更を最新の表示値へ反映する。 </summary>
        private void StateChangedHandler()
        {
            _state.SetValue(_query.GetDto());
        }

        private readonly PauseQuery _query;
        private readonly PauseService _service;
        private readonly ReactiveProperty<PauseDto> _state;

        private bool _isDisposed;
    }
}
