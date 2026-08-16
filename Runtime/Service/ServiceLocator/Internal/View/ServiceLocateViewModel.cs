using System;
using System.Collections.Generic;

using SymphonyFrameWork.Core;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     Service Locatorの状態変更を表示用ReactivePropertyへ変換する。
    /// </summary>
    internal sealed class ServiceLocateViewModel : IDisposable
    {
        #region 外部向けAPI

        /// <summary>
        ///     Queryと状態変更元Serviceを指定してViewModelを生成する。
        /// </summary>
        /// <param name="query"> 表示用Dtoを生成するQuery。 </param>
        /// <param name="service"> 状態変更eventを発行するService。 </param>
        internal ServiceLocateViewModel(
            ServiceLocateQuery query,
            ServiceLocateService service)
        {
            // 表示値の取得元と変更通知元を固定し、現在の登録一覧から初期表示を作る。
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _registrations = new ReactiveProperty<IReadOnlyList<ServiceLocateDto>>(
                _query.GetDtos(),
                new ServiceLocateDtoListComparer());

            // 登録状態が確定するたびに表示値を同期できるよう、生成時に購読する。
            _service.OnStateChanged += StateChangedHandler;
        }

        /// <summary> 登録中Serviceの表示用更新値一覧。 </summary>
        internal IReadOnlyReactiveProperty<IReadOnlyList<ServiceLocateDto>> Registrations =>
            _registrations;

        /// <summary>
        ///     Service eventの購読とReactivePropertyを破棄する。
        /// </summary>
        public void Dispose()
        {
            // 破棄済みの場合は、購読解除とReactivePropertyの破棄を重複させない。
            if (_isDisposed) { return; }

            // 再入を拒否してから通知経路と表示状態を破棄する。
            _isDisposed = true;
            _service.OnStateChanged -= StateChangedHandler;
            _registrations.Dispose();
        }

        #endregion

        #region 内部処理

        private readonly ServiceLocateQuery _query;
        private readonly ServiceLocateService _service;
        private readonly ReactiveProperty<IReadOnlyList<ServiceLocateDto>> _registrations;

        private bool _isDisposed;

        /// <summary>
        ///     Serviceの状態変更を最新Dto一覧へ反映する。
        /// </summary>
        private void StateChangedHandler()
        {
            // Queryが固定した順序のスナップショットで表示値を更新する。
            _registrations.SetValue(_query.GetDtos());
        }

        /// <summary>
        ///     Service Locate Dto一覧を内容で比較する。
        /// </summary>
        private sealed class ServiceLocateDtoListComparer :
            IEqualityComparer<IReadOnlyList<ServiceLocateDto>>
        {
            #region 外部向けAPI

            /// <summary>
            ///     2つのDto一覧が同じ順序と値を持つか判定する。
            /// </summary>
            /// <param name="left"> 左辺の一覧。 </param>
            /// <param name="right"> 右辺の一覧。 </param>
            /// <returns> 内容が同じ場合はtrue。 </returns>
            public bool Equals(
                IReadOnlyList<ServiceLocateDto> left,
                IReadOnlyList<ServiceLocateDto> right)
            {
                // 同じ一覧参照は内容の走査をせず同値とする。
                if (ReferenceEquals(left, right)) { return true; }

                // 片方だけがnull、または要素数が異なる一覧は同じ内容にならない。
                if (left == null || right == null || left.Count != right.Count) { return false; }

                // 同じ位置のDtoが異なる一覧は、並び順を含む表示状態が一致しない。
                for (int i = 0; i < left.Count; i++)
                {
                    if (!left[i].Equals(right[i])) { return false; }
                }

                return true;
            }

            /// <summary>
            ///     Dto一覧の内容に基づくハッシュコードを返す。
            /// </summary>
            /// <param name="serviceDtos"> ハッシュ化する一覧。 </param>
            /// <returns> ハッシュコード。 </returns>
            public int GetHashCode(IReadOnlyList<ServiceLocateDto> serviceDtos)
            {
                // Equalsがnullを内容の無い値として扱うため、固定値を返す。
                if (serviceDtos == null) { return 0; }

                // Equalsと同じ順序ですべてのDtoをハッシュへ反映する。
                unchecked
                {
                    int hashCode = 17;
                    for (int i = 0; i < serviceDtos.Count; i++)
                    {
                        hashCode = (hashCode * 31)
                            ^ serviceDtos[i].GetHashCode();
                    }

                    return hashCode;
                }
            }

            #endregion
        }

        #endregion
    }
}
