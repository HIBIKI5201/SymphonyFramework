using System.Threading;
using System.Threading.Tasks;

using SymphonyFramework.Utility;

using UnityEngine;

namespace SymphonyFramework.Services
{
    /// <summary>
    ///     Partituraに登録されているインスタンスを保持する。
    /// </summary>
    public sealed class PartituraReference<T> where T : class
    {
        #region 外部向けAPI

        /// <summary>
        ///     インスタンスを初期化して生成する。
        /// </summary>
        public PartituraReference() => _instance = null;

        /// <summary>
        ///     初期インスタンスをセットして生成する。
        /// </summary>
        /// <param name="instance"> 初期キャッシュとして保持するインスタンス。 </param>
        public PartituraReference(T instance) => _instance = instance;

        /// <summary>
        ///     登録済みであることが必須のインスタンスを取得する。
        /// </summary>
        /// <returns> キャッシュまたはPartituraから取得したインスタンス。 </returns>
        /// <exception cref="ServiceNotRegisteredException"> キャッシュが無く、指定型も登録されていない場合。 </exception>
        public T Get()
        {
            T instance = GetCachedInstance();

            // キャッシュが無い場合だけPartituraへ問い合わせ、以後の取得に再利用する。
            if (instance == null)
            {
                instance = Partitura.Get<T>();
                _instance = instance;
            }

            return instance;
        }

        /// <summary>
        ///     非同期で取得する。
        /// </summary>
        /// <param name="grace"> 登録を待機する最大秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> キャッシュまたは待機後に取得したインスタンス。 </returns>
        public Awaitable<T> GetAsync(byte grace = 120, CancellationToken token = default)
        {
            T instance = GetCachedInstance();

            // キャッシュ済みなら登録待機を避け、呼び出しごとの新しいAwaitableで即座に返す。
            if (instance != null)
            {
                return SymphonyAwaitable.FromResult(instance);
            }

            return SymphonyAwaitable.FromTask(LocateAndCacheAsync(grace, token));
        }

        /// <summary>
        ///     インスタンスの取得を試みる。
        /// </summary>
        /// <param name="instance"> 取得できたインスタンス。 </param>
        /// <returns> インスタンスを取得できた場合はtrue。 </returns>
        public bool TryGet(out T instance)
        {
            instance = GetCachedInstance();

            // キャッシュが無い場合だけPartituraへ問い合わせ、取得できた値を以後も再利用する。
            if (instance == null && Partitura.TryGet(out instance))
            {
                _instance = instance;
            }

            return instance != null;
        }

        #endregion

        #region 内部処理

        /// <summary> キャッシュするインスタンス。 </summary>
        private T _instance;

        /// <summary>
        ///     利用可能なキャッシュを返す。
        /// </summary>
        /// <returns> キャッシュ。無い場合と破棄済みUnity Objectの場合はnull。 </returns>
        /// <remarks>
        ///     型引数の <c>== null</c> はUnity Objectの破棄を判定できないため、
        ///     Partituraと同じ判定で破棄済みのキャッシュを捨てて取り直させる。
        /// </remarks>
        private T GetCachedInstance()
        {
            if (!Partitura.IsAvailableInstance(_instance)) { _instance = null; }

            return _instance;
        }

        /// <summary>
        ///     Partituraの登録を待機し、取得したインスタンスをキャッシュする。
        /// </summary>
        /// <param name="grace"> 登録を待機する最大秒数。 </param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 待機後に取得したインスタンス。 </returns>
        private async Task<T> LocateAndCacheAsync(byte grace, CancellationToken token)
        {
            // 待機完了後の取得結果を同期APIと共有し、次回以降の登録待機を省く。
            T instance = await Partitura.GetAsync<T>(grace, token);
            _instance = instance;
            return instance;
        }

        #endregion
    }
}
