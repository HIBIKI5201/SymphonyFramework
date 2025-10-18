using SymphonyFrameWork.System;
using System;
using System.Reflection;
using UnityEngine;

namespace SymphonyFrameWork.Utility
{
    /// <summary>
    ///     ServiceLocatorにロケート登録するクラス
    /// </summary>
    [HelpURL("https://www.notion.so/SymphonyLocate-19d7c2c6cc02809ea815c3a750fa95ca?pvs=4")]
    public class SymphonyLocate : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("ロケートするコンポーネント")]
        private Component _target;

        [SerializeField]
        private ServiceLocator.LocateType _locateType = ServiceLocator.LocateType.Locator;

        [SerializeField] private bool _autoRegister = true;
        [SerializeField] private bool _autoUnregister = true;

        private Type _targetType;

        private void Awake()
        {
            _targetType = _target.GetType();
            Debug.Assert(_targetType != null, "Target type is null. Please assign a valid component to the target field.");
        }
        private void OnEnable()
        {
            if (!_autoRegister) return;
            if (_target == null) return;

            if (_target)
            {
                //Targetのクラスをキャストして実行する

                var method = typeof(ServiceLocator)
                    .GetMethod(nameof(ServiceLocator.RegisterInstance))
                    ?.MakeGenericMethod(_targetType);

                method?.Invoke(null, new object[]
                    { _target, _locateType });
            }
        }

        private void OnDisable()
        {
            if (!_autoUnregister) return;
            if (_target == null) return;

            if (_target != null)
            {
                //ロケーターに登録されているか確認する
                var getMethod = typeof(ServiceLocator)
                    .GetMethod(nameof(ServiceLocator.GetInstance),
                        BindingFlags.Public | BindingFlags.Static,
                        null, Type.EmptyTypes, null)
                    ?.MakeGenericMethod(_targetType);

                var instance = getMethod?.Invoke(null, null);
                if (instance == null) return; //登録されていなければ終了

                //ServiceLocator.DestroyInstanceを取得する
                var destroyMethod = typeof(ServiceLocator)
                    .GetMethod(nameof(ServiceLocator.UnregisterInstance),
                        BindingFlags.Public | BindingFlags.Static,
                        null, Type.EmptyTypes, null)
                    ?.MakeGenericMethod(_targetType);

                destroyMethod?.Invoke(null, null);
            }
        }
    }
}