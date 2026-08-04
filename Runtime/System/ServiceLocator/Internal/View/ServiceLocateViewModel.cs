using System;
using System.Collections.Generic;

using SymphonyFrameWork.Core;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary> Service Locatorの状態変更を表示用ReactivePropertyへ変換する。 </summary>
    internal sealed class ServiceLocateViewModel : IDisposable
    {
        /// <summary> Queryと状態変更元Serviceを指定してViewModelを生成する。 </summary>
        /// <param name="query"> 表示用Dtoを生成するQuery。 </param>
        /// <param name="service"> 状態変更eventを発行するService。 </param>
        internal ServiceLocateViewModel(
            ServiceLocateQuery query,
            ServiceLocateService service)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _registrations = new ReactiveProperty<IReadOnlyList<ServiceLocateDto>>(
                _query.GetDtos(),
                new ServiceLocateDtoListComparer());
            _service.OnStateChanged += StateChangedHandler;
        }

        /// <summary> 登録中Serviceの表示用更新値一覧。 </summary>
        internal IReadOnlyReactiveProperty<IReadOnlyList<ServiceLocateDto>> Registrations =>
            _registrations;

        private readonly ServiceLocateQuery _query;
        private readonly ServiceLocateService _service;
        private readonly ReactiveProperty<IReadOnlyList<ServiceLocateDto>> _registrations;

        private bool _isDisposed;

        /// <summary> Service eventの購読とReactivePropertyを破棄する。 </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _service.OnStateChanged -= StateChangedHandler;
            _registrations.Dispose();
        }

        /// <summary> Serviceの状態変更を最新Dto一覧へ反映する。 </summary>
        private void StateChangedHandler()
        {
            _registrations.SetValue(_query.GetDtos());
        }

        /// <summary> Service Locate Dto一覧を内容で比較する。 </summary>
        private sealed class ServiceLocateDtoListComparer :
            IEqualityComparer<IReadOnlyList<ServiceLocateDto>>
        {
            /// <summary> 2つのDto一覧が同じ順序と値を持つか判定する。 </summary>
            /// <param name="left"> 左辺の一覧。 </param>
            /// <param name="right"> 右辺の一覧。 </param>
            /// <returns> 内容が同じ場合はtrue。 </returns>
            public bool Equals(
                IReadOnlyList<ServiceLocateDto> left,
                IReadOnlyList<ServiceLocateDto> right)
            {
                if (ReferenceEquals(left, right))
                {
                    return true;
                }

                if (left == null || right == null || left.Count != right.Count)
                {
                    return false;
                }

                for (int i = 0; i < left.Count; i++)
                {
                    if (!left[i].Equals(right[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary> Dto一覧の内容に基づくハッシュコードを返す。 </summary>
            /// <param name="serviceDtos"> ハッシュ化する一覧。 </param>
            /// <returns> ハッシュコード。 </returns>
            public int GetHashCode(IReadOnlyList<ServiceLocateDto> serviceDtos)
            {
                if (serviceDtos == null)
                {
                    return 0;
                }

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
        }
    }
}
