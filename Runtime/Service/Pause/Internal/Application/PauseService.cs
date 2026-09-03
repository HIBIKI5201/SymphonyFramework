using System;
using System.Collections.Generic;

using SymphonyFrameWork.Debugger.Logger;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     カテゴリーごとのポーズ状態を変更し、登録された対象へ通知する。
    /// </summary>
    /// <remarks> 状態は<see cref="PauseStateEntity"/>、購読は<see cref="PausableRegistry"/>へ委譲する。 </remarks>
    internal sealed class PauseService
    {
        #region 外部向けAPI

        /// <summary>
        ///     状態と購読の保持先を指定して生成する。
        /// </summary>
        /// <param name="state"> ポーズ状態を保持するEntity。 </param>
        /// <param name="registry"> ポーズ通知の購読を所有するレジストリ。 </param>
        public PauseService(PauseStateEntity state, PausableRegistry registry)
        {
            // 状態と購読のどちらが欠けても通知の整合を保てないため、生成時に拒否する。
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary> どれか1つでもポーズ中かが変化したときに新しい状態を通知する。 </summary>
        /// <remarks> ゲームロジックの購読者例外はそのまま伝播する。 </remarks>
        public event Action<bool> OnPauseChanged;

        /// <summary> ポーズ状態または購読件数が変化したときに通知する。 </summary>
        public event Action OnStateChanged;

        /// <summary> どれか1つでもポーズ中かどうか。 </summary>
        public bool IsPausedAny => _state.IsPausedAny;

        /// <summary> ポーズ通知を購読している対象の件数。 </summary>
        public int PausableSubscriberCount => _registry.Count;

        /// <summary>
        ///     カテゴリーに属するポーズ対象の件数を返す。
        /// </summary>
        /// <param name="category"> 対象のカテゴリー。 </param>
        /// <returns> 件数。 </returns>
        public int CountPausablesIn(Type category) => _registry.CountIn(category);

        /// <summary>
        ///     カテゴリーのポーズ状態を返す。
        /// </summary>
        /// <param name="category"> 対象のカテゴリー。 </param>
        /// <returns> ポーズ中の場合はtrue。 </returns>
        public bool IsPaused(Type category) => _state.IsPaused(category);

        /// <summary>
        ///     カテゴリーのポーズ状態を設定する。状態が変化したときだけ通知する。
        /// </summary>
        /// <param name="category"> 対象のカテゴリー。 </param>
        /// <param name="isPaused"> 設定するポーズ状態。 </param>
        /// <exception cref="ArgumentNullException"> categoryがnullの場合。 </exception>
        public void SetPaused(Type category, bool isPaused)
        {
            if (category == null) { throw new ArgumentNullException(nameof(category)); }

            ApplyCategories(new[] { category }, isPaused);
        }

        /// <summary>
        ///     登録済みの全カテゴリーと既定カテゴリーへポーズ状態を設定する。
        /// </summary>
        /// <param name="isPaused"> 設定するポーズ状態。 </param>
        /// <remarks>
        ///     対象は「状態を持つカテゴリー」と「登録済みの対象が属するカテゴリー」の和に、
        ///     既定カテゴリーを加えたものである。**状態側だけを見ると足りない。**
        ///     まだ誰も止めていないカテゴリーは状態を持たず、そこに属する対象を取りこぼす。
        /// </remarks>
        public void SetPausedAll(bool isPaused)
        {
            HashSet<Type> targets = new(_state.Categories);
            targets.UnionWith(_registry.GetAllCategories());

            // 何も登録されていない状態で止めても「ポーズ中」になるよう、既定カテゴリーは必ず含める。
            targets.Add(PauseCategoryResolver.DefaultCategory);

            ApplyCategories(targets, isPaused);
        }

        /// <summary>
        ///     ポーズ通知の購読者を追加する。
        /// </summary>
        /// <param name="handler"> 追加する処理。 </param>
        public void AddPauseChangedHandler(Action<bool> handler)
        {
            OnPauseChanged += handler;
        }

        /// <summary>
        ///     ポーズ通知の購読者を除去する。
        /// </summary>
        /// <param name="handler"> 除去する処理。 </param>
        public void RemovePauseChangedHandler(Action<bool> handler)
        {
            OnPauseChanged -= handler;
        }

        /// <summary>
        ///     カテゴリー単位のポーズ通知の購読者を追加する。
        /// </summary>
        /// <param name="category"> 対象のカテゴリー。 </param>
        /// <param name="handler"> 追加する処理。 </param>
        /// <exception cref="ArgumentNullException"> categoryまたはhandlerがnullの場合。 </exception>
        public void AddPauseChangedHandler(Type category, Action<bool> handler)
        {
            if (category == null) { throw new ArgumentNullException(nameof(category)); }
            if (handler == null) { throw new ArgumentNullException(nameof(handler)); }

            _categoryHandlers.TryGetValue(category, out Action<bool> current);
            _categoryHandlers[category] = current + handler;
        }

        /// <summary>
        ///     カテゴリー単位のポーズ通知の購読者を除去する。
        /// </summary>
        /// <param name="category"> 対象のカテゴリー。 </param>
        /// <param name="handler"> 除去する処理。 </param>
        /// <exception cref="ArgumentNullException"> categoryまたはhandlerがnullの場合。 </exception>
        public void RemovePauseChangedHandler(Type category, Action<bool> handler)
        {
            if (category == null) { throw new ArgumentNullException(nameof(category)); }
            if (handler == null) { throw new ArgumentNullException(nameof(handler)); }

            if (!_categoryHandlers.TryGetValue(category, out Action<bool> current)) { return; }

            Action<bool> remaining = current - handler;

            // 購読者が0件になったカテゴリーの行を残さない。Resetまで辞書が伸び続ける。
            if (remaining == null) { _categoryHandlers.Remove(category); }
            else { _categoryHandlers[category] = remaining; }
        }

        /// <summary>
        ///     ポーズ通知を受け取る対象を登録する。登録済みの場合は何もしない。
        /// </summary>
        /// <param name="pausable"> ポーズ通知を受け取る対象。 </param>
        /// <exception cref="ArgumentNullException"> pausableがnullの場合。 </exception>
        /// <remarks>
        ///     **登録した時点では <c>Pause()</c> を呼ばない。** 停止要因の数だけを現在の状態から
        ///     引き継ぎ、後で解除されたときに <c>Resume()</c> が届く形にする。従来と同じ挙動である。
        /// </remarks>
        public void Register(PauseManager.IPausable pausable)
        {
            // 通知先を持たない購読は登録できないため、呼び出し元の誤りとして拒否する。
            if (pausable == null) { throw new ArgumentNullException(nameof(pausable)); }

            IReadOnlyList<Type> categories = PauseCategoryResolver.Resolve(pausable.GetType());

            int pausedCategoryCount = 0;
            foreach (Type category in categories)
            {
                if (_state.IsPaused(category)) { pausedCategoryCount++; }
            }

            // 同じ対象の二重購読はPauseとResumeを重複実行するため追加しない。
            if (!_registry.TryRegister(pausable, categories, pausedCategoryCount)) { return; }

            RaiseStateChanged();
        }

        /// <summary>
        ///     ポーズ通知を受け取る対象の登録を解除する。未登録の場合は何もしない。
        /// </summary>
        /// <param name="pausable"> ポーズ通知を解除する対象。 </param>
        /// <exception cref="ArgumentNullException"> pausableがnullの場合。 </exception>
        public void Unregister(PauseManager.IPausable pausable)
        {
            // 通知先を特定できない解除要求は、呼び出し元の誤りとして拒否する。
            if (pausable == null) { throw new ArgumentNullException(nameof(pausable)); }

            if (!_registry.TryUnregister(pausable)) { return; }

            RaiseStateChanged();
        }

        /// <summary>
        ///     ポーズ状態と購読を消去する。
        /// </summary>
        public void Reset()
        {
            // Domain Reloadなしの再初期化へ前回の状態やゲームロジックの購読を残さない。
            _state.Reset();
            _registry.Clear();
            _categoryHandlers.Clear();
            OnPauseChanged = null;
            RaiseStateChanged();
        }

        #endregion

        #region 内部処理

        private readonly PauseStateEntity _state;
        private readonly PausableRegistry _registry;
        private readonly Dictionary<Type, Action<bool>> _categoryHandlers = new();

        /// <summary>
        ///     複数カテゴリーの状態をまとめて設定し、切り替わった対象と全体の変化を通知する。
        /// </summary>
        /// <param name="categories"> 設定するカテゴリー。 </param>
        /// <param name="isPaused"> 設定するポーズ状態。 </param>
        /// <remarks>
        ///     **状態を全部確定させてから通知する。** カテゴリーごとに通知すると、
        ///     購読者が例外を投げたときに残りのカテゴリーが未設定のまま中断し、
        ///     状態と通知が食い違ったまま残る。表示への通知も1回にまとまる。
        /// </remarks>
        private void ApplyCategories(IEnumerable<Type> categories, bool isPaused)
        {
            // 全体の状態が変化したかは、カテゴリーを書き換える前後で比べないと判定できない。
            bool wasPausedAny = _state.IsPausedAny;

            List<Type> changedCategories = new();
            List<PauseManager.IPausable> affected = new();
            foreach (Type category in categories)
            {
                // 同じ状態の再設定ではゲームロジックと表示へ重複通知しない。
                if (!_state.SetPaused(category, isPaused)) { continue; }

                changedCategories.Add(category);
                affected.AddRange(_registry.ApplyCategoryState(category, isPaused));
            }

            if (changedCategories.Count == 0) { return; }

            // 表示状態を先に同期し、確定したポーズ状態をゲームロジックへ通知する。従来と同じ順序である。
            RaiseStateChanged();

            foreach (PauseManager.IPausable pausable in affected)
            {
                // 新しい状態に応じて、対象の停止と再開のどちらか一方だけを通知する。
                if (isPaused) { pausable.Pause(); }
                else { pausable.Resume(); }
            }

            // カテゴリー単位の購読者へは、実際に変化したカテゴリーの分だけ発行する。
            foreach (Type category in changedCategories)
            {
                if (_categoryHandlers.TryGetValue(category, out Action<bool> handler))
                {
                    handler.Invoke(isPaused);
                }
            }

            // 公開APIの OnPauseChanged は「ポーズ中か否か」の変化を表す。
            // カテゴリー単位の変化で毎回発行すると、従来の購読者へ同じ値が連続して届く。
            bool isPausedAny = _state.IsPausedAny;
            if (wasPausedAny != isPausedAny) { OnPauseChanged?.Invoke(isPausedAny); }
        }

        /// <summary>
        ///     表示向けの状態変更を通知する。
        /// </summary>
        /// <remarks> 表示専用ViewModelの失敗をゲーム側のポーズ処理へ伝播させない。 </remarks>
        private void RaiseStateChanged()
        {
            try
            {
                // 表示をポーズ状態と購読件数の確定後に同期する。
                OnStateChanged?.Invoke();
            }
            catch (Exception exception)
            {
                // 表示側の失敗はゲームロジックのポーズ処理へ逆流させず、診断ログだけを残す。
                SymphonyDebugLogger.LogException(exception);
            }
        }

        #endregion
    }
}
