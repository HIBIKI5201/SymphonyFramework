using System.Threading;

using SymphonyFramework.Services;

using UnityEngine;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     6.x の <c>SymphonyLocateObject&lt;T&gt;</c> を <see cref="PartituraReference{T}" /> へ転送する。
    /// </summary>
    /// <remarks> 7.0.0 の移行期間だけ残す。8.0.0 で削除する。 </remarks>
    /// <typeparam name="T"> 取得するサービスの型。 </typeparam>
    [global::System.Obsolete("SymphonyLocateObject<T> は PartituraReference<T>（名前空間 SymphonyFramework.Services）へ改名しました。GetInstance は Get / TryGet です。8.0.0 で削除します。")]
    public sealed class SymphonyLocateObject<T> where T : class
    {
        #region 外部向けAPI

        /// <summary> キャッシュを持たずに生成する。 </summary>
        public SymphonyLocateObject() => _reference = new PartituraReference<T>();

        /// <summary> 取得済みのインスタンスをキャッシュとして生成する。 </summary>
        /// <param name="instance"> キャッシュするインスタンス。 </param>
        public SymphonyLocateObject(T instance) => _reference = new PartituraReference<T>(instance);

        /// <summary> 旧 <c>GetInstance</c>。 </summary>
        /// <remarks> 6.x と同じく、キャッシュも登録も無いときは null を返す。 </remarks>
        public T GetInstance() => _reference.TryGet(out T instance) ? instance : null;

        /// <summary> 旧 <c>GetInstanceAsync</c>。<see cref="PartituraReference{T}.GetAsync" /> へ転送する。 </summary>
        public Awaitable<T> GetInstanceAsync(byte grace = 120, CancellationToken token = default) =>
            _reference.GetAsync(grace, token);

        /// <summary> 旧 <c>TryGetInstance</c>。<see cref="PartituraReference{T}.TryGet" /> へ転送する。 </summary>
        public bool TryGetInstance(out T instance) => _reference.TryGet(out instance);

        #endregion

        #region 内部処理

        /// <summary> 転送先の参照。キャッシュはこちらが持つ。 </summary>
        private readonly PartituraReference<T> _reference;

        #endregion
    }
}
