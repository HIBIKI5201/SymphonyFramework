using System;

using UnityEngine;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary> Singleton Componentの所有とUnity Objectの解放を行う境界。 </summary>
    internal sealed class ServiceHostComponent : MonoBehaviour, IServiceHost
    {
        /// <summary> MCP診断が既存の実効登録方式を判定するための所有先。 </summary>
        internal Transform Root => this != null ? transform : null;

        /// <summary> 所有GameObjectを冪等に破棄する。 </summary>
        internal void DisposeHost()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            if (this != null && gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        /// <inheritdoc />
        void IServiceHost.Attach(object instance)
        {
            if (instance is Component component && component != null)
            {
                component.transform.SetParent(transform);
            }
        }

        /// <inheritdoc />
        void IServiceHost.Detach(object instance)
        {
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

            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
                disposed = true;
            }

            if (instance is Component component && component != null)
            {
                Destroy(component.gameObject);
                disposed = true;
            }

            return disposed;
        }

        /// <summary> Unity終了中のTransform操作を防ぐ。 </summary>
        private void OnApplicationQuit() => _isQuitting = true;

        private bool _isDisposed;
        private bool _isQuitting;
    }
}
