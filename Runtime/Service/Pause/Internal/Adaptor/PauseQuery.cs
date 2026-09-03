using System;
using System.Collections.Generic;

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
        internal PauseDto GetDto() =>
            new(_state.IsPausedAny, _registry.Count, BuildCategoryDtos());

        #endregion

        #region 内部処理

        private readonly PauseStateEntity _state;
        private readonly PausableRegistry _registry;

        /// <summary>
        ///     表示するカテゴリーの一覧を組み立てる。
        /// </summary>
        /// <returns> 表示名の昇順に並んだカテゴリーごとの表示値。 </returns>
        /// <remarks>
        ///     <para>
        ///         対象は「状態を持つカテゴリー」と「登録済みの対象が属するカテゴリー」の和である。
        ///         **状態側だけを見ると足りない。** まだ誰も止めていないカテゴリーは状態を持たない。
        ///     </para>
        ///     <para>
        ///         **必ず並べ替える。** 辞書の列挙順は保証されず、並びが揺れると
        ///         内容が同じでもViewModelが変化として通知してしまう。
        ///     </para>
        /// </remarks>
        private IReadOnlyList<PauseCategoryDto> BuildCategoryDtos()
        {
            HashSet<Type> categories = new(_state.Categories);
            categories.UnionWith(_registry.GetAllCategories());

            List<PauseCategoryDto> dtos = new(categories.Count);
            foreach (Type category in categories)
            {
                dtos.Add(new PauseCategoryDto(
                    category.Name,
                    _state.IsPaused(category),
                    _registry.CountIn(category)));
            }

            dtos.Sort(static (left, right) =>
                string.CompareOrdinal(left.CategoryName, right.CategoryName));

            return dtos;
        }

        #endregion
    }
}
