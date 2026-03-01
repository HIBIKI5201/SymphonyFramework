using SymphonyFrameWork.System.ServiceLocate;
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
        private LocateType _locateType = LocateType.Locator;

        [SerializeField] private bool _autoRegister = true;
        [SerializeField] private bool _autoUnregister = true;

        [SerializeField, HideInInspector]
        private Type _targetType;

        private void Awake()
        {
            Debug.Assert(_targetType != null, "Target type is null. Please assign a valid component to the target field.");
        }
        private void OnEnable()
        {
            if (!_autoRegister) { return; }
            if (_target == null) { return; }

            ServiceLocator.RegisterInstance(_targetType, _target, _locateType);
        }

        private void OnDisable()
        {
            if (!_autoUnregister) { return; }
            if (_target == null) { return; }

            if (_target != null)
            {
                //ロケーターに登録されているか確認する。
                bool isExist = ServiceLocator.IsExistInstance(_targetType);
                if (!isExist) { return; }

                ServiceLocator.UnregisterInstance(_targetType);
            }
        }

        private void OnValidate()
        {
            if (_target == null) { return; }
            _targetType = _target.GetType();
        }
    }
}