using System;

using UnityEngine;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     Singleton Componentの所有とUnity Objectの解放を行う境界。
    /// </summary>
    internal sealed class ServiceHostComponent : MonoBehaviour, IServiceHost
    {
        #region 外部向けAPI

        /// <summary>
        ///     所有GameObjectを冪等に破棄する。
        /// </summary>
        internal void DisposeHost()
        {
            // 破棄要求が重複した場合は、同じGameObjectへ再度破棄処理を行わない。
            if (_isDisposed) { return; }

            // 再入を防いでから、Unity上に所有先が残っている場合だけ破棄する。
            _isDisposed = true;
            if (this != null && gameObject != null) { DestroyGameObject(gameObject); }
        }

        /// <inheritdoc />
        void IServiceHost.Attach(object instance)
        {
            // 終了中またはHost破棄後は、無効なTransformへ所有関係を作らない。
            if (_isQuitting || _isDisposed) { return; }

            // Unity上で有効なComponentだけをHostの所有階層へ接続する。
            if (instance is Component component && component != null) { component.transform.SetParent(transform); }
        }

        /// <inheritdoc />
        void IServiceHost.Detach(object instance)
        {
            // 終了中、Component以外、破棄済み、または別所有者のpayloadは階層を変更しない。
            if (_isQuitting
                || instance is not Component component
                || component == null
                || component.transform.parent != transform)
            {
                return;
            }

            component.transform.SetParent(null);
        }

        /// <inheritdoc />
        bool IServiceHost.DisposeInstance(object instance)
        {
            bool disposed = false;

            // IDisposableの解放が失敗しても、Unity ComponentのGameObject破棄は必ず試みる。
            try
            {
                // payloadがIDisposableを実装する場合は、型固有の解放処理を実行する。
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                    disposed = true;
                }
            }
            finally
            {
                // 有効なComponentの場合は、Disposeの成否にかかわらずGameObjectも破棄する。
                if (instance is Component component && component != null)
                {
                    DestroyGameObject(component.gameObject);
                    disposed = true;
                }
            }

            return disposed;
        }

        #endregion

        #region 内部処理

        private bool _isDisposed;
        private bool _isQuitting;

        /// <summary>
        ///     Unity終了中のTransform操作を防ぐ。
        /// </summary>
        private void OnApplicationQuit() => _isQuitting = true;

        /// <summary>
        ///     実行環境に適したUnity Object破棄処理を使用する。
        /// </summary>
        /// <param name="target"> 破棄するGameObject。 </param>
        private static void DestroyGameObject(GameObject target)
        {
            // Play Modeではフレーム終端の破棄を使い、Edit Modeでは即時に破棄する。
            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }

        #endregion
    }
}
