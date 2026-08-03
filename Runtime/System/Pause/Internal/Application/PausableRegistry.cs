using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     <see cref="PauseManager.IPausable"/>と、それへ通知するための処理の対応表を所有する。
    ///     同じ対象を二重に登録しないことと、解除時に同一の処理を返すことを保証する。
    /// </summary>
    internal sealed class PausableRegistry
    {
        /// <summary> 登録されている購読の件数。 </summary>
        public int Count => _pauseEvents.Count;

        /// <summary>
        ///     未登録の場合だけ通知処理を登録する。
        /// </summary>
        /// <param name="pausable"> ポーズ通知を受け取る対象。 </param>
        /// <param name="pauseEvent"> 対象へ通知するための処理。 </param>
        /// <returns> 新たに登録した場合はtrue。登録済みの場合はfalse。 </returns>
        public bool TryRegister(PauseManager.IPausable pausable, Action<bool> pauseEvent)
        {
            if (pausable == null)
            {
                throw new ArgumentNullException(nameof(pausable));
            }

            if (pauseEvent == null)
            {
                throw new ArgumentNullException(nameof(pauseEvent));
            }

            if (_pauseEvents.ContainsKey(pausable))
            {
                return false;
            }

            _pauseEvents.Add(pausable, pauseEvent);
            return true;
        }

        /// <summary>
        ///     登録済みの場合だけ通知処理を取り出して登録を解除する。
        /// </summary>
        /// <param name="pausable"> ポーズ通知を解除する対象。 </param>
        /// <param name="pauseEvent"> 登録時に使用した処理。 </param>
        /// <returns> 解除した場合はtrue。未登録の場合はfalse。 </returns>
        public bool TryUnregister(PauseManager.IPausable pausable, out Action<bool> pauseEvent)
        {
            if (pausable == null)
            {
                throw new ArgumentNullException(nameof(pausable));
            }

            if (!_pauseEvents.TryGetValue(pausable, out pauseEvent))
            {
                return false;
            }

            _pauseEvents.Remove(pausable);
            return true;
        }

        /// <summary> 全ての登録を消去する。 </summary>
        public void Clear()
        {
            _pauseEvents.Clear();
        }

        private readonly Dictionary<PauseManager.IPausable, Action<bool>> _pauseEvents = new();
    }
}
