using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ対象と通知処理の対応を保持する。
    /// </summary>
    /// <remarks> 同じ対象の二重登録を防ぎ、解除時に登録済みの同一処理を返す。 </remarks>
    internal sealed class PausableRegistry
    {
        #region 外部向けAPI

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
            // nullをDictionaryのキーとして扱えないため、呼び出し元の登録誤りとして拒否する。
            if (pausable == null) { throw new ArgumentNullException(nameof(pausable)); }

            // 登録解除で同一Delegateを返せるよう、通知処理の欠落を許容しない。
            if (pauseEvent == null) { throw new ArgumentNullException(nameof(pauseEvent)); }

            // 二重購読によるPauseとResumeの重複通知を防ぐため、先の登録を維持する。
            if (_pauseEvents.ContainsKey(pausable)) { return false; }

            // 解除時に同じDelegateをeventから除去できるよう、対象との対応を保持する。
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
            // nullは登録済みの対象になり得ないため、呼び出し元の解除誤りとして拒否する。
            if (pausable == null) { throw new ArgumentNullException(nameof(pausable)); }

            // 未登録の対象ではeventから除去するDelegateを特定できない。
            if (!_pauseEvents.TryGetValue(pausable, out pauseEvent)) { return false; }

            // event解除に使うDelegateを返したうえで、Registryの所有から外す。
            _pauseEvents.Remove(pausable);
            return true;
        }

        /// <summary>
        ///     全ての登録を消去する。
        /// </summary>
        public void Clear()
        {
            _pauseEvents.Clear();
        }

        #endregion

        #region 内部処理

        private readonly Dictionary<PauseManager.IPausable, Action<bool>> _pauseEvents = new();

        #endregion
    }
}
