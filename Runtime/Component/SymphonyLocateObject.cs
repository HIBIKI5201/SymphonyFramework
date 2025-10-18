using SymphonyFrameWork.System;
using System.Threading;
using System.Threading.Tasks;

namespace SymphonyFrameWork.Utility
{
    /// <summary>
    ///     ServiceLocatorに登録されているインスタンスを保持する。
    /// </summary>
    public class SymphonyLocateObject<T> where T : class
    {
        /// <summary>
        ///     取得する。
        /// </summary>
        /// <returns></returns>
        public T GetInstance()
        {
            T instance = _instance;

            // インスタンスがキャッシュされていなければ取得。
            if (instance == null)
            {
                instance = ServiceLocator.GetInstance<T>();
                _instance = instance; //キャッシュする。
            }

            return instance;
        }

        /// <summary>
        ///     非同期で取得する。
        /// </summary>
        /// <param name="grace"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<T> GetInstanceAsync(byte grace = 120, CancellationToken token = default)
        {
            T instance = _instance;

            // インスタンスがキャッシュされていなければ取得。
            if (instance == null)
            {
                instance = await ServiceLocator.GetInstanceAsync<T>(grace, token);
                _instance = instance; //キャッシュする。
            }

            return instance;
        }

        /// <summary>
        ///     インスタンスの取得を試みる。
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public bool TryGetInstance(out T instance)
        {
            instance = _instance;

            // インスタンスがキャッシュされていなければ取得。
            if (instance == null && ServiceLocator.TryGetInstance(out instance))
            {
                _instance = instance; // キャッシュする。
            }

            return instance != null;
        }

        /// <summary> キャッシュされる値 </summary>
        private T _instance;
    }
}
