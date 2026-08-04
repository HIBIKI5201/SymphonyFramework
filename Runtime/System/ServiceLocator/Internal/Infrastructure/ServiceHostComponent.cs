using System;

using UnityEngine;

namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary> Singleton Componentの所有とUnity Objectの解放を行う境界。 </summary>
    internal sealed class ServiceHostComponent : MonoBehaviour, IServiceHost
    {
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
                DestroyGameObject(gameObject);
            }
        }

        /// <inheritdoc />
        void IServiceHost.Attach(object instance)
        {
            if (_isQuitting || _isDisposed)
            {
                return;
            }

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

            try
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                    disposed = true;
                }
            }
            finally
            {
                if (instance is Component component && component != null)
                {
                    DestroyGameObject(component.gameObject);
                    disposed = true;
                }
            }

            return disposed;
        }

        /// <summary> Unity終了中のTransform操作を防ぐ。 </summary>
        private void OnApplicationQuit() => _isQuitting = true;

        /// <summary> 実行環境に適したUnity Object破棄処理を使用する。 </summary>
        /// <param name="target"> 破棄するGameObject。 </param>
        private static void DestroyGameObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }

        private bool _isDisposed;
        private bool _isQuitting;
    }
}
