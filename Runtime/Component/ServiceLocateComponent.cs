using SymphonyFrameWork.Attribute;
using SymphonyFrameWork.System.ServiceLocate;
using System;
using UnityEngine;

namespace SymphonyFrameWork.Utility
{
    /// <summary>
    ///     ServiceLocatorにロケート登録するクラス
    /// </summary>
    [HelpURL("https://www.notion.so/SymphonyLocate-19d7c2c6cc02809ea815c3a750fa95ca?pvs=4")]
    [DefaultExecutionOrder(-1000)] // 最初に実行されるようにする。
    public sealed class ServiceLocateComponent : MonoBehaviour
    {
        [SerializeField, Tooltip("ロケートするコンポーネント")]
        private Component _target;

        [SerializeField, Tooltip("SingletonまたはLocatorの登録方式。")]
        private LocateTypeEnum _locateType = LocateTypeEnum.Locator;

        [SerializeField, Tooltip("有効化時に対象をService Locatorへ自動登録するか。")]
        private bool _autoRegister = true;

        [SerializeField, Tooltip("無効化時に対象をService Locatorから自動解除するか。")]
        private bool _autoUnregister = true;

        [SerializeField, ReadOnly, Tooltip("ロケートするコンポーネントの型。")]
        private Type _targetType;

        /// <summary> 
        ///     シリアライズされた登録対象の型が有効か検証する。 
        /// </summary>
        private void Awake()
        {
            Debug.Assert(_targetType != null, "Target type is null. Please assign a valid component to the target field.");
        }

        /// <summary>
        ///     自動登録が有効な場合に対象をService Locatorへ登録する。 
        /// </summary>
        private void OnEnable()
        {
            if (!_autoRegister) { return; }
            if (_target == null) { return; }

            ServiceLocator.RegisterInstance(_targetType, _target, _locateType);
        }

        /// <summary> 
        ///     自動解除が有効な場合に対象をService Locatorから解除する。
        /// </summary>
        private void OnDisable()
        {
            if (!_autoUnregister) { return; }
            if (_target == null) { return; }
            if (!ServiceLocator.IsInitialized) { return; }

            // 別の所有者が解除済みの場合に重複解除しない。
            bool isExist = ServiceLocator.IsExistInstance(_targetType); // TODO: これだと、同じ型の別のインスタンスが登録されている場合に誤判定する可能性がある。必要に応じて、インスタンス自体を確認する方法に変更することを検討する。
            if (!isExist) { return; }

            ServiceLocator.UnregisterInstance(_targetType);
        }

        /// <summary>
        ///     インスペクターで指定された対象の実行時型を同期する。
        ///     </summary>
        private void OnValidate()
        {
            if (_target == null) { return; }
            _targetType = _target.GetType();
        }
    }
}
