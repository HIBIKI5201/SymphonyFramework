using UnityEngine;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     依存性注入を受け取れるコンポーネントを表す。
    /// </summary>
    /// <remarks>
    ///     SceneLoaderは、シーンロード時に実装済みのルートオブジェクトへ自動的に注入する。
    /// </remarks>
    public interface IInjectable
    {
    }

    /// <summary>
    ///     1件の依存関係を注入可能なコンポーネントを表す。
    /// </summary>
    public interface IInjectable<T0> : IInjectable
        where T0 : class
    {
        #region 外部向けAPI

        /// <summary>
        ///     解決済みの依存関係を受け取る。
        /// </summary>
        void Inject(T0 arg0);

        #endregion
    }

    /// <summary>
    ///     2件の依存関係を注入可能なコンポーネントを表す。
    /// </summary>
    public interface IInjectable<T0, T1> : IInjectable
        where T0 : class
        where T1 : class
    {
        #region 外部向けAPI

        /// <summary>
        ///     解決済みの依存関係を受け取る。
        /// </summary>
        void Inject(T0 arg0, T1 arg1);

        #endregion
    }

    /// <summary>
    ///     3件の依存関係を注入可能なコンポーネントを表す。
    /// </summary>
    public interface IInjectable<T0, T1, T2> : IInjectable
        where T0 : class
        where T1 : class
        where T2 : class
    {
        #region 外部向けAPI

        /// <summary>
        ///     解決済みの依存関係を受け取る。
        /// </summary>
        void Inject(T0 arg0, T1 arg1, T2 arg2);

        #endregion
    }

    /// <summary>
    ///     4件の依存関係を注入可能なコンポーネントを表す。
    /// </summary>
    public interface IInjectable<T0, T1, T2, T3> : IInjectable
        where T0 : class
        where T1 : class
        where T2 : class
        where T3 : class
    {
        #region 外部向けAPI

        /// <summary>
        ///     解決済みの依存関係を受け取る。
        /// </summary>
        void Inject(T0 arg0, T1 arg1, T2 arg2, T3 arg3);

        #endregion
    }
}
