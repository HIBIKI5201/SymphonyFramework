using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     ポーズ対象と、その対象が属するカテゴリーおよび停止要因の数を保持する。
    /// </summary>
    /// <remarks>
    ///     同じ対象の二重登録を防ぐ。
    ///     **停止要因を件数で持つ。** 真偽値1つで持つと、2カテゴリーがポーズ中に
    ///     片方だけ解除したとき、まだ止まっているべき対象へ再開を通知してしまう。
    /// </remarks>
    internal sealed class PausableRegistry
    {
        #region 外部向けAPI

        /// <summary> 登録されている対象の件数。 </summary>
        public int Count => _registrations.Count;

        /// <summary>
        ///     未登録の場合だけ対象を登録する。
        /// </summary>
        /// <param name="pausable"> ポーズ通知を受け取る対象。 </param>
        /// <param name="categories"> 対象が属するカテゴリー。 </param>
        /// <param name="pausedCategoryCount"> 登録時点でポーズ中のカテゴリーの数。 </param>
        /// <returns> 新たに登録した場合はtrue。登録済みの場合はfalse。 </returns>
        /// <exception cref="ArgumentNullException"> pausableまたはcategoriesがnullの場合。 </exception>
        /// <exception cref="ArgumentException"> categoriesが空の場合。 </exception>
        /// <exception cref="ArgumentOutOfRangeException"> pausedCategoryCountが範囲外の場合。 </exception>
        public bool TryRegister(
            PauseManager.IPausable pausable,
            IReadOnlyList<Type> categories,
            int pausedCategoryCount)
        {
            // nullをDictionaryのキーとして扱えないため、呼び出し元の登録誤りとして拒否する。
            if (pausable == null) { throw new ArgumentNullException(nameof(pausable)); }
            if (categories == null) { throw new ArgumentNullException(nameof(categories)); }

            // 属するカテゴリーが無い対象は、どのカテゴリーを止めても届かない。
            // 解決側が既定カテゴリーを補うため、ここへ空が来るのは呼び出し誤りである。
            if (categories.Count == 0)
            {
                throw new ArgumentException("カテゴリーを1つ以上指定してください。", nameof(categories));
            }

            if (pausedCategoryCount < 0 || pausedCategoryCount > categories.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pausedCategoryCount),
                    pausedCategoryCount,
                    "ポーズ中のカテゴリー数は0以上、カテゴリー数以下で指定してください。");
            }

            // 二重購読によるPauseとResumeの重複通知を防ぐため、先の登録を維持する。
            if (_registrations.ContainsKey(pausable)) { return false; }

            _registrations.Add(pausable, new Registration(categories, pausedCategoryCount));
            return true;
        }

        /// <summary>
        ///     登録済みの場合だけ登録を解除する。
        /// </summary>
        /// <param name="pausable"> ポーズ通知を解除する対象。 </param>
        /// <returns> 解除した場合はtrue。未登録の場合はfalse。 </returns>
        /// <exception cref="ArgumentNullException"> pausableがnullの場合。 </exception>
        public bool TryUnregister(PauseManager.IPausable pausable)
        {
            // nullは登録済みの対象になり得ないため、呼び出し元の解除誤りとして拒否する。
            if (pausable == null) { throw new ArgumentNullException(nameof(pausable)); }

            return _registrations.Remove(pausable);
        }

        /// <summary>
        ///     カテゴリーの状態変化を各対象の停止要因へ反映し、通知が要る対象を返す。
        /// </summary>
        /// <param name="category"> 状態が変化したカテゴリー。 </param>
        /// <param name="isPaused"> 変化後のポーズ状態。 </param>
        /// <returns>
        ///     <paramref name="isPaused" />がtrueなら停止へ移った対象、
        ///     falseなら再開へ移った対象。状態が変わらない対象は含まない。
        /// </returns>
        /// <exception cref="ArgumentNullException"> categoryがnullの場合。 </exception>
        public IReadOnlyList<PauseManager.IPausable> ApplyCategoryState(Type category, bool isPaused)
        {
            if (category == null) { throw new ArgumentNullException(nameof(category)); }

            List<PauseManager.IPausable> changed = new();
            foreach (KeyValuePair<PauseManager.IPausable, Registration> entry in _registrations)
            {
                if (!entry.Value.BelongsTo(category)) { continue; }

                // 0件と1件の境目だけが停止・再開の切り替わりであり、それ以外は状態が続いている。
                if (entry.Value.ApplyPausedCategory(isPaused)) { changed.Add(entry.Key); }
            }

            return changed;
        }

        /// <summary>
        ///     対象が属するカテゴリーを返す。
        /// </summary>
        /// <param name="pausable"> 対象。 </param>
        /// <returns> 属するカテゴリー。未登録の場合は空。 </returns>
        /// <exception cref="ArgumentNullException"> pausableがnullの場合。 </exception>
        public IReadOnlyList<Type> GetCategories(PauseManager.IPausable pausable)
        {
            if (pausable == null) { throw new ArgumentNullException(nameof(pausable)); }

            return _registrations.TryGetValue(pausable, out Registration registration)
                ? registration.Categories
                : Array.Empty<Type>();
        }

        /// <summary>
        ///     登録済みの対象が属するカテゴリーを重複なく返す。
        /// </summary>
        /// <returns> 登録済みの全カテゴリー。 </returns>
        /// <remarks>
        ///     **一括操作の対象を決めるために要る。** 状態側は一度でも操作したカテゴリーしか
        ///     知らないため、まだ誰も止めていないカテゴリーの対象を取りこぼす。
        /// </remarks>
        public IReadOnlyCollection<Type> GetAllCategories()
        {
            HashSet<Type> categories = new();
            foreach (KeyValuePair<PauseManager.IPausable, Registration> entry in _registrations)
            {
                foreach (Type category in entry.Value.Categories)
                {
                    categories.Add(category);
                }
            }

            return categories;
        }

        /// <summary>
        ///     カテゴリーに属する対象の件数を返す。
        /// </summary>
        /// <param name="category"> 対象のカテゴリー。 </param>
        /// <returns> 件数。 </returns>
        /// <exception cref="ArgumentNullException"> categoryがnullの場合。 </exception>
        public int CountIn(Type category)
        {
            if (category == null) { throw new ArgumentNullException(nameof(category)); }

            int count = 0;
            foreach (KeyValuePair<PauseManager.IPausable, Registration> entry in _registrations)
            {
                if (entry.Value.BelongsTo(category)) { count++; }
            }

            return count;
        }

        /// <summary>
        ///     全ての登録を消去する。
        /// </summary>
        public void Clear()
        {
            _registrations.Clear();
        }

        #endregion

        #region 内部処理

        private readonly Dictionary<PauseManager.IPausable, Registration> _registrations = new();

        /// <summary>
        ///     1つの対象について、属するカテゴリーと現在の停止要因の数を保持する。
        /// </summary>
        private sealed class Registration
        {
            /// <summary>
            ///     カテゴリーと初期の停止要因数を指定して生成する。
            /// </summary>
            /// <param name="categories"> 対象が属するカテゴリー。 </param>
            /// <param name="pausedCategoryCount"> 登録時点でポーズ中のカテゴリーの数。 </param>
            internal Registration(IReadOnlyList<Type> categories, int pausedCategoryCount)
            {
                // 呼び出し側の配列を後から書き換えられても登録内容が変わらないよう写す。
                Type[] snapshot = new Type[categories.Count];
                for (int index = 0; index < categories.Count; index++)
                {
                    snapshot[index] = categories[index];
                }

                Categories = snapshot;
                _pausedCategoryCount = pausedCategoryCount;
            }

            /// <summary> 対象が属するカテゴリー。 </summary>
            internal IReadOnlyList<Type> Categories { get; }

            /// <summary>
            ///     カテゴリーに属するかを判定する。
            /// </summary>
            /// <param name="category"> 判定するカテゴリー。 </param>
            /// <returns> 属する場合はtrue。 </returns>
            internal bool BelongsTo(Type category)
            {
                foreach (Type owned in Categories)
                {
                    if (owned == category) { return true; }
                }

                return false;
            }

            /// <summary>
            ///     停止要因の数を更新し、停止と再開の切り替わりかを返す。
            /// </summary>
            /// <param name="isPaused"> 増やす場合はtrue、減らす場合はfalse。 </param>
            /// <returns> 停止または再開へ切り替わった場合はtrue。 </returns>
            internal bool ApplyPausedCategory(bool isPaused)
            {
                if (isPaused)
                {
                    _pausedCategoryCount++;
                    return _pausedCategoryCount == 1;
                }

                // 同じ解除が二重に届いても負の要因数にしない。既に0なら状態は変わっていない。
                if (_pausedCategoryCount == 0) { return false; }

                _pausedCategoryCount--;
                return _pausedCategoryCount == 0;
            }

            private int _pausedCategoryCount;
        }

        #endregion
    }
}
