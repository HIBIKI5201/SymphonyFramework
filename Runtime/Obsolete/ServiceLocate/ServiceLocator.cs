using System;
using System.Threading;
using System.Threading.Tasks;
using Api = SymphonyFrameWork.System.API;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     <see cref="Api.ServiceLocator"/> へ移動しました。
    /// </summary>
    [Obsolete("SymphonyFrameWork.System.API.ServiceLocator に移動しました。今後はそちらを使用してください。", error: false)]
    public static class ServiceLocator
    {
        /// <summary>
        ///     指定されたインスタンスをロケーターに登録します。
        /// </summary>
        /// <typeparam name="T"> 登録するインスタンスの型。 </typeparam>
        /// <param name="instance"> 登録するインスタンス。 </param>
        /// <param name="type"> SingletonまたはLocatorの登録方式。 </param>
        public static bool RegisterInstance<T>(T instance, LocateType type = LocateType.Locator) where T : class
            => Api.ServiceLocator.RegisterInstance(instance, type);

        /// <summary>
        ///     指定されたインスタンスをロケーターに登録します。
        /// </summary>
        /// <param name="type"> 登録時のキーとして使用する実行時型。 </param>
        /// <param name="instance"> 登録するインスタンス。 </param>
        /// <param name="locateType"> SingletonまたはLocatorの登録方式。 </param>
        public static bool RegisterInstance(Type type, object instance, LocateType locateType = LocateType.Locator)
            => Api.ServiceLocator.RegisterInstance(type, instance, locateType);

        /// <summary>
        ///     指定したインスタンスをロケーターから登録解除します。
        /// </summary>
        /// <typeparam name="T"> 登録解除するインスタンスの型。 </typeparam>
        /// <param name="instance"> 現在の登録と同一か確認するインスタンス。 </param>
        /// <returns> 同一インスタンスの登録を解除できた場合はtrue。 </returns>
        public static bool UnregisterInstance<T>(T instance) where T : class
            => Api.ServiceLocator.UnregisterInstance(instance);

        /// <summary>
        ///     指定したタイプをロケーターから登録解除します。
        /// </summary>
        /// <param name="type"> 登録解除する実行時型。 </param>
        /// <returns> 登録を解除できた場合はtrue。 </returns>
        public static bool UnregisterInstance(Type type)
            => Api.ServiceLocator.UnregisterInstance(type);

        /// <summary>
        ///     指定した型のインスタンスをロケーターから登録解除します。
        /// </summary>
        /// <typeparam name="T"> 登録解除するインスタンスの型。 </typeparam>
        /// <returns> 登録を解除できた場合はtrue。 </returns>
        public static bool UnregisterInstance<T>() where T : class
            => Api.ServiceLocator.UnregisterInstance<T>();

        /// <summary>
        ///     指定したインスタンスと同じ型の登録済みインスタンスを破棄します。
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型。</typeparam>
        /// <param name="instance">破棄の対象となるインスタンス。</param>
        public static bool DestroyInstance<T>(T instance) where T : class
            => Api.ServiceLocator.DestroyInstance(instance);

        /// <summary>
        ///     指定した型のインスタンスを破棄します。
        /// </summary>
        /// <typeparam name="T">破棄したいインスタンスの型。</typeparam>
        public static bool DestroyInstance<T>() where T : class
            => Api.ServiceLocator.DestroyInstance<T>();

        /// <summary> 指定型のインスタンスが登録されているか確認する。 </summary>
        public static bool IsExistInstance<T>() where T : class
            => Api.ServiceLocator.IsExistInstance<T>();

        /// <summary> 指定インスタンスの型が登録されているか確認する。 </summary>
        public static bool IsExistInstance<T>(T instance) where T : class
            => Api.ServiceLocator.IsExistInstance<T>(instance);

        /// <summary> 実行時型のインスタンスが登録されているか確認する。 </summary>
        public static bool IsExistInstance(Type type)
            => Api.ServiceLocator.IsExistInstance(type);

        /// <summary>
        ///     登録されたインスタンスを取得します。
        /// </summary>
        /// <typeparam name="T">取得したいインスタンスの型。</typeparam>
        /// <returns>指定した型のインスタンス。見つからない場合や破棄済みの場合はnull。</returns>
        public static T GetInstance<T>() where T : class
            => Api.ServiceLocator.GetInstance<T>();

        /// <summary>
        ///     指定した型のインスタンスが登録されているかどうかを確認し、登録されていればそのインスタンスを返します。
        /// </summary>
        /// <typeparam name="T"> 取得するインスタンスの型。 </typeparam>
        /// <param name="result"> 取得できた登録済みインスタンス。 </param>
        /// <returns> インスタンスを取得できた場合はtrue。 </returns>
        public static bool TryGetInstance<T>(out T result) where T : class
            => Api.ServiceLocator.TryGetInstance(out result);

        /// <summary>
        ///     指定した型のインスタンスが登録されるまで非同期で待機し、取得します。
        /// </summary>
        /// <typeparam name="T">取得したいインスタンスの型。</typeparam>
        /// <param name="grace">最大待機時間（秒）。この時間を超えるとnullを返します。</param>
        /// <param name="token">キャンセルトークン。</param>
        /// <returns>指定した型のインスタンス。見つからない場合はnull。</returns>
        public static ValueTask<T> GetInstanceAsync<T>(
            byte grace = 120,
            CancellationToken token = default) where T : class
            => Api.ServiceLocator.GetInstanceAsync<T>(grace, token);

        /// <summary> 制限時間内に指定型のインスタンスを取得し、成否と結果を返す。 </summary>
        public static ValueTask<(bool success, T result)> TryGetInstanceAsync<T>(
            byte grace = 120,
            CancellationToken token = default)
            where T : class
            => Api.ServiceLocator.TryGetInstanceAsync<T>(grace, token);

        /// <summary>
        ///     指定した型のオブジェクトが登録された時に、指定したアクションを実行します。
        ///     既に登録済みの場合は即座に実行されます。
        /// </summary>
        /// <typeparam name="T">待機するインスタンスの型。</typeparam>
        /// <param name="action">実行するアクション。</param>
        public static void RegisterAfterLocate<T>(Action action) where T : class
            => Api.ServiceLocator.RegisterAfterLocate<T>(action);

        /// <summary>
        ///     指定した型のオブジェクトが登録された時に、そのインスタンスを引数としてアクションを実行します。
        ///     既に登録済みの場合は即座に実行されます。
        /// </summary>
        /// <typeparam name="T">待機するインスタンスの型。</typeparam>
        /// <param name="action">実行するアクション。引数として登録されたインスタンスを受け取ります。</param>
        public static void RegisterAfterLocate<T>(Action<T> action) where T : class
            => Api.ServiceLocator.RegisterAfterLocate(action);
    }
}
