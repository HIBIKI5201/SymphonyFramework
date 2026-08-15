using System;
using System.Collections.Generic;

using SymphonyFrameWork.Core;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータの状態変更を表示用ReactivePropertyへ変換する。
    /// </summary>
    internal sealed class SaveDataViewModel : IDisposable
    {
        #region 外部向けAPI

        /// <summary>
        ///     Queryと状態変更元Serviceを指定して生成する。
        /// </summary>
        /// <param name="query"> 表示用Dtoを生成するQuery。 </param>
        /// <param name="service"> 状態変更eventを発行するService。 </param>
        internal SaveDataViewModel(SaveDataQuery query, SaveDataService service)
        {
            // 表示値の生成元と更新通知元が欠けたViewModelは成立しないため、生成時に拒否する。
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _service = service ?? throw new ArgumentNullException(nameof(service));

            // 初期スナップショットを設定し、内容が変わった場合だけ購読者へ通知する。
            _entries = new ReactiveProperty<IReadOnlyList<SaveDataDto>>(
                _query.GetDtos(),
                new SaveDataDtoListComparer());

            // Serviceの論理更新ごとに表示スナップショットを再生成する。
            _service.OnStateChanged += StateChangedHandler;
        }

        /// <summary> キャッシュ済みエントリの表示用更新値一覧。 </summary>
        internal IReadOnlyReactiveProperty<IReadOnlyList<SaveDataDto>> Entries => _entries;

        /// <summary>
        ///     Service eventの購読とReactivePropertyを破棄する。
        /// </summary>
        public void Dispose()
        {
            // 解除済みのeventを重ねて操作せず、複数回の破棄を同じ結果にする。
            if (_isDisposed) { return; }

            // 再入を拒否してから購読を解除し、表示状態を破棄する。
            _isDisposed = true;
            _service.OnStateChanged -= StateChangedHandler;
            _entries.Dispose();
        }

        #endregion

        #region 内部処理

        private readonly SaveDataQuery _query;
        private readonly SaveDataService _service;
        private readonly ReactiveProperty<IReadOnlyList<SaveDataDto>> _entries;

        private bool _isDisposed;

        /// <summary>
        ///     Serviceの状態変更を最新Dto一覧へ反映する。
        /// </summary>
        private void StateChangedHandler()
        {
            // I/Oを行わず現在のRegistryだけから表示値を作り、内容比較付きで更新する。
            _entries.SetValue(_query.GetDtos());
        }

        /// <summary>
        ///     Save Data Dto一覧を内容で比較する。
        /// </summary>
        private sealed class SaveDataDtoListComparer :
            IEqualityComparer<IReadOnlyList<SaveDataDto>>
        {
            #region 外部向けAPI

            /// <summary>
            ///     2つのDto一覧が同じ順序と値を持つか判定する。
            /// </summary>
            /// <param name="left"> 左辺の一覧。 </param>
            /// <param name="right"> 右辺の一覧。 </param>
            /// <returns> 内容が同じ場合はtrue。 </returns>
            public bool Equals(
                IReadOnlyList<SaveDataDto> left,
                IReadOnlyList<SaveDataDto> right)
            {
                // 同じスナップショット参照なら、要素比較を省いて等しいと判定する。
                if (ReferenceEquals(left, right)) { return true; }

                // 片方だけがnull、または件数が違う一覧は同じ表示内容にならない。
                if (left == null || right == null || left.Count != right.Count) { return false; }

                // Queryが保証する順序を前提に、対応するDtoを先頭から比較する。
                for (int i = 0; i < left.Count; i++)
                {
                    // 対応位置の表示値が異なる場合は、以降を比較せず変更ありと判定する。
                    if (!left[i].Equals(right[i])) { return false; }
                }

                return true;
            }

            /// <summary>
            ///     Dto一覧の内容に基づくハッシュコードを返す。
            /// </summary>
            /// <param name="saveDataDtos"> ハッシュ化する一覧。 </param>
            /// <returns> ハッシュコード。 </returns>
            public int GetHashCode(IReadOnlyList<SaveDataDto> saveDataDtos)
            {
                // nullを空でない一覧と区別しつつ、例外を出さず比較契約へ含める。
                if (saveDataDtos == null) { return 0; }

                unchecked
                {
                    // Equalsと同じ順序で全Dtoを組み合わせ、オーバーフローをハッシュ値として許容する。
                    int hashCode = 17;
                    for (int i = 0; i < saveDataDtos.Count; i++)
                    {
                        hashCode = (hashCode * 31) ^ saveDataDtos[i].GetHashCode();
                    }

                    return hashCode;
                }
            }

            #endregion
        }

        #endregion
    }
}
