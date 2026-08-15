using System;
using System.Collections.Generic;

using SymphonyFrameWork.Core;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary>
    ///     Scene Loadの状態変更を表示用ReactivePropertyへ変換する。
    /// </summary>
    internal sealed class SceneLoadViewModel : IDisposable
    {
        #region 外部向けAPI

        /// <summary>
        ///     Queryと状態変更元Serviceを指定してViewModelを生成する。
        /// </summary>
        /// <param name="query"> 表示用Dtoを生成するQuery。 </param>
        /// <param name="service"> 状態変更eventを発行するService。 </param>
        internal SceneLoadViewModel(
            SceneLoadQuery query,
            SceneLoadService service)
        {
            // 読み取り元と変更通知元を同じ生存期間へ固定する。
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _service = service ?? throw new ArgumentNullException(nameof(service));

            // 初期スナップショットを保持してから、以後の状態変更を購読する。
            _scenes = new ReactiveProperty<IReadOnlyList<SceneLoadDto>>(
                _query.GetDtos(),
                new SceneLoadDtoListComparer());
            _service.OnStateChanged += StateChangedHandler;
        }

        /// <summary> 追跡中Sceneの表示用更新値一覧。 </summary>
        internal IReadOnlyReactiveProperty<IReadOnlyList<SceneLoadDto>> Scenes => _scenes;

        /// <summary>
        ///     Service eventの購読とReactivePropertyを破棄する。
        /// </summary>
        public void Dispose()
        {
            // 破棄済みの場合は、購読解除とReactivePropertyの破棄を重複させない。
            if (_isDisposed) { return; }

            // 再入を拒否してから、登録時と逆順に所有する購読と表示状態を解放する。
            _isDisposed = true;
            _service.OnStateChanged -= StateChangedHandler;
            _scenes.Dispose();
        }

        #endregion

        #region 内部処理

        private readonly SceneLoadQuery _query;
        private readonly SceneLoadService _service;
        private readonly ReactiveProperty<IReadOnlyList<SceneLoadDto>> _scenes;

        private bool _isDisposed;

        /// <summary>
        ///     Serviceの状態変更を最新Dto一覧へ反映する。
        /// </summary>
        private void StateChangedHandler()
        {
            // Entityを直接公開せず、Queryが固定した不変スナップショットだけを表示へ渡す。
            _scenes.SetValue(_query.GetDtos());
        }

        /// <summary>
        ///     Scene Load Dto一覧を内容で比較する。
        /// </summary>
        private sealed class SceneLoadDtoListComparer : IEqualityComparer<IReadOnlyList<SceneLoadDto>>
        {
            #region 外部向けAPI

            /// <summary>
            ///     2つのDto一覧が同じ順序と値を持つか判定する。
            /// </summary>
            /// <param name="left"> 左辺の一覧。 </param>
            /// <param name="right"> 右辺の一覧。 </param>
            /// <returns> 内容が同じ場合はtrue。 </returns>
            public bool Equals(
                IReadOnlyList<SceneLoadDto> left,
                IReadOnlyList<SceneLoadDto> right)
            {
                // 同じ参照なら、要素を走査せず同値と判断できる。
                if (ReferenceEquals(left, right)) { return true; }

                // 片方だけがnullか要素数が異なる一覧は同値にならない。
                if (left == null || right == null || left.Count != right.Count) { return false; }

                // 並び順も表示順の契約に含まれるため、同じ位置のDtoを比較する。
                for (int i = 0; i < left.Count; i++)
                {
                    if (!left[i].Equals(right[i])) { return false; }
                }

                return true;
            }

            /// <summary>
            ///     Dto一覧の内容に基づくハッシュコードを返す。
            /// </summary>
            /// <param name="sceneDtos"> ハッシュ化する一覧。 </param>
            /// <returns> ハッシュコード。 </returns>
            public int GetHashCode(IReadOnlyList<SceneLoadDto> sceneDtos)
            {
                // nullは内容を持たない一覧として固定値を返す。
                if (sceneDtos == null) { return 0; }

                unchecked
                {
                    // Equalsと同じ順序で全Dtoを合成する。
                    int hashCode = 17;
                    for (int i = 0; i < sceneDtos.Count; i++)
                    {
                        hashCode = (hashCode * 31) ^ sceneDtos[i].GetHashCode();
                    }

                    return hashCode;
                }
            }

            #endregion
        }

        #endregion
    }
}
