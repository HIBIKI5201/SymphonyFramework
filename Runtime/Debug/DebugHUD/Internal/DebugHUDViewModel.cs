using System;

using SymphonyFrameWork.Core;

namespace SymphonyFrameWork.Debugger.HUD
{
    /// <summary> Debug HUDの状態変更を表示用ReactivePropertyへ変換する。 </summary>
    internal sealed class DebugHUDViewModel : IDisposable
    {
        #region 外部向けAPI

        /// <summary> 未初期化状態を初期値として生成する。 </summary>
        internal DebugHUDViewModel()
        {
            _state = new ReactiveProperty<DebugHUDDto>(default);
        }

        /// <summary> Debug HUDの表示用更新値。 </summary>
        internal IReadOnlyReactiveProperty<DebugHUDDto> State => _state;

        /// <summary> 最新の表示値を反映する。 </summary>
        internal void SetState(DebugHUDDto state) => _state.SetValue(state);

        /// <summary> ReactivePropertyと全購読を破棄する。 </summary>
        public void Dispose()
        {
            if (_isDisposed) { return; }

            _isDisposed = true;
            _state.Dispose();
        }

        #endregion

        #region 内部処理

        private readonly ReactiveProperty<DebugHUDDto> _state;
        private bool _isDisposed;

        #endregion
    }
}
