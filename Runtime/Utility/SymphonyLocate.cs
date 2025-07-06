using System;
using System.Reflection;
using SymphonyFrameWork.System;
using UnityEngine;

namespace SymphonyFrameWork.Utility
{
    /// <summary>
    ///     ServiceLocatorにロケート登録するクラス
    /// </summary>
    [HelpURL("https://www.notion.so/SymphonyLocate-19d7c2c6cc02809ea815c3a750fa95ca?pvs=4")]
    public class SymphonyLocate : MonoBehaviour
    {
        [SerializeField] [Tooltip("ロケートするコンポーネント")]
        private Component _target;

        [SerializeField]
        private ServiceLocator.LocateType _locateType = ServiceLocator.LocateType.Locator;
        
        [SerializeField] private bool _autoSet = true;
        [SerializeField] private bool _autoDestroy = true;
        private void Awake()
        {
            if (!_autoSet) return;
            
            if (_target)
            {
                //Targetのクラスをキャストして実行する
                var targetType = _target.GetType();
                var method = typeof(ServiceLocator)
                    .GetMethod(nameof(ServiceLocator.SetInstance))
                    ?.MakeGenericMethod(targetType);

                method?.Invoke(null, new object[]
                    { _target, _locateType });
            }
        }

        private void OnDestroy()
        {
            if (Application.isEditor) return;
            if (!_autoDestroy) return;
            
            if (_target)
            {
                var targetType = _target.GetType();

                //ロケーターに登録されているか確認する
                var getMethod = typeof(ServiceLocator)
                    .GetMethod(nameof(ServiceLocator.GetInstance),
                        BindingFlags.Public | BindingFlags.Static,
                        null, Type.EmptyTypes, null)
                    ?.MakeGenericMethod(targetType);
                
                var instance = getMethod?.Invoke(null, null);
                if (instance == null) return; //登録されていなければ終了
                
                //ServiceLocator.DestroyInstanceを取得する
                var destroyMethod = typeof(ServiceLocator)
                    .GetMethod(nameof(ServiceLocator.DestroyInstance),
                        BindingFlags.Public | BindingFlags.Static,
                        null, Type.EmptyTypes, null)
                    ?.MakeGenericMethod(targetType);

                destroyMethod?.Invoke(null, null);
            }
        }
    }
}