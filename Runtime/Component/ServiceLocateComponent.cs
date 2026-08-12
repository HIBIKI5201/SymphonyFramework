using SymphonyFrameWork.Attribute;
using SymphonyFrameWork.System.ServiceLocate;
using System;
using UnityEngine;

namespace SymphonyFrameWork.Utility
{
    /// <summary>
    ///     Inspectorで指定したComponentをService Locatorへ登録する。
    /// </summary>
    [HelpURL("https://www.notion.so/SymphonyLocate-19d7c2c6cc02809ea815c3a750fa95ca?pvs=4")]
    [DefaultExecutionOrder(-1000)] // 最初に実行されるようにする。
    public sealed class ServiceLocateComponent : MonoBehaviour
    {
        #region 内部処理

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
            // 登録時に型情報を参照できないシリアライズ状態を、ライフサイクル開始時に検出する。
            Debug.Assert(_targetType != null, "Target type is null. Please assign a valid component to the target field.");
        }

        /// <summary>
        ///     自動登録が有効な場合に対象をService Locatorへ登録する。 
        /// </summary>
        private void OnEnable()
        {
            // 自動登録を利用しない設定では、呼び出し側による明示的な登録を優先する。
            if (!_autoRegister) { return; }

            // 対象が未設定なら、無効な型とインスタンスの組をService Locatorへ渡さない。
            if (_target == null) { return; }

            // 有効化と無効化を登録期間として扱い、指定された方式で対象を公開する。
            ServiceLocator.RegisterInstance(_targetType, _target, _locateType);
        }

        /// <summary>
        ///     自動解除が有効な場合に対象をService Locatorから解除する。
        /// </summary>
        private void OnDisable()
        {
            // 自動解除を利用しない設定では、登録を所有する呼び出し側へ解除判断を委ねる。
            if (!_autoUnregister) { return; }

            // 登録対象が無ければ、このComponentが解除すべきインスタンスも存在しない。
            if (_target == null) { return; }

            // Orchestratorの終了処理でService Locatorが先にリセット済みの場合は重複操作を避ける。
            if (!ServiceLocator.IsInitialized) { return; }

            // 別の所有者が解除済みの場合に重複解除しない。
            bool isExist = ServiceLocator.IsExistInstance(_targetType); // TODO: これだと、同じ型の別のインスタンスが登録されている場合に誤判定する可能性がある。必要に応じて、インスタンス自体を確認する方法に変更することを検討する。
            if (!isExist) { return; }

            // このComponentの無効化に合わせて、Service Locator上の登録期間を閉じる。
            ServiceLocator.UnregisterInstance(_targetType);
        }

        /// <summary>
        ///     インスペクターで指定された対象の実行時型を同期する。
        /// </summary>
        private void OnValidate()
        {
            // 未設定時は既存の型情報を維持し、Inspector編集中の一時的なnullで上書きしない。
            if (_target == null) { return; }

            // Unityが直接シリアライズできないTypeを、選択されたComponentから同期する。
            _targetType = _target.GetType();
        }

        #endregion
    }
}
