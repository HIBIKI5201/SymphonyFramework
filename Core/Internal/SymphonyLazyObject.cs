using System;

namespace SymphonyFrameWork.Core
{
    /// <summary>
    ///     UnityEngine.Objectを遅延生成し、破棄後に生成し直す参照を保持する。
    /// </summary>
    /// <remarks>
    ///     System.Lazy&lt;T&gt;では検知できないUnity側の破棄と、Domain Reload無効環境に対応する。
    /// </remarks>
    /// <typeparam name="T"> 遅延生成するUnityEngine.Objectの型。 </typeparam>
    internal sealed class SymphonyLazyObject<T> where T : UnityEngine.Object
    {
        #region 外部向けAPI

        /// <summary>
        ///     生成処理と破棄処理を指定して初期化する。
        /// </summary>
        /// <param name="factory"> 対象を生成する処理。 </param>
        /// <param name="destroyer">
        ///     対象を破棄する処理。
        ///     ComponentのようにGameObjectごと破棄したい場合に指定する。
        ///     省略した場合はObject.Destroyで対象自身のみを破棄する。
        /// </param>
        public SymphonyLazyObject(Func<T> factory, Action<T> destroyer = null)
        {
            // 遅延生成に必須の処理は構築時に検証し、破棄方法は対象に応じて差し替えられるよう保持する。
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _destroyer = destroyer;
        }

        /// <summary> 生成済みで、かつ破棄されていない場合はtrue。 </summary>
        public bool IsAlive => _instance;

        /// <summary> 生成済みの対象。未生成または破棄済みの場合は生成し直す。 </summary>
        public T Value
        {
            get
            {
                // 破棄済みのUnityEngine.Objectはbool変換でfalseになるため、生成し直す。
                if (!_instance) { _instance = _factory(); }

                return _instance;
            }
        }

        /// <summary>
        ///     生成せずに現在の対象を取得する。
        /// </summary>
        /// <param name="value"> 生存している場合は対象、それ以外はnull。 </param>
        /// <returns> 生存している対象を取得できた場合はtrue。 </returns>
        public bool TryGetValue(out T value)
        {
            // Unityのbool変換を一度だけ評価し、戻り値とout値の判定を一致させる。
            bool isAlive = _instance;
            value = isAlive ? _instance : null;
            return isAlive;
        }

        /// <summary>
        ///     生成済みの対象を破棄し、未生成の状態へ戻す。
        /// </summary>
        /// <remarks> 破棄済み、または未生成の場合は破棄処理を行わない。 </remarks>
        public void Destroy()
        {
            // 生存中の対象だけを破棄し、Unity側で既に破棄された参照には触れない。
            if (_instance)
            {
                // 呼び出し側が破棄規則を指定していればそれを優先し、未指定ならUnity標準の方法で破棄する。
                if (_destroyer != null) { _destroyer(_instance); }
                else { UnityEngine.Object.Destroy(_instance); }
            }

            // Unityの遅延破棄中でも次回アクセス時に新しい対象を生成できる状態へ戻す。
            _instance = null;
        }

        #endregion

        #region 内部処理

        private readonly Func<T> _factory;
        private readonly Action<T> _destroyer;

        private T _instance;

        #endregion
    }
}
